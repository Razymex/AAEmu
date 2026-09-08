using AAEmu.Commons.Network;
using AAEmu.Game.GameData;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>Wire helpers, status gates, and week/mission limits for Arche Pass.</summary>
public static class ArchePassRules
{
    /// <summary>Client list packet clamps <c>count</c> to this.</summary>
    public const int MaxListedPasses = 10;

    public const string ConfigMissionCompleteCount = "arche_pass_mission_complete_count";
    public const string ConfigMissionChangeCount = "arche_pass_mission_change_count";
    public const string ConfigResetWeeklyDay = "arche_pass_reset_weekly_day";

    public static int MissionCompleteMax =>
        ContentConfigGameData.Instance.GetInt(ConfigMissionCompleteCount, 20);

    public static int MissionChangeMax =>
        ContentConfigGameData.Instance.GetInt(ConfigMissionChangeCount, 6);

    /// <summary>
    /// Compact weekday for the weekly mission reset. <c>1</c> is Monday, matching
    /// <see cref="DayOfWeek.Monday"/>.
    /// </summary>
    public static int ResetWeekday =>
        ContentConfigGameData.Instance.GetInt(ConfigResetWeeklyDay, 1);

    public static bool CanBuy(ArchePassStatus status) =>
        status is ArchePassStatus.Invalid or ArchePassStatus.Dropped;

    public static bool CanStart(ArchePassStatus status) =>
        status == ArchePassStatus.Owned;

    public static bool CanRemove(ArchePassStatus status) =>
        status is ArchePassStatus.Owned or ArchePassStatus.Progress;

    public static bool CanUpgrade(ArchePassStatus status, bool premium, uint upgradeItemId) =>
        status == ArchePassStatus.Progress && !premium && upgradeItemId != 0;

    public static bool CanComplete(ArchePassStatus status, bool premium, uint lastRewardTier, uint maxTier) =>
        status == ArchePassStatus.Progress && !premium && maxTier != 0 && lastRewardTier >= maxTier;

    public static bool CanChangeMission(int used, int max) =>
        used < max;

    public static bool CanClaimLastPremium(uint lastRewardTier, uint maxTier) =>
        maxTier == 0 || lastRewardTier >= maxTier;

    public static DateTime WeekStartUtc(DateTime utc)
    {
        var day = utc.Kind == DateTimeKind.Utc ? utc.Date : DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
        var target = (DayOfWeek)(ResetWeekday % 7);
        var offset = ((int)day.DayOfWeek - (int)target + 7) % 7;
        return DateTime.SpecifyKind(day.AddDays(-offset), DateTimeKind.Utc);
    }

    public static IReadOnlyList<(uint Idx, ulong Body)> PackCompleted(IEnumerable<uint> passIds)
    {
        var words = new SortedDictionary<uint, ulong>();
        if (passIds == null)
            return [];

        foreach (var passId in passIds)
        {
            var idx = passId >> 6;
            words.TryGetValue(idx, out var body);
            words[idx] = body | (1UL << (int)(passId & 63));
        }

        return words.Select(pair => (pair.Key, pair.Value)).ToList();
    }

    public static bool IsCompleted(IEnumerable<(uint Idx, ulong Body)> words, uint passId)
    {
        if (words == null)
            return false;
        var idx = passId >> 6;
        var bit = passId & 63;
        foreach (var (wordIdx, body) in words)
        {
            if (wordIdx == idx)
                return ((body >> (int)bit) & 1UL) != 0;
        }

        return false;
    }

    public static void WriteRow(PacketStream stream, ArchePassProgress row)
    {
        row ??= new ArchePassProgress();
        stream.Write(row.PassId);
        stream.Write(row.LastRewardTier);
        stream.Write(row.LastPremiumRewardTier);
        stream.Write(row.Point);
        stream.Write(row.Premium);
        stream.Write((byte)row.Status);
    }

    public static IReadOnlyList<ArchePassProgress> RowsForWire(IReadOnlyList<ArchePassProgress> rows)
    {
        if (rows == null || rows.Count == 0)
            return [];
        if (rows.Count <= MaxListedPasses)
            return rows;
        return rows.Take(MaxListedPasses).ToList();
    }
}
