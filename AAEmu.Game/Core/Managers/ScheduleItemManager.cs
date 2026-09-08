using System.Collections.Concurrent;
using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Mails;
using AAEmu.Game.Models.Game.ScheduleItems;
using MySql.Data.MySqlClient;
using NLog;

namespace AAEmu.Game.Core.Managers;

public class ScheduleItemManager : Singleton<ScheduleItemManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<(uint AccountId, int ScheduleId), Progress> _progress = [];

    private sealed class Progress
    {
        public byte Gave { get; set; }
        public long Cumulated { get; set; }
        public DateTime Updated { get; set; }
        public DateTime LastOnlineTick { get; set; }
    }

    public void SendActive(Character character)
    {
        if (character == null)
            return;

        var now = DateTime.UtcNow;
        var items = new List<ScheduleItem>();
        foreach (var def in ScheduleItemGameData.Instance.ActiveOnAir(now))
        {
            if (!IsEligible(character, def))
                continue;
            var progress = Load(character.AccountId, def.Id, now);
            items.Add(ToWire(def.Id, progress));
            if (items.Count >= ScheduleItemRules.MaxItemsInPacket)
                break;
        }

        character.SendPacket(new SCScheduleItemUpdatePacket(items));
        AutoGrantSilent(character, now);
        Logger.Info("Schedule items sent account={0} count={1}", character.AccountId, items.Count);
    }

    private void AutoGrantSilent(Character character, DateTime now)
    {
        foreach (var def in ScheduleItemGameData.Instance.SilentOnAir(now))
        {
            if (!IsEligible(character, def))
                continue;

            var progress = Load(character.AccountId, def.Id, now);
            if (!ScheduleItemRules.ShouldAutoGrant(def.Kind, def.ActiveTake, def.GiveTerm, progress.Gave, def.GiveMax))
                continue;
            if (!TryGrant(character, def, out var byMail))
                continue;

            progress.Gave = (byte)Math.Min(byte.MaxValue, progress.Gave + 1);
            progress.Cumulated = 0;
            progress.Updated = now;
            Persist(character.AccountId, def.Id, progress);
            Logger.Info(
                "Schedule item auto-granted account={0} id={1} item={2} x{3} byMail={4}",
                character.AccountId,
                def.Id,
                def.ItemId,
                def.ItemCount,
                byMail);
        }
    }

    public void TickOnline(GameConnection connection)
    {
        var character = connection?.ActiveChar;
        if (character == null)
            return;

        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var def in ScheduleItemGameData.Instance.ActiveOnAir(now))
        {
            if (def.GiveTerm <= 0 || !IsEligible(character, def))
                continue;
            var progress = Load(character.AccountId, def.Id, now);
            var addSeconds = (int)Math.Max(0, (now - progress.LastOnlineTick).TotalSeconds);
            progress.LastOnlineTick = now;
            if (addSeconds <= 0)
                continue;
            var next = ScheduleItemRules.TickCumulated(progress.Cumulated, def.GiveTerm, addSeconds);
            if (next == progress.Cumulated)
                continue;
            progress.Cumulated = next;
            progress.Updated = now;
            Persist(character.AccountId, def.Id, progress);
            changed = true;
        }

        if (changed)
            SendActive(character);
    }

    public void HandleTake(Character character, int scheduleId)
    {
        if (character == null)
            return;

        var def = ScheduleItemGameData.Instance.Get(scheduleId);
        var now = DateTime.UtcNow;
        if (def is not { OnAir: true, ActiveTake: true } || !def.IsOnAir(now) || def.ItemId == 0 || def.ItemCount <= 0)
            return;
        if (!IsEligible(character, def))
            return;

        var progress = Load(character.AccountId, scheduleId, now);
        if (!ScheduleItemRules.CanTake(progress.Gave, def.GiveMax, progress.Cumulated, def.GiveTerm))
            return;

        if (!TryGrant(character, def, out var byMail))
            return;

        progress.Gave = (byte)Math.Min(byte.MaxValue, progress.Gave + 1);
        progress.Cumulated = 0;
        progress.Updated = now;
        Persist(character.AccountId, scheduleId, progress);
        character.SendPacket(new SCScheduleItemSentPacket(scheduleId, byMail));
        SendActive(character);
        Logger.Info(
            "Schedule item claimed account={0} id={1} gave={2}/{3} byMail={4}",
            character.AccountId,
            scheduleId,
            progress.Gave,
            def.GiveMax,
            byMail);
    }

    private static bool IsEligible(Character character, ScheduleItemDef def)
    {
        var memberships = AccountMemberships.ActiveIds(character.AccountId, AppConfiguration.Instance.Id);
        return AccountPatronRules.IsScheduleEligible(
            def.Kind,
            def.KindValue,
            character.PremiumGrade,
            memberships,
            AccountPatron.PaidFloorGradeId,
            isPcBang: false);
    }

    private static ScheduleItem ToWire(int scheduleId, Progress progress) =>
        new()
        {
            Type = scheduleId,
            Gave = progress.Gave,
            Cumulated = progress.Cumulated,
            Updated = progress.Updated
        };

    private static bool TryGrant(Character character, ScheduleItemDef def, out bool byMail)
    {
        byMail = false;
        if (character.Inventory.Bag.SpaceLeftForItem(def.ItemId) >= def.ItemCount)
        {
            return character.Inventory.Bag.AcquireDefaultItem(
                ItemTaskType.TakeScheduleItem,
                def.ItemId,
                def.ItemCount);
        }

        if (string.IsNullOrEmpty(def.MailTitle) || string.IsNullOrEmpty(def.MailBody))
            return false;

        var mail = new BaseMail
        {
            MailType = MailType.Promotion,
            Title = def.MailTitle,
            ReceiverName = character.Name,
            Header =
            {
                SenderName = def.MailTitle,
                ReceiverId = character.Id
            },
            Body =
            {
                Text = def.MailBody,
                SendDate = DateTime.UtcNow,
                RecvDate = DateTime.UtcNow
            }
        };

        if (!character.Inventory.MailAttachments.AcquireDefaultItemEx(
                ItemTaskType.Invalid,
                def.ItemId,
                def.ItemCount,
                -1,
                out var added,
                out _,
                character.Id))
            return false;

        mail.Body.Attachments.AddRange(added);
        if (!mail.Send())
            return false;
        byMail = true;
        return true;
    }

    private Progress Load(uint accountId, int scheduleId, DateTime utcNow)
    {
        var key = (accountId, scheduleId);
        if (_progress.TryGetValue(key, out var cached))
        {
            ResetIfNeeded(cached, utcNow);
            return cached;
        }

        var progress = new Progress { Updated = utcNow, LastOnlineTick = utcNow };
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT gave, cumulated, updated
                FROM account_schedule_items
                WHERE account_id = @account AND schedule_id = @schedule
                """;
            command.Parameters.AddWithValue("@account", accountId);
            command.Parameters.AddWithValue("@schedule", scheduleId);
            command.Prepare();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                progress.Gave = reader.GetByte("gave");
                progress.Cumulated = reader.GetInt64("cumulated");
                progress.Updated = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64("updated")).UtcDateTime;
            }
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn(
                "Schedule item table missing — run SQL/updates/2026-09-06_aaemu_game_account_schedule_items.sql ({0})",
                ex.Message);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Schedule item Load failed account={0} id={1}", accountId, scheduleId);
        }

        ResetIfNeeded(progress, utcNow);
        _progress[key] = progress;
        return progress;
    }

    private static void ResetIfNeeded(Progress progress, DateTime utcNow)
    {
        if (!ScheduleItemRules.NeedsDailyReset(progress.Updated, utcNow))
            return;
        progress.Gave = 0;
        progress.Cumulated = 0;
        progress.Updated = utcNow;
        progress.LastOnlineTick = utcNow;
    }

    private void Persist(uint accountId, int scheduleId, Progress progress)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                REPLACE INTO account_schedule_items
                    (account_id, schedule_id, gave, cumulated, updated)
                VALUES
                    (@account, @schedule, @gave, @cumulated, @updated)
                """;
            command.Parameters.AddWithValue("@account", accountId);
            command.Parameters.AddWithValue("@schedule", scheduleId);
            command.Parameters.AddWithValue("@gave", progress.Gave);
            command.Parameters.AddWithValue("@cumulated", progress.Cumulated);
            command.Parameters.AddWithValue("@updated", Helpers.UnixTime(progress.Updated));
            command.Prepare();
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn("Schedule item Persist skipped (table missing): account={0} id={1}", accountId, scheduleId);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Schedule item Persist failed account={0} id={1}", accountId, scheduleId);
        }
    }
}
