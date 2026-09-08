using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Features;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;

using MySql.Data.MySqlClient;

using NLog;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>Owned / in-progress Arche Passes and weekly mission counters for one character.</summary>
public sealed class CharacterArchePass
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly object _sync = new();
    private readonly Dictionary<uint, ArchePassProgress> _passes = [];
    private int _missionCompleteUsed;
    private int _missionChangeUsed;
    private DateTime _missionWeekStart = DateTime.MinValue;

    public CharacterArchePass()
    {
    }

    public CharacterArchePass(Character owner)
    {
        Owner = owner;
    }

    public Character Owner { get; private set; }

    /// <summary>Tests skip currency, item consume, item grant, and MySQL.</summary>
    public bool BypassChargesForTests { get; set; }

    /// <summary>Tests force the next persist to fail and then clear this flag.</summary>
    public bool FailNextPersist { get; set; }

    public void Bind(Character owner) => Owner = owner;

    public IReadOnlyList<ArchePassProgress> Snapshot()
    {
        lock (_sync)
            return _passes.Values.OrderBy(pass => pass.PassId).Select(pass => pass.Clone()).ToList();
    }

    public ArchePassStatus StatusOf(uint passId)
    {
        lock (_sync)
            return _passes.TryGetValue(passId, out var row) ? row.Status : ArchePassStatus.Invalid;
    }

    public bool HasProgress(uint passId = 0)
    {
        lock (_sync)
        {
            if (passId != 0)
                return _passes.TryGetValue(passId, out var row) && row.Status == ArchePassStatus.Progress;
            return _passes.Values.Any(row => row.Status == ArchePassStatus.Progress);
        }
    }

    public bool HasPremium(uint passId = 0)
    {
        lock (_sync)
        {
            if (passId != 0)
            {
                return _passes.TryGetValue(passId, out var row)
                       && row.Status == ArchePassStatus.Progress
                       && row.Premium;
            }

            return _passes.Values.Any(row => row.Status == ArchePassStatus.Progress && row.Premium);
        }
    }

    public (int Used, int Max) MissionCompleteCounts()
    {
        EnsureMissionWeek();
        lock (_sync)
            return (_missionCompleteUsed, ArchePassRules.MissionCompleteMax);
    }

    public (int Used, int Max) MissionChangeCounts()
    {
        EnsureMissionWeek();
        lock (_sync)
            return (_missionChangeUsed, ArchePassRules.MissionChangeMax);
    }

    public bool TryBuy(uint passId)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        if (!ArchePassGameData.Instance.TryGet(passId, out var desc))
        {
            Logger.Warn("ArchePass buy {0}: unknown type {1}", Owner.Name, passId);
            return false;
        }

        ArchePassProgress row;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            var current = _passes.TryGetValue(passId, out var existing)
                ? existing.Status
                : ArchePassStatus.Invalid;
            if (!ArchePassRules.CanBuy(current))
            {
                Logger.Info("ArchePass buy {0}: type {1} status {2}", Owner.Name, passId, current);
                return false;
            }

            var listed = _passes.Values.Count(pass =>
                pass.PassId != passId &&
                pass.Status is ArchePassStatus.Owned or ArchePassStatus.Progress);
            if (listed >= ArchePassRules.MaxListedPasses)
            {
                Logger.Info("ArchePass buy {0}: list full", Owner.Name);
                return false;
            }

            snapshot = ClonePasses();
            row = existing ?? new ArchePassProgress { PassId = passId };
            row.PassId = passId;
            row.Status = ArchePassStatus.Owned;
            row.Point = 0;
            row.Premium = false;
            row.LastRewardTier = 0;
            row.LastPremiumRewardTier = 0;
            _passes[passId] = row;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }

        if (!BypassChargesForTests &&
            !Owner.TryPayCurrency(desc.CurrencyId, desc.CurrencyValue, false, ItemTaskType.ArchePassBuy))
        {
            RestorePasses(snapshot);
            TryPersist();
            return false;
        }

        SendUpdate(row, ArchePassUpdateReason.Buy);
        Logger.Info("ArchePass buy {0}: type={1} name={2} currency={3} value={4}",
            Owner.Name, passId, desc.Name, desc.CurrencyId, desc.CurrencyValue);
        return true;
    }

    public bool TryStart(uint passId)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        if (!ArchePassGameData.Instance.TryGet(passId, out var desc))
        {
            Logger.Warn("ArchePass start {0}: unknown type {1}", Owner.Name, passId);
            return false;
        }

        ArchePassProgress row;
        ArchePassProgress previous = null;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            if (!_passes.TryGetValue(passId, out row) || !ArchePassRules.CanStart(row.Status))
            {
                Logger.Info("ArchePass start {0}: type {1} status {2}", Owner.Name, passId, StatusOf(passId));
                return false;
            }

            snapshot = ClonePasses();
            foreach (var pass in _passes.Values)
            {
                if (pass.PassId == passId || pass.Status != ArchePassStatus.Progress)
                    continue;
                pass.Status = ArchePassStatus.Owned;
                previous = pass;
            }

            row.Status = ArchePassStatus.Progress;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }
        if (previous != null)
        {
            SendUpdate(previous, ArchePassUpdateReason.Owned);
            Logger.Info("ArchePass start {0}: parked type={1}", Owner.Name, previous.PassId);
        }

        SendUpdate(row, ArchePassUpdateReason.Started);
        Logger.Info("ArchePass start {0}: type={1} name={2}", Owner.Name, passId, desc.Name);
        return true;
    }

    public bool TryRemove(uint passId)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        if (!ArchePassGameData.Instance.TryGet(passId, out var desc))
        {
            Logger.Warn("ArchePass remove {0}: unknown type {1}", Owner.Name, passId);
            return false;
        }

        ArchePassProgress row;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            if (!_passes.TryGetValue(passId, out row) || !ArchePassRules.CanRemove(row.Status))
            {
                Logger.Info("ArchePass remove {0}: type {1} status {2}", Owner.Name, passId, StatusOf(passId));
                return false;
            }

            snapshot = ClonePasses();
            row.Status = ArchePassStatus.Dropped;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }
        SendUpdate(row, ArchePassUpdateReason.Dropped);
        Logger.Info("ArchePass remove {0}: type={1} name={2}", Owner.Name, passId, desc.Name);
        return true;
    }

    public bool TryUpgrade()
    {
        if (!FeatureOn() || Owner == null)
            return false;

        ArchePassProgress row;
        ArchePassDesc desc;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            row = LiveProgress();
            if (row == null || !ArchePassGameData.Instance.TryGet(row.PassId, out desc))
            {
                Logger.Info("ArchePass upgrade {0}: no in-progress pass", Owner.Name);
                return false;
            }

            if (!ArchePassRules.CanUpgrade(row.Status, row.Premium, desc.UpgradeItemId))
            {
                Logger.Info("ArchePass upgrade {0}: type {1} refused", Owner.Name, row.PassId);
                return false;
            }

            snapshot = ClonePasses();
            row.Premium = true;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }

        if (!BypassChargesForTests &&
            !TryConsume(desc.UpgradeItemId, 1, ItemTaskType.ArchePassUpgrade))
        {
            RestorePasses(snapshot);
            TryPersist();
            return false;
        }
        SendUpdate(row, ArchePassUpdateReason.UpgradePremium);
        Logger.Info("ArchePass upgrade {0}: type={1} item={2}", Owner.Name, row.PassId, desc.UpgradeItemId);
        return true;
    }

    public bool TryComplete(uint passId)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        if (!ArchePassGameData.Instance.TryGet(passId, out var desc))
        {
            Logger.Warn("ArchePass complete {0}: unknown type {1}", Owner.Name, passId);
            return false;
        }

        var maxTier = ArchePassGameData.Instance.MaxTier(passId);
        ArchePassProgress row;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            if (!_passes.TryGetValue(passId, out row)
                || !ArchePassRules.CanComplete(row.Status, row.Premium, row.LastRewardTier, maxTier))
            {
                Logger.Info("ArchePass complete {0}: type {1} refused", Owner.Name, passId);
                return false;
            }

            snapshot = ClonePasses();
            row.Status = ArchePassStatus.Completed;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }
        SendUpdate(row, ArchePassUpdateReason.Completed, allDone: false);
        SendCompletedWord(passId);
        Logger.Info("ArchePass complete {0}: type={1} name={2}", Owner.Name, passId, desc.Name);
        return true;
    }

    public bool TryClaim(uint tier, bool premium)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        ArchePassProgress row;
        ArchePassTierDesc reward;
        uint maxTier;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            row = LiveProgress();
            if (row == null)
            {
                Logger.Info("ArchePass claim {0}: no in-progress pass", Owner.Name);
                return false;
            }

            if (premium && !row.Premium)
            {
                Logger.Info("ArchePass claim {0}: type {1} is not premium", Owner.Name, row.PassId);
                return false;
            }

            var last = premium ? row.LastPremiumRewardTier : row.LastRewardTier;
            if (!ArchePassGameData.Instance.TryNextReward(row.PassId, row.Point, last, premium, out reward)
                || reward.Tier != tier)
            {
                Logger.Info("ArchePass claim {0}: type {1} tier {2} premium={3} is not next",
                    Owner.Name, row.PassId, tier, premium);
                return false;
            }

            maxTier = ArchePassGameData.Instance.MaxTier(row.PassId);
            if (premium && reward.Tier == maxTier
                && !ArchePassRules.CanClaimLastPremium(row.LastRewardTier, maxTier))
            {
                Logger.Info("ArchePass claim {0}: take the free last reward first", Owner.Name);
                return false;
            }

            snapshot = ClonePasses();
            if (premium)
                row.LastPremiumRewardTier = reward.Tier;
            else
                row.LastRewardTier = reward.Tier;

            if (premium && reward.Tier == maxTier && row.LastRewardTier >= maxTier)
                row.Status = ArchePassStatus.Completed;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }

        var itemId = premium ? reward.PremiumRewardItemId : reward.RewardItemId;
        var count = premium ? reward.PremiumRewardItemCount : reward.RewardItemCount;
        if (!BypassChargesForTests && !TryGrant(itemId, count, ItemTaskType.ArchePassReward))
        {
            RestorePasses(snapshot);
            TryPersist();
            return false;
        }
        if (row.Status == ArchePassStatus.Completed)
        {
            SendUpdate(row, ArchePassUpdateReason.Completed, allDone: true);
            SendCompletedWord(row.PassId);
        }
        else
        {
            var lastFree = !premium && reward.Tier == maxTier;
            SendUpdate(row, ArchePassUpdateReason.RewardItem, allDone: lastFree);
        }

        Logger.Info("ArchePass claim {0}: type={1} tier={2} premium={3}",
            Owner.Name, row.PassId, reward.Tier, premium);
        return true;
    }

    public bool TryAddPoints(int amount)
    {
        if (!FeatureOn() || Owner == null || amount <= 0)
            return false;

        ArchePassProgress row;
        Dictionary<uint, ArchePassProgress> snapshot;
        lock (_sync)
        {
            row = LiveProgress();
            if (row == null)
            {
                Logger.Info("ArchePass add-point {0}: no in-progress pass", Owner.Name);
                return false;
            }

            snapshot = ClonePasses();
            row.Point += amount;
        }

        if (!TryPersist())
        {
            RestorePasses(snapshot);
            return false;
        }
        SendUpdate(row, ArchePassUpdateReason.Point, diffPoint: amount);
        Logger.Info("ArchePass add-point {0}: type={1} +{2} total={3}",
            Owner.Name, row.PassId, amount, row.Point);
        return true;
    }

    public bool TryChangeMission(uint realStep)
    {
        if (!FeatureOn() || Owner == null || realStep == 0)
            return false;

        EnsureMissionWeek();
        int used;
        int max;
        int previousUsed;
        DateTime previousWeek;
        lock (_sync)
        {
            used = _missionChangeUsed;
            max = ArchePassRules.MissionChangeMax;
            if (!ArchePassRules.CanChangeMission(used, max))
            {
                Logger.Info("ArchePass change-mission {0}: {1}/{2}", Owner.Name, used, max);
                return false;
            }

            previousUsed = used;
            previousWeek = _missionWeekStart;
            _missionChangeUsed = used + 1;
        }

        if (!TryPersistMissions())
        {
            lock (_sync)
                _missionChangeUsed = previousUsed;
            return false;
        }

        if (!TodayAssignmentManager.Instance.TryRerollProgress(Owner, realStep, requireArchePassBoard: true))
        {
            lock (_sync)
            {
                _missionChangeUsed = previousUsed;
                _missionWeekStart = previousWeek;
            }

            TryPersistMissions();
            return false;
        }
        Owner.SendPacket(new SCArchePassChangeMissionPacket((uint)_missionChangeUsed));
        Logger.Info("ArchePass change-mission {0}: realStep={1} used={2}/{3}",
            Owner.Name, realStep, _missionChangeUsed, max);
        return true;
    }

    public void NotifyMissionCompleted()
    {
        if (!FeatureOn() || Owner == null)
            return;

        EnsureMissionWeek();
        var max = ArchePassRules.MissionCompleteMax;
        int previousUsed;
        lock (_sync)
        {
            if (_missionCompleteUsed >= max)
                return;
            previousUsed = _missionCompleteUsed;
            _missionCompleteUsed++;
        }

        if (!TryPersistMissions())
        {
            lock (_sync)
                _missionCompleteUsed = previousUsed;
            return;
        }
        Owner.SendPacket(new SCArchePassMissionCountPacket((uint)_missionCompleteUsed));
        Logger.Info("ArchePass mission-complete {0}: used={1}/{2}", Owner.Name, _missionCompleteUsed, max);
    }

    public void SendLogin()
    {
        if (!FeatureOn() || Owner == null)
            return;
        EnsureMissionWeek();
        SendList(last: true);
        SendCompletedList();
        Owner.SendPacket(new SCArchePassMissionCountPacket((uint)_missionCompleteUsed));
        Owner.SendPacket(new SCArchePassChangeMissionPacket((uint)_missionChangeUsed));
    }

    public void Load(MySqlConnection connection)
    {
        if (Owner == null)
            return;

        try
        {
            using var command = connection.CreateCommand();
            command.Connection = connection;
            command.CommandText =
                "SELECT `pass_id`,`status`,`point`,`premium`,`last_reward_tier`,`last_premium_reward_tier` " +
                "FROM character_arche_passes WHERE `owner` = @owner";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            using var reader = command.ExecuteReader();
            lock (_sync)
            {
                _passes.Clear();
                while (reader.Read())
                {
                    var passId = reader.GetUInt32("pass_id");
                    _passes[passId] = new ArchePassProgress
                    {
                        PassId = passId,
                        Status = (ArchePassStatus)reader.GetByte("status"),
                        Point = reader.GetInt64("point"),
                        Premium = reader.GetBoolean("premium"),
                        LastRewardTier = reader.GetUInt32("last_reward_tier"),
                        LastPremiumRewardTier = reader.GetUInt32("last_premium_reward_tier")
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "ArchePass load skipped for {0}", Owner.Name);
        }

        LoadMissions(connection);
    }

    public void Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        if (Owner == null || Owner.Id == 0)
            return;

        try
        {
            Persist(connection, transaction);
            PersistMissions(connection, transaction);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "ArchePass save skipped for {0}", Owner.Name);
        }
    }

    private ArchePassProgress LiveProgress()
    {
        return _passes.Values
            .Where(pass => pass.Status == ArchePassStatus.Progress)
            .OrderBy(pass => pass.PassId)
            .FirstOrDefault();
    }

    private void SendList(bool last)
    {
        Owner?.SendPacket(new SCArchePassesPacket(Snapshot(), last));
    }

    private void SendUpdate(ArchePassProgress row, ArchePassUpdateReason reason, int diffPoint = 0, bool allDone = false)
    {
        Owner?.SendPacket(new SCUpdateArchePassPacket(row.Clone(), (byte)reason, diffPoint, allDone));
    }

    private void SendCompletedList()
    {
        var words = PackCompleted();
        Owner?.SendPacket(new SCCompletedArchePassesPacket(words));
    }

    private void SendCompletedWord(uint passId)
    {
        var words = PackCompleted();
        var idx = passId >> 6;
        ulong body = 0;
        foreach (var (wordIdx, wordBody) in words)
        {
            if (wordIdx != idx)
                continue;
            body = wordBody;
            break;
        }

        Owner?.SendPacket(new SCCompletedArchePassPacket((int)idx, (long)body));
    }

    private IReadOnlyList<(uint Idx, ulong Body)> PackCompleted()
    {
        lock (_sync)
        {
            return ArchePassRules.PackCompleted(
                _passes.Values.Where(pass => pass.Status == ArchePassStatus.Completed).Select(pass => pass.PassId));
        }
    }

    private bool FeatureOn()
    {
        var features = FeaturesManager.Fsets;
        return features == null || features.Check(Feature.arche_pass);
    }

    private bool TryConsume(uint templateId, int count, ItemTaskType task)
    {
        if (count <= 0 || templateId == 0)
            return false;
        if (!Owner.Inventory.CheckItems(SlotType.Inventory, templateId, count))
        {
            Owner.SendErrorMessage(ErrorMessageType.NotEnoughRequiredItem);
            return false;
        }

        return Owner.Inventory.Bag.ConsumeItem(task, templateId, count, null) == count;
    }

    private bool TryGrant(uint templateId, int count, ItemTaskType task)
    {
        if (count <= 0 || templateId == 0)
            return false;
        if (!Owner.Inventory.Bag.AcquireDefaultItem(task, templateId, count))
        {
            Owner.SendErrorMessage(ErrorMessageType.BagFull);
            return false;
        }

        return true;
    }

    private void EnsureMissionWeek()
    {
        var week = ArchePassRules.WeekStartUtc(ServerCalendar.UtcNow);
        lock (_sync)
        {
            if (_missionWeekStart == week)
                return;
            _missionWeekStart = week;
            _missionCompleteUsed = 0;
            _missionChangeUsed = 0;
        }
    }

    private Dictionary<uint, ArchePassProgress> ClonePasses() =>
        _passes.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());

    private void RestorePasses(Dictionary<uint, ArchePassProgress> snapshot)
    {
        lock (_sync)
        {
            _passes.Clear();
            foreach (var pair in snapshot)
                _passes[pair.Key] = pair.Value.Clone();
        }
    }

    private bool TryPersist()
    {
        if (FailNextPersist)
        {
            FailNextPersist = false;
            return false;
        }

        if (Owner == null || BypassChargesForTests || Owner.Id == 0)
            return true;

        using var connection = MySQL.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            Persist(connection, transaction);
            PersistMissions(connection, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "ArchePass persist failed for {0}", Owner.Name);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollback)
            {
                Logger.Fatal(rollback, "ArchePass persist rollback failed for {0}", Owner.Name);
            }

            return false;
        }
    }

    private bool TryPersistMissions()
    {
        if (FailNextPersist)
        {
            FailNextPersist = false;
            return false;
        }

        if (Owner == null || BypassChargesForTests || Owner.Id == 0)
            return true;

        using var connection = MySQL.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            PersistMissions(connection, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "ArchePass mission persist failed for {0}", Owner.Name);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollback)
            {
                Logger.Fatal(rollback, "ArchePass mission persist rollback failed for {0}", Owner.Name);
            }

            return false;
        }
    }

    private void Persist(MySqlConnection connection, MySqlTransaction transaction)
    {
        List<ArchePassProgress> snapshot;
        lock (_sync)
            snapshot = _passes.Values.Select(pass => pass.Clone()).ToList();

        using (var command = connection.CreateCommand())
        {
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM character_arche_passes WHERE `owner` = @owner";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.ExecuteNonQuery();
        }

        foreach (var row in snapshot)
        {
            using var command = connection.CreateCommand();
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO character_arche_passes " +
                "(`owner`,`pass_id`,`status`,`point`,`premium`,`last_reward_tier`,`last_premium_reward_tier`) " +
                "VALUES (@owner,@pass,@status,@point,@premium,@lastReward,@lastPremium)";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.Parameters.AddWithValue("@pass", row.PassId);
            command.Parameters.AddWithValue("@status", (byte)row.Status);
            command.Parameters.AddWithValue("@point", row.Point);
            command.Parameters.AddWithValue("@premium", row.Premium);
            command.Parameters.AddWithValue("@lastReward", row.LastRewardTier);
            command.Parameters.AddWithValue("@lastPremium", row.LastPremiumRewardTier);
            command.ExecuteNonQuery();
        }
    }

    private void LoadMissions(MySqlConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.Connection = connection;
            command.CommandText =
                "SELECT `complete_used`,`change_used`,`week_start` " +
                "FROM character_arche_pass_missions WHERE `owner` = @owner";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                EnsureMissionWeek();
                return;
            }

            var week = ServerCalendar.AsUtc(reader.GetDateTime("week_start")).Date;
            lock (_sync)
            {
                _missionWeekStart = DateTime.SpecifyKind(week, DateTimeKind.Utc);
                _missionCompleteUsed = reader.GetInt32("complete_used");
                _missionChangeUsed = reader.GetInt32("change_used");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "ArchePass mission load skipped for {0}", Owner.Name);
        }

        EnsureMissionWeek();
    }

    private void PersistMissions(MySqlConnection connection, MySqlTransaction transaction)
    {
        int complete;
        int change;
        DateTime week;
        lock (_sync)
        {
            complete = _missionCompleteUsed;
            change = _missionChangeUsed;
            week = _missionWeekStart;
        }

        if (week == DateTime.MinValue)
            week = ArchePassRules.WeekStartUtc(ServerCalendar.UtcNow);

        using var command = connection.CreateCommand();
        command.Connection = connection;
        command.Transaction = transaction;
        command.CommandText =
            "REPLACE INTO character_arche_pass_missions " +
            "(`owner`,`complete_used`,`change_used`,`week_start`) " +
            "VALUES (@owner,@complete,@change,@week)";
        command.Parameters.AddWithValue("@owner", Owner.Id);
        command.Parameters.AddWithValue("@complete", complete);
        command.Parameters.AddWithValue("@change", change);
        command.Parameters.AddWithValue("@week", week.Date);
        command.ExecuteNonQuery();
    }
}
