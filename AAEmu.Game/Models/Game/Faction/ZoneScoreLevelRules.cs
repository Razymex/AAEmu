using AAEmu.Game.GameData;

namespace AAEmu.Game.Models.Game.Faction;

/// <summary>The catalog level selected for one zone-score value.</summary>
public readonly record struct ZoneScoreLevelResolution(
    uint KindId,
    long Score,
    int Level,
    ZoneScoreLevel Definition);

/// <summary>The before/after level selected when a zone-score delta is applied.</summary>
public readonly record struct ZoneScoreLevelTransition(
    uint KindId,
    long PreviousScore,
    long Score,
    int AppliedDelta,
    int PreviousLevel,
    int CurrentLevel,
    ZoneScoreLevel PreviousDefinition,
    ZoneScoreLevel CurrentDefinition,
    bool Changed);

/// <summary>
/// Pure zone-score level resolution backed by the shipped <c>zone_score_levels</c> thresholds.
/// The resolver selects the highest catalog level whose required score is not above the supplied
/// score. It does not grant buffs, enforce <c>max_score</c>, persist state, or bridge World/Zone.
/// </summary>
public static class ZoneScoreLevelRules
{
    /// <summary>Resolves the level for a score, including the shipped level-zero baseline.</summary>
    public static ZoneScoreLevelResolution Resolve(FactionScoringGameData gameData, uint kindId, long score)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        var levels = gameData.GetZoneScoreLevels(kindId);
        if (levels.Count == 0)
            throw new InvalidOperationException($"Zone score kind {kindId} has no level rows.");

        var baseLevel = levels.SingleOrDefault(level => level.Level == 0);
        if (baseLevel is null)
            throw new InvalidOperationException($"Zone score kind {kindId} has no level-zero baseline.");

        var selected = levels
            .Where(level => level.RequiredScore <= score)
            .OrderBy(level => level.Level)
            .LastOrDefault();
        if (selected is null)
            throw new InvalidOperationException($"Zone score kind {kindId} has no level for score {score}.");

        return new ZoneScoreLevelResolution(kindId, score, selected.Level, selected);
    }

    /// <summary>
    /// Applies a signed score delta and reports the catalog level transition. Arithmetic is checked;
    /// no invented maximum or clamp is applied.
    /// </summary>
    public static ZoneScoreLevelTransition ApplyDelta(
        FactionScoringGameData gameData,
        uint kindId,
        long currentScore,
        int scoreDelta)
    {
        long nextScore;
        try
        {
            nextScore = checked(currentScore + scoreDelta);
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException(
                $"Zone score kind {kindId} score overflow while applying {scoreDelta}.",
                exception);
        }

        var previous = Resolve(gameData, kindId, currentScore);
        var current = Resolve(gameData, kindId, nextScore);
        return new ZoneScoreLevelTransition(
            kindId,
            currentScore,
            nextScore,
            scoreDelta,
            previous.Level,
            current.Level,
            previous.Definition,
            current.Definition,
            previous.Level != current.Level);
    }
}
