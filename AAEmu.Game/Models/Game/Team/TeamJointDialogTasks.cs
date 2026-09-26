namespace AAEmu.Game.Models.Game.Team;

/// <summary>
/// Client dialog task ids, as the 10.0.2.13 client exposes them to its own UI scripts. These are
/// protocol identifiers, not shipped content: the client dispatches a task id to the handler it
/// registered for that name, and the server selects a frame by sending the id.
/// </summary>
/// <remarks>
/// Every id here is cross-checked against the client's own exported constant table, and each has a
/// matching <c>X2DialogManager:SetHandler</c> registration in the client's dialog scripts:
/// <list type="bullet">
/// <item><c>DLG_TASK_REQUEST_RAID_JOINT</c> — the frame that offers a joint role to the asked raid.</item>
/// <item><c>DLG_TASK_RESOPONSE_RAID_JOINT</c> — the frame the other raid's owner answers in. It
/// carries a 60 s reply timer, after which the client answers for itself.</item>
/// <item><c>DLG_TASK_TEAM_SUMMON_SUGGEST</c> — the summon confirmation, shown for 60 s.</item>
/// </list>
/// </remarks>
public static class TeamJointDialogTasks
{
    /// <summary>Asks the raid owner to accept or decline a joint, choosing a role.</summary>
    public const uint RequestRaidJoint = 88;

    /// <summary>Asks the other raid's owner to accept or decline a joint it was offered.</summary>
    public const uint ResponseRaidJoint = 93;

    /// <summary>Asks the member whether to be moved to the raid leader's position.</summary>
    public const uint TeamSummonSuggest = 94;
}
