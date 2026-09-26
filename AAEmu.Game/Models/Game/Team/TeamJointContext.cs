using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Models.Game.Team;

/// <summary>Everything the joint manager needs to know about one team, without a live Team object.</summary>
public sealed record TeamJointTeamSnapshot(
    uint Id,
    bool IsParty,
    uint OwnerId,
    ulong OfficerId,
    int MemberCount,
    uint[] OnlineMemberIds,
    uint JointId,
    bool IsJointLeader,
    int JointOrder)
{
    public bool IsRaid => !IsParty;

    public bool CanManage(uint characterId) =>
        !IsParty && (OwnerId == characterId || OfficerId == characterId);

    public bool CanBreak(uint characterId) => IsJointLeader && CanManage(characterId);

    public bool HasOnlineMembersExcept(uint characterId) =>
        Array.Exists(OnlineMemberIds, id => id != characterId);
}

/// <summary>Everything the joint manager needs to know about one character, without a live unit.</summary>
/// <param name="BlockedStates">
/// The shipped <c>team_summon_notice</c> conditions currently true for this character, plus
/// <see cref="TeamSummonRefusal.InCombat"/>. The live context fills only the states a real
/// <c>Character</c> can answer today; the rest stay clear and are documented as such rather than
/// being assumed.
/// </param>
public sealed record TeamJointCharacterSnapshot(
    uint Id,
    string Name,
    bool IsOnline,
    bool IsInBattle,
    uint ZoneId,
    float X,
    float Y,
    float Z,
    TeamSummonRefusal BlockedStates = TeamSummonRefusal.None);

/// <summary>Where a teleport must land, resolved from the summoner.</summary>
public sealed record TeamSummonDestination(uint ZoneId, float X, float Y, float Z);

/// <summary>
/// World-side access the joint/summon flows need. The live implementation reads TeamManager and
/// IWorldManager; tests supply a fake so every flow is deterministic without a running World.
/// </summary>
public interface ITeamJointContext
{
    TeamJointTeamSnapshot? FindTeam(uint teamId);
    TeamJointTeamSnapshot? FindTeamByMember(uint unitId);
    TeamJointCharacterSnapshot? FindCharacterByName(string name);
    TeamJointCharacterSnapshot? FindCharacterById(uint characterId);
    /// <summary>
    /// The character a character currently has selected, if any. The target context menu arrives
    /// with an empty name and no unit id, so the server has to resolve it from the requester.
    /// </summary>
    TeamJointCharacterSnapshot? FindSelectedTarget(uint characterId);
    bool IsLocalWorld(sbyte worldId);
    void Send(uint characterId, GamePacket packet);
    void SendError(uint characterId, ErrorMessageType error);
    void SendTeamHeader(uint teamId, uint recipientId);
    void ApplyJoint(uint teamId, uint jointId, bool isLeader, int order);
    void ClearJoint(uint teamId);

    /// <summary>
    /// Moves a character to the summoner. Returns false when the world refused the move, which must
    /// not be treated as a success.
    /// </summary>
    bool TryTeleport(uint characterId, TeamSummonDestination destination);

    /// <summary>
    /// True when the character is carrying the summon flame. This is an availability question, asked
    /// before anything is spent, so a summoner without one is refused before the member is moved.
    /// </summary>
    bool HasSummonFlame(uint characterId, uint itemTemplateId);

    /// <summary>
    /// Takes one of <paramref name="itemTemplateId"/> from the character. False means the character
    /// does not have it, which is a refusal, not something to retry. It is called only after the move
    /// has already succeeded, so every earlier refusal has cost the summoner nothing.
    /// </summary>
    bool TryConsumeSummonFlame(uint characterId, uint itemTemplateId);

    /// <summary>True when the summon skill is still on cooldown for the character.</summary>
    bool IsOnSummonCooldown(uint characterId, uint skillId);

    /// <summary>
    /// True when the character cannot receive anything where it stands, so the summon would have
    /// nowhere to land. This is the condition behind the shipped <c>SUMMON_NOT_ENOUGH_SPACE</c>.
    /// </summary>
    bool HasSummonLandingSpace(uint characterId);

    /// <summary>
    /// Restores the loot acquisition method and the per-member dice bid to their defaults. The
    /// shipped texts promise this on both accepting a joint and dissolving one.
    /// </summary>
    void ResetLootRules(uint teamId);

    /// <summary>Opens a client dialog by task id with up to two string arguments.</summary>
    void SendDialogTask(uint characterId, uint taskId, string argv0, string argv1);
}
