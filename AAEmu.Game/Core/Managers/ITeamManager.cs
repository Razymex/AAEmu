using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Team;

namespace AAEmu.Game.Core.Managers;

public interface ITeamManager : ILoadable
{
    Team GetActiveTeamByUnit(uint unitId);
    Team GetTeamByObjId(uint objId);
    Team GetActiveTeam(uint teamId);
    void MemberRemoveFromTeam(Character unit, Character source, RiskyAction leaveType);

    /// <summary>
    /// Sets a member's dice bid response and tells the team. The raid joint flow resets this when a
    /// joint forms or dissolves, which is why it is on the interface rather than reached through the
    /// concrete manager.
    /// </summary>
    void ChangeDiceBidRule(Character unit, int teamId, ulong memberId, DiceBidRuleKind rule, bool byIdleState);
}
