using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Finishing component on <c>SCQuestContextCompleted</c>. Zero skips the
/// client's component lookup and leaves that quest's camera / chat work undone.
/// Prefer Ready when it owns a cinema so directing cleanup can clear before
/// the next quest accept. Progress cinema (column or act) is next so a
/// no-Ready scene still names the camera that just played.
/// </summary>
public static class QuestCompletedWire
{
    public static uint ComponentIdForCompletedPacket(uint currentComponentId, IQuestTemplate template)
    {
        if (template != null)
        {
            var readyWithCinema = QuestCinemaRules.FirstCinemaComponentId(template, QuestComponentKind.Ready);
            if (readyWithCinema != 0)
                return readyWithCinema;
            var progressWithCinema = QuestCinemaRules.FirstCinemaComponentId(template, QuestComponentKind.Progress);
            if (progressWithCinema != 0)
                return progressWithCinema;
        }

        if (currentComponentId != 0)
            return currentComponentId;
        if (template == null)
            return 0;

        var reward = template.GetComponents(QuestComponentKind.Reward);
        if (reward is { Length: > 0 } && reward[0].Id != 0)
            return reward[0].Id;

        var ready = template.GetComponents(QuestComponentKind.Ready);
        if (ready is { Length: > 0 } && ready[0].Id != 0)
            return ready[0].Id;

        return 0;
    }
}
