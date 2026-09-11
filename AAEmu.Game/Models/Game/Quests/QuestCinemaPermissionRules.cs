using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Directing permission names a component id. Bind only when that cinema
/// is on the component and the player is at a supplied NPC or doodad.
/// </summary>
public static class QuestCinemaPermissionRules
{
    /// <summary>
    /// Same interact band as merchant / specialty (<c>InteractionRange</c> default).
    /// </summary>
    public const float MaxSourceDistanceMetres = 3f;

    public static bool SourceAllowed(bool requested, bool found, float distanceMetres) =>
        requested && found && distanceMetres <= MaxSourceDistanceMetres;

    public static bool CanBind(
        uint cinemaId,
        bool componentKnown,
        bool questActive,
        QuestComponentKind? componentKind,
        bool npcAllowed,
        bool doodadAllowed)
    {
        if (cinemaId == 0 || !componentKnown)
            return false;
        if (!questActive && componentKind != QuestComponentKind.Start)
            return false;
        return npcAllowed || doodadAllowed;
    }
}
