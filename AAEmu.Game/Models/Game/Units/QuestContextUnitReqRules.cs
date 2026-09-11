using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.Units;

/// <summary>
/// <c>unit_reqs</c> kind 32 ProgressQuestContext: value1 is <c>quest_contexts.id</c>.
/// The toast is "quest must be in progress" — journal status, not the internal step.
/// Some quests keep Use/Hunt acts on <see cref="QuestComponentKind.None"/> (Under Dahuta's Veil).
/// </summary>
public static class QuestContextUnitReqRules
{
    public static bool IsInProgress(QuestStatus? status) => status == QuestStatus.Progress;
}
