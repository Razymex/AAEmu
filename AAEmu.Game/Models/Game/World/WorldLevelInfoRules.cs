namespace AAEmu.Game.Models.Game.World;

/// <summary>
/// <c>SCWorldLevelInfo</c> body. Field names match the client reader.
/// </summary>
public readonly record struct WorldLevelInfoWire(
    uint WorldLevel,
    uint ServerDays,
    uint ExpModifier,
    uint HardCapLevel,
    uint CharacterLevel,
    int LevelDiff);

public readonly record struct WorldLevelHardCapRow(
    int MinServerDays,
    int MaxServerDays,
    int HardCapLevel,
    bool GetQuest);

public readonly record struct WorldLevelExpModifierRow(
    int LevelDiffMin,
    int LevelDiffMax,
    uint ExpModifier);

/// <summary>
/// World-level HUD body and the server-open stamp the client uses for
/// <c>world_level_hard_caps</c>. Main-quest Accept hides when the selected
/// hard-cap row has <c>get_quest</c> false and the character is at or over
/// <c>hard_cap_level</c>. That row is chosen from calendar days since
/// <c>SCServerInfo.serverOpenTime</c>, not from <c>SCWorldLevelInfo</c>.
/// Sending "now" as the open time selects the first band. A zero stamp does
/// not clear a stamp the client already cached.
/// </summary>
public static class WorldLevelInfoRules
{
    /// <summary>
    /// Extra whole days on top of the unlock row's <c>min_server_days</c>.
    /// The client diffs local calendar dates, not unix/86400, so a stamp
    /// exactly N days back can still land on N-1.
    /// </summary>
    public const int LocalCalendarSlackDays = 1;

    public const int SecondsPerDay = 86400;

    public static WorldLevelInfoWire ForCharacter(
        int characterLevel,
        int playerLevelCap,
        IReadOnlyList<WorldLevelHardCapRow> hardCaps,
        IReadOnlyList<WorldLevelExpModifierRow> modifiers)
    {
        if (hardCaps == null || hardCaps.Count == 0)
            throw new InvalidOperationException("world_level_hard_caps has no rows.");
        if (modifiers == null || modifiers.Count == 0)
            throw new InvalidOperationException("world_level_exp_modifiers has no rows.");
        if (playerLevelCap <= 0)
            throw new ArgumentOutOfRangeException(nameof(playerLevelCap));

        var level = Math.Clamp(characterLevel, 1, playerLevelCap);
        var unlock = SelectUnlockRow(hardCaps, playerLevelCap);
        var worldLevel = (uint)unlock.HardCapLevel;
        var levelDiff = level - unlock.HardCapLevel;
        var exp = SelectExpModifier(modifiers, levelDiff);

        return new WorldLevelInfoWire(
            WorldLevel: worldLevel,
            ServerDays: (uint)unlock.MinServerDays,
            ExpModifier: exp,
            HardCapLevel: (uint)unlock.HardCapLevel,
            CharacterLevel: (uint)level,
            LevelDiff: levelDiff);
    }

    public static WorldLevelHardCapRow SelectUnlockRow(
        IReadOnlyList<WorldLevelHardCapRow> hardCaps,
        int playerLevelCap)
    {
        if (hardCaps == null || hardCaps.Count == 0)
            throw new InvalidOperationException("world_level_hard_caps has no rows.");
        if (playerLevelCap <= 0)
            throw new ArgumentOutOfRangeException(nameof(playerLevelCap));

        WorldLevelHardCapRow? best = null;
        foreach (var row in hardCaps)
        {
            if (!row.GetQuest || row.HardCapLevel < playerLevelCap)
                continue;
            if (best == null || row.MinServerDays < best.Value.MinServerDays)
                best = row;
        }

        if (best != null)
            return best.Value;

        throw new InvalidOperationException(
            $"world_level_hard_caps has no get_quest row with hard_cap_level >= {playerLevelCap}.");
    }

    /// <summary>
    /// Unix seconds for <c>SCServerInfo</c>. Must be non-zero so the client
    /// replaces any cached stamp. Age is the compact unlock row plus
    /// <see cref="LocalCalendarSlackDays"/>.
    /// </summary>
    public static long ServerOpenUnixTime(long nowUnixSeconds, int unlockMinServerDays)
    {
        if (nowUnixSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(nowUnixSeconds));
        if (unlockMinServerDays < 0)
            throw new ArgumentOutOfRangeException(nameof(unlockMinServerDays));

        var ageSeconds = (long)(unlockMinServerDays + LocalCalendarSlackDays) * SecondsPerDay;
        var open = nowUnixSeconds - ageSeconds;
        return open > 0 ? open : 1;
    }

    internal static uint SelectExpModifier(IReadOnlyList<WorldLevelExpModifierRow> modifiers, int levelDiff)
    {
        foreach (var row in modifiers)
        {
            if (levelDiff >= row.LevelDiffMin && levelDiff <= row.LevelDiffMax)
                return row.ExpModifier;
        }

        throw new InvalidOperationException(
            $"world_level_exp_modifiers has no band for levelDiff={levelDiff}.");
    }
}
