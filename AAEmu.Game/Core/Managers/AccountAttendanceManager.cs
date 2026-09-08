using System.Collections.Concurrent;
using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.AccountAttendance;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Features;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Mails;
using MySql.Data.MySqlClient;
using NLog;

namespace AAEmu.Game.Core.Managers;

public class AccountAttendanceManager : Singleton<AccountAttendanceManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<(uint AccountId, int Year, int Month), Dictionary<int, Claim>> _months = [];

    private sealed class Claim
    {
        public long AttendedAt { get; init; }
        public bool IsArchelife { get; init; }
    }

    public void SendMonth(Character character)
    {
        if (character == null)
            return;

        var day = AccountAttendanceRules.CalendarDay(DateTime.UtcNow);
        if (!TryGetMonth(character.AccountId, day.Year, day.Month, out var claims))
            claims = [];

        var times = new long[AccountAttendanceRules.DaysInPacket];
        var archelife = new bool[AccountAttendanceRules.DaysInPacket];
        foreach (var (claimedDay, claim) in claims)
        {
            if (!AccountAttendanceRules.IsValidDay(claimedDay))
                continue;
            times[claimedDay - 1] = claim.AttendedAt;
            archelife[claimedDay - 1] = claim.IsArchelife;
        }

        character.SendPacket(new SCAccountAttendancePacket(times, archelife));
    }

    public void HandleAdd(Character character)
    {
        if (character == null)
            return;
        if (!FeaturesManager.Fsets.Check(Feature.account_attendance))
        {
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        var day = AccountAttendanceRules.CalendarDay(DateTime.UtcNow);
        if (!TryGetMonth(character.AccountId, day.Year, day.Month, out var claims) ||
            !AccountAttendanceRules.CanClaim(claims.ContainsKey(day.Day)))
        {
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        var daily = AccountAttendanceGameData.Instance.DailyReward(
            day.Year,
            day.Month,
            AccountAttendanceRules.NextDayCount(claims.Count));
        if (daily == null)
        {
            Logger.Warn(
                "Account attendance: no daily reward for {0}-{1:00} dayCount={2} name={3}",
                day.Year,
                day.Month,
                AccountAttendanceRules.NextDayCount(claims.Count),
                character.Name);
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        var memberships = AccountMemberships.ActiveIds(character.AccountId, AppConfiguration.Instance.Id);
        var isArchelife = AccountPatronRules.IsArcheLife(memberships);
        var grants = new List<AccountAttendanceReward> { daily };
        var archelifeDays = claims.Values.Count(x => x.IsArchelife) + (isArchelife ? 1 : 0);
        var extra = AccountAttendanceGameData.Instance.AdditionalReward(day.Year, day.Month, archelifeDays);
        if (isArchelife && extra != null &&
            AccountAttendanceRules.ShouldGrantAdditional(archelifeDays, extra.DayCount))
            grants.Add(extra);

        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claim = new Claim { AttendedAt = unix, IsArchelife = isArchelife };
        if (!TryPersist(character.AccountId, day.Year, day.Month, day.Day, unix, isArchelife))
        {
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        claims[day.Day] = claim;
        if (!TryGrant(character, grants, out var byMail))
        {
            claims.Remove(day.Day);
            TryRemoveClaim(character.AccountId, day.Year, day.Month, day.Day);
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        character.SendPacket(new SCAccountAttendanceAddedPacket(true, unix, isArchelife));
        character.SendPacket(new SCAccountAttendanceRewardedPacket(0, byMail));
        SendMonth(character);
        Logger.Info(
            "Account attendance claimed {0} {1}-{2:00}-{3:00} archelife={4} byMail={5}",
            character.Name,
            day.Year,
            day.Month,
            day.Day,
            isArchelife,
            byMail);
    }

    private static bool TryGrant(Character character, IReadOnlyList<AccountAttendanceReward> grants, out bool byMail)
    {
        byMail = false;
        var items = new List<AccountAttendanceReward>();
        foreach (var grant in grants)
        {
            if (ItemWallet.TryCreditCashPack(character, grant.ItemId, grant.ItemCount))
                continue;
            items.Add(grant);
        }

        if (items.Count == 0)
            return true;

        var bagOk = items.All(x =>
            character.Inventory.Bag.SpaceLeftForItem(x.ItemId) >= x.ItemCount);
        if (bagOk)
        {
            foreach (var grant in items)
            {
                if (!character.Inventory.Bag.AcquireDefaultItemEx(
                        ItemTaskType.SkillEffectGainItem,
                        grant.ItemId,
                        grant.ItemCount,
                        grant.ItemGradeId,
                        out _,
                        out _,
                        character.Id))
                    return false;
            }
            return true;
        }

        var mail = new BaseMail
        {
            MailType = MailType.Promotion,
            Title = "Attendance",
            ReceiverName = character.Name,
            Header =
            {
                SenderName = ".attendance",
                ReceiverId = character.Id
            },
            Body =
            {
                Text = "Attendance",
                SendDate = DateTime.UtcNow,
                RecvDate = DateTime.UtcNow
            }
        };

        foreach (var grant in items)
        {
            if (!character.Inventory.MailAttachments.AcquireDefaultItemEx(
                    ItemTaskType.Invalid,
                    grant.ItemId,
                    grant.ItemCount,
                    grant.ItemGradeId,
                    out var added,
                    out _,
                    character.Id))
                return false;
            mail.Body.Attachments.AddRange(added);
        }

        if (!mail.Send())
            return false;
        byMail = true;
        return true;
    }

    private bool TryGetMonth(uint accountId, int year, int month, out Dictionary<int, Claim> claims)
    {
        var key = (accountId, year, month);
        if (_months.TryGetValue(key, out claims))
            return true;

        claims = [];
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT day, attended_at, is_archelife
                FROM account_attendances
                WHERE account_id = @account AND year = @year AND month = @month
                """;
            command.Parameters.AddWithValue("@account", accountId);
            command.Parameters.AddWithValue("@year", year);
            command.Parameters.AddWithValue("@month", month);
            command.Prepare();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                claims[reader.GetInt32("day")] = new Claim
                {
                    AttendedAt = reader.GetInt64("attended_at"),
                    IsArchelife = reader.GetBoolean("is_archelife")
                };
            }
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn(
                "Account attendance table missing — run SQL/updates/2026-09-06_aaemu_game_account_attendances.sql ({0})",
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Account attendance LoadMonth failed {0}-{1:00}", year, month);
            return false;
        }

        _months[key] = claims;
        return true;
    }

    private static bool TryPersist(uint accountId, int year, int month, int day, long attendedAt, bool isArchelife)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO account_attendances
                    (account_id, year, month, day, attended_at, is_archelife)
                VALUES
                    (@account, @year, @month, @day, @attendedAt, @archelife)
                """;
            command.Parameters.AddWithValue("@account", accountId);
            command.Parameters.AddWithValue("@year", year);
            command.Parameters.AddWithValue("@month", month);
            command.Parameters.AddWithValue("@day", day);
            command.Parameters.AddWithValue("@attendedAt", attendedAt);
            command.Parameters.AddWithValue("@archelife", isArchelife ? 1 : 0);
            command.Prepare();
            return command.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn("Account attendance Persist skipped (table missing)");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Account attendance Persist failed {0}-{1:00}-{2:00}", year, month, day);
            return false;
        }
    }

    private static bool TryRemoveClaim(uint accountId, int year, int month, int day)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                DELETE FROM account_attendances
                WHERE account_id = @account AND year = @year AND month = @month AND day = @day
                """;
            command.Parameters.AddWithValue("@account", accountId);
            command.Parameters.AddWithValue("@year", year);
            command.Parameters.AddWithValue("@month", month);
            command.Parameters.AddWithValue("@day", day);
            command.Prepare();
            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Account attendance RemoveClaim failed {0}-{1:00}-{2:00}", year, month, day);
            return false;
        }
    }
}
