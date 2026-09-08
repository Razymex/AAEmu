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
            if (!TryLoad(character.AccountId, def.Id, now, out var progress))
                continue;
            items.Add(ToWire(def.Id, progress));
            if (items.Count >= ScheduleItemRules.MaxItemsInPacket)
                break;
        }

        character.SendPacket(new SCScheduleItemUpdatePacket(items));
        AutoGrantSilent(character, now);
        Logger.Info("Schedule items sent {0} count={1}", character.Name, items.Count);
    }

    private void AutoGrantSilent(Character character, DateTime now)
    {
        foreach (var def in ScheduleItemGameData.Instance.SilentOnAir(now))
        {
            if (!IsEligible(character, def))
                continue;

            if (!TryLoad(character.AccountId, def.Id, now, out var progress))
                continue;
            if (!ScheduleItemRules.ShouldAutoGrant(def.Kind, def.ActiveTake, def.GiveTerm, progress.Gave, def.GiveMax))
                continue;

            var previousGave = progress.Gave;
            var previousCumulated = progress.Cumulated;
            var previousUpdated = progress.Updated;
            progress.Gave = (byte)Math.Min(byte.MaxValue, progress.Gave + 1);
            progress.Cumulated = 0;
            progress.Updated = now;
            if (!TryPersist(character.AccountId, def.Id, progress))
            {
                progress.Gave = previousGave;
                progress.Cumulated = previousCumulated;
                progress.Updated = previousUpdated;
                continue;
            }

            if (!TryGrant(character, def, out var byMail, out var deliveredAny))
            {
                if (deliveredAny)
                {
                    Logger.Warn(
                        "Schedule item leftover grant failed after a partial delivery id={0} name={1}",
                        def.Id,
                        character.Name);
                    continue;
                }

                RevertGave(progress, previousGave, previousCumulated, previousUpdated, character.AccountId, def.Id);
                continue;
            }
            Logger.Info(
                "Schedule item auto-granted {0} id={1} item={2} x{3} byMail={4}",
                character.Name,
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
            if (!TryLoad(character.AccountId, def.Id, now, out var progress))
                continue;
            var addSeconds = (int)Math.Max(0, (now - progress.LastOnlineTick).TotalSeconds);
            progress.LastOnlineTick = now;
            if (addSeconds <= 0)
                continue;
            var next = ScheduleItemRules.TickCumulated(progress.Cumulated, def.GiveTerm, addSeconds);
            if (next == progress.Cumulated)
                continue;
            var previousCumulated = progress.Cumulated;
            var previousUpdated = progress.Updated;
            progress.Cumulated = next;
            progress.Updated = now;
            if (!TryPersist(character.AccountId, def.Id, progress))
            {
                progress.Cumulated = previousCumulated;
                progress.Updated = previousUpdated;
                continue;
            }
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

        if (!TryLoad(character.AccountId, scheduleId, now, out var progress))
            return;
        if (!ScheduleItemRules.CanTake(progress.Gave, def.GiveMax, progress.Cumulated, def.GiveTerm))
            return;

        var previousGave = progress.Gave;
        var previousCumulated = progress.Cumulated;
        var previousUpdated = progress.Updated;
        progress.Gave = (byte)Math.Min(byte.MaxValue, progress.Gave + 1);
        progress.Cumulated = 0;
        progress.Updated = now;
        if (!TryPersist(character.AccountId, scheduleId, progress))
        {
            progress.Gave = previousGave;
            progress.Cumulated = previousCumulated;
            progress.Updated = previousUpdated;
            return;
        }

        if (!TryGrant(character, def, out var byMail, out var deliveredAny))
        {
            if (deliveredAny)
            {
                Logger.Warn(
                    "Schedule item leftover grant failed after a partial delivery id={0} name={1}",
                    scheduleId,
                    character.Name);
                return;
            }

            RevertGave(progress, previousGave, previousCumulated, previousUpdated, character.AccountId, scheduleId);
            return;
        }

        character.SendPacket(new SCScheduleItemSentPacket(scheduleId, byMail));
        SendActive(character);
        Logger.Info(
            "Schedule item claimed {0} id={1} gave={2}/{3} byMail={4}",
            character.Name,
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

    private static void RevertGave(
        Progress progress,
        byte previousGave,
        long previousCumulated,
        DateTime previousUpdated,
        uint accountId,
        int scheduleId)
    {
        var committedGave = progress.Gave;
        var committedCumulated = progress.Cumulated;
        var committedUpdated = progress.Updated;
        progress.Gave = previousGave;
        progress.Cumulated = previousCumulated;
        progress.Updated = previousUpdated;
        var rollbackOk = TryPersist(accountId, scheduleId, progress);
        if (!DurableRewardRules.KeepClaim(false, false, rollbackOk))
            return;

        progress.Gave = committedGave;
        progress.Cumulated = committedCumulated;
        progress.Updated = committedUpdated;
        Logger.Error("Schedule item grant failed and the compensating write did not land id={0}", scheduleId);
    }

    private static bool TryGrant(Character character, ScheduleItemDef def, out bool byMail, out bool deliveredAny)
    {
        byMail = false;
        deliveredAny = false;
        if (character.Inventory.Bag.SpaceLeftForItem(def.ItemId) >= def.ItemCount)
        {
            if (!character.Inventory.Bag.AcquireDefaultItem(
                    ItemTaskType.TakeScheduleItem,
                    def.ItemId,
                    def.ItemCount))
                return false;
            deliveredAny = true;
            return true;
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
        {
            if (!MailDeliveryRules.TryDiscardStagedAttachments(character.Inventory.MailAttachments, added))
                deliveredAny = true;
            return false;
        }

        deliveredAny = true;
        byMail = true;
        return true;
    }

    private bool TryLoad(uint accountId, int scheduleId, DateTime utcNow, out Progress progress)
    {
        var key = (accountId, scheduleId);
        if (_progress.TryGetValue(key, out progress))
        {
            ResetIfNeeded(progress, utcNow);
            return true;
        }

        progress = new Progress { Updated = utcNow, LastOnlineTick = utcNow };
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
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Schedule item Load failed id={0}", scheduleId);
            return false;
        }

        ResetIfNeeded(progress, utcNow);
        _progress[key] = progress;
        return true;
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

    private static bool TryPersist(uint accountId, int scheduleId, Progress progress)
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
            return true;
        }
        catch (MySqlException ex) when (ex.Number is 1146 or 1054)
        {
            Logger.Warn("Schedule item Persist skipped (table missing) id={0}", scheduleId);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Schedule item Persist failed id={0}", scheduleId);
            return false;
        }
    }
}
