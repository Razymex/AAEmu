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
        var claims = LoadMonth(character.AccountId, day.Year, day.Month);
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
        var claims = LoadMonth(character.AccountId, day.Year, day.Month);
        if (!AccountAttendanceRules.CanClaim(claims.ContainsKey(day.Day)))
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
                "Account attendance: no daily reward for {0}-{1:00} dayCount={2} account={3}",
                day.Year,
                day.Month,
                AccountAttendanceRules.NextDayCount(claims.Count),
                character.AccountId);
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

        if (!TryGrant(character, grants, out var byMail))
        {
            character.SendPacket(new SCAccountAttendanceAddedPacket(false, 0, false));
            return;
        }

        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        claims[day.Day] = new Claim { AttendedAt = unix, IsArchelife = isArchelife };
        Persist(character.AccountId, day.Year, day.Month, day.Day, unix, isArchelife);
        character.SendPacket(new SCAccountAttendanceAddedPacket(true, unix, isArchelife));
        character.SendPacket(new SCAccountAttendanceRewardedPacket(0, byMail));
        SendMonth(character);
        Logger.Info(
            "Account attendance claimed account={0} {1}-{2:00}-{3:00} archelife={4} byMail={5}",
            character.AccountId,
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
                character.Inventory.Bag.AcquireDefaultItemEx(
                    ItemTaskType.SkillEffectGainItem,
                    grant.ItemId,
                    grant.ItemCount,
                    grant.ItemGradeId,
                    out _,
                    out _,
                    character.Id);
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

    private Dictionary<int, Claim> LoadMonth(uint accountId, int year, int month)
    {
        var key = (accountId, year, month);
        if (_months.TryGetValue(key, out var cached))
            return cached;

        var claims = new Dictionary<int, Claim>();
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
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Account attendance LoadMonth failed for account {0}", accountId);
        }

        _months[key] = claims;
        return claims;
    }

    private void Persist(uint accountId, int year, int month, int day, long attendedAt, bool isArchelife)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                REPLACE INTO account_attendances
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
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn(
                "Account attendance Persist skipped (table missing): account={0}",
                accountId);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Account attendance Persist failed account={0}", accountId);
        }
    }
}
