using AAEmu.Game.GameData;

namespace AAEmu.Game.Models.Game.Faction;

/// <summary>The content-backed source of a faction-competition point contribution.</summary>
public enum FactionCompetitionContributionKind
{
    PlayerKill,
    NpcKill,
    QuestComplete
}

/// <summary>
/// A contribution opportunity resolved from a shipped competition link. The resolver never awards
/// points itself; the caller supplies the already-observed event and the current in-memory score.
/// </summary>
public readonly record struct FactionCompetitionContribution(
    uint CompetitionId,
    FactionCompetitionContributionKind Kind,
    uint SourceId);

/// <summary>The result of applying one catalog-valued contribution to a score.</summary>
public readonly record struct FactionCompetitionScoreUpdate(
    uint CompetitionId,
    FactionCompetitionContributionKind Kind,
    uint SourceId,
    long PreviousScore,
    long Score,
    int AppliedDelta,
    bool RequiredPointsReached);

/// <summary>
/// Pure faction-competition scoring rules backed only by <see cref="FactionScoringGameData"/>.
/// NPC and quest links are eligibility catalogs; point values and the required-point threshold
/// always come from the competition row. There is no faction-state, winner, reset, bridge, or
/// persistence policy in this class because the metadata slice does not prove those decisions.
/// </summary>
public static class FactionCompetitionScoringRules
{
    /// <summary>
    /// Resolves the competitions linked to an NPC. Link rows are retained as content, but one NPC
    /// event yields at most one contribution per competition even when a shipped link is duplicated.
    /// </summary>
    public static IReadOnlyList<FactionCompetitionContribution> ResolveNpcKillContributions(
        FactionScoringGameData gameData,
        uint npcId)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        return gameData.CompetitionNpcLinks
            .Where(link => link.NpcId == npcId)
            .Select(link => link.CompetitionId)
            .Distinct()
            .OrderBy(id => id)
            .Select(id => new FactionCompetitionContribution(id, FactionCompetitionContributionKind.NpcKill, npcId))
            .ToArray();
    }

    /// <summary>Resolves the competitions linked to a quest context.</summary>
    public static IReadOnlyList<FactionCompetitionContribution> ResolveQuestCompleteContributions(
        FactionScoringGameData gameData,
        uint questContextId)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        return gameData.CompetitionQuestLinks
            .Where(link => link.QuestContextId == questContextId)
            .Select(link => link.CompetitionId)
            .Distinct()
            .OrderBy(id => id)
            .Select(id => new FactionCompetitionContribution(id, FactionCompetitionContributionKind.QuestComplete, questContextId))
            .ToArray();
    }

    /// <summary>Returns the shipped point value for an explicitly selected competition and event kind.</summary>
    public static int GetPointDelta(
        FactionScoringGameData gameData,
        uint competitionId,
        FactionCompetitionContributionKind kind)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        var competition = gameData.GetCompetition(competitionId);
        return kind switch
        {
            FactionCompetitionContributionKind.PlayerKill => competition.PlayerKillPoints,
            FactionCompetitionContributionKind.NpcKill => competition.NpcKillPoints,
            FactionCompetitionContributionKind.QuestComplete => competition.QuestCompletePoints,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown faction competition contribution kind.")
        };
    }

    /// <summary>
    /// Applies one contribution using the catalog point value. The score is intentionally not capped:
    /// the catalogs do not prove that a maximum score or reset rule is a gameplay cap.
    /// </summary>
    public static FactionCompetitionScoreUpdate Apply(
        FactionScoringGameData gameData,
        FactionCompetitionContribution contribution,
        long currentScore)
    {
        var delta = GetPointDelta(gameData, contribution.CompetitionId, contribution.Kind);
        long nextScore;
        try
        {
            nextScore = checked(currentScore + delta);
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException(
                $"Faction competition {contribution.CompetitionId} score overflow while applying {delta}.",
                exception);
        }

        var competition = gameData.GetCompetition(contribution.CompetitionId);
        return new FactionCompetitionScoreUpdate(
            contribution.CompetitionId,
            contribution.Kind,
            contribution.SourceId,
            currentScore,
            nextScore,
            delta,
            nextScore >= competition.RequiredPoints);
    }
}
