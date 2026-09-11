using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Accept starts the Progress cinema. None acts (orb / talk / loot) are
/// the film and complete during it. The client owns doodad retry if that
/// chain did not finish; World only re-runs the same path while the quest
/// is still active.
/// </summary>
public static class QuestNoneSceneRules
{
    public static bool ShouldStartSceneCinema(bool playCinemaBeforeBubble, uint progressCinemaId) =>
        playCinemaBeforeBubble && progressCinemaId != 0;

    public static bool NoneHasPlayCinemaBeforeBubble(IQuestTemplate template)
    {
        var none = template?.GetComponents(QuestComponentKind.None);
        if (none == null)
            return false;
        foreach (var component in none)
        {
            if (component.PlayCinemaBeforeBubble)
                return true;
        }

        return false;
    }

    public static bool ShouldStartSceneOnAccept(IQuestTemplate template) =>
        ShouldStartSceneCinema(
            NoneHasPlayCinemaBeforeBubble(template),
            QuestCinemaRules.FirstCinema(template, QuestComponentKind.Progress));

    /// <summary>
    /// None-step ReportNpc on a scene quest is the film talk. Leftover
    /// accept target must not complete it before the scene credits the act.
    /// </summary>
    public static bool IsSceneReportTalk(
        QuestComponentKind kind,
        bool playCinemaBeforeBubble,
        uint progressCinemaId) =>
        kind == QuestComponentKind.None && ShouldStartSceneCinema(playCinemaBeforeBubble, progressCinemaId);
}
