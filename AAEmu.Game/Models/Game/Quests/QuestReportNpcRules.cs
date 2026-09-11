using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// None-step ReportNpc is the talk that starts the scene. It must not wait
/// for Progress (that cinema is later) and must not skip to Ready.
/// A scene talk is credited with the orb, not leftover accept target or
/// a second visit to the starter.
/// </summary>
public static class QuestReportNpcRules
{
    public static bool TalkCompletesWithoutProgress(QuestComponentKind kind) =>
        kind == QuestComponentKind.None;

    public static bool TalkAdvancesToReady(QuestComponentKind kind) =>
        kind is QuestComponentKind.Ready or QuestComponentKind.Progress;

    public static bool CompletesFromCurrentTarget(
        QuestComponentKind kind,
        bool playCinemaBeforeBubble,
        uint progressCinemaId) =>
        !QuestNoneSceneRules.IsSceneReportTalk(kind, playCinemaBeforeBubble, progressCinemaId);

    public static bool CompletesFromTalkEvent(
        QuestComponentKind kind,
        bool playCinemaBeforeBubble,
        uint progressCinemaId) =>
        TalkCompletesWithoutProgress(kind)
        && !QuestNoneSceneRules.IsSceneReportTalk(kind, playCinemaBeforeBubble, progressCinemaId);
}
