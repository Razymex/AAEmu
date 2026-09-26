namespace AAEmu.Game.Models.Game.Team;

/// <summary>
/// The states in which a raid-team summon must be refused.
/// </summary>
/// <remarks>
/// This is not an invented list. The client's summon dialog renders a caution box whose body is the
/// shipped <c>ui_texts</c> row <c>team_summon_notice</c>, and that row enumerates exactly these
/// seven conditions, in this order: dungeon entry, trial, imprisonment, siege participation,
/// incapacitated, death, and a worn backpack. A matching caution heading ships as
/// <c>team_summon_caution</c>.
/// <para>
/// The client also drops the reply entirely when the recipient is in combat, so the frame never
/// reaches the server in that case; <see cref="InCombat"/> is kept here so the server-side rule is
/// the same one the client shows.
/// </para>
/// <para>
/// Which of these states a live <c>Character</c> can currently answer is a separate question, and
/// the two the server has no predicate for stay off. See
/// <see cref="TeamJointCharacterSnapshot.BlockedStates"/>.
/// </para>
/// </remarks>
[Flags]
public enum TeamSummonRefusal
{
    None = 0,

    /// <summary>Dungeon (instance) entry.</summary>
    InDungeon = 1 << 0,

    /// <summary>On trial.</summary>
    InTrial = 1 << 1,

    /// <summary>Imprisoned.</summary>
    Imprisoned = 1 << 2,

    /// <summary>Taking part in a siege.</summary>
    InSiege = 1 << 3,

    /// <summary>Incapacitated.</summary>
    Incapacitated = 1 << 4,

    /// <summary>Dead.</summary>
    Dead = 1 << 5,

    /// <summary>Wearing a backpack.</summary>
    WearingBackpack = 1 << 6,

    /// <summary>In combat. The client suppresses the reply in this state.</summary>
    InCombat = 1 << 7,

    /// <summary>
    /// Everything that refuses a summon: the seven conditions the shipped caution box names, plus
    /// combat, which the client handles by never sending the reply at all. Combat is not in
    /// <see cref="TeamSummonRefusalRules.ShippedList"/> because the caution box does not list it, but
    /// it is refused all the same.
    /// </summary>
    All =
        InDungeon | InTrial | Imprisoned | InSiege | Incapacitated | Dead | WearingBackpack | InCombat,
}

/// <summary>Pure evaluation of the shipped refusal list.</summary>
public static class TeamSummonRefusalRules
{
    /// <summary>The seven conditions the client's caution box names, in shipped order.</summary>
    public static readonly TeamSummonRefusal[] ShippedList =
    [
        TeamSummonRefusal.InDungeon,
        TeamSummonRefusal.InTrial,
        TeamSummonRefusal.Imprisoned,
        TeamSummonRefusal.InSiege,
        TeamSummonRefusal.Incapacitated,
        TeamSummonRefusal.Dead,
        TeamSummonRefusal.WearingBackpack,
    ];

    /// <summary>
    /// True when at least one refusal condition holds. Any bit refuses: the shipped list is a
    /// disjunction, and picking a "most important" reason would be a guess the client does not make.
    /// </summary>
    public static bool Refuses(TeamSummonRefusal states) => (states & TeamSummonRefusal.All) != 0;

    /// <summary>
    /// The names of the conditions that hold, shipped order first and combat last. Combat is
    /// included even though the caution box does not list it, because it refuses just the same and a
    /// log that named no reason would be useless.
    /// </summary>
    public static IReadOnlyList<string> ActiveNames(TeamSummonRefusal states) =>
        RefusalOrder.Where(state => (states & state) != 0).Select(NameOf).ToArray();

    /// <summary>Every refusing condition, in the order the log names them.</summary>
    private static readonly TeamSummonRefusal[] RefusalOrder =
        [.. ShippedList, TeamSummonRefusal.InCombat];

    public static string NameOf(TeamSummonRefusal state) => state switch
    {
        TeamSummonRefusal.InDungeon => "dungeon entry",
        TeamSummonRefusal.InTrial => "trial",
        TeamSummonRefusal.Imprisoned => "imprisonment",
        TeamSummonRefusal.InSiege => "siege participation",
        TeamSummonRefusal.Incapacitated => "incapacitated",
        TeamSummonRefusal.Dead => "dead",
        TeamSummonRefusal.WearingBackpack => "worn backpack",
        TeamSummonRefusal.InCombat => "in combat",
        _ => "none",
    };
}
