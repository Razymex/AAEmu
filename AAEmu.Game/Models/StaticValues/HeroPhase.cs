namespace AAEmu.Game.Models.StaticValues;

/// <summary>
/// enum_hero_schedule_events from game_decrypted.sqlite3 — the 4-phase monthly election cycle. Ids match the
/// table exactly (1-4, not 0-based) so HeroGameData can use them directly as dictionary keys without translation.
/// </summary>
public enum HeroPhase : byte
{
    /// <summary>No cycle is currently active/known for "now" — not one of the 4 real DB phases, used only as a fallback.</summary>
    None = 0,

    /// <summary>Leadership points accumulate; at phase end the top hero_conditions.leadership_ranking_scope characters per faction are ranked.</summary>
    LeadershipRanking = 1,

    /// <summary>Top hero_conditions.hero_candidate_scope of the ranked characters are candidates; they may voluntarily withdraw (CSHeroAbstainPacket).</summary>
    HeroAbstain = 2,

    /// <summary>Faction members vote for a remaining candidate (CSHeroVotingPacket).</summary>
    HeroVoting = 3,

    /// <summary>The elected character serves as that faction's Hero; hero_rewards is paid out at phase start.</summary>
    HeroPeriod = 4
}
