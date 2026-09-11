using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// Phase gate from <c>doodad_func_quest_reacts</c>. Evaluated only for a character — boot
/// settle with no caster must leave the doodad on its start phase.
/// </summary>
public class DoodadFuncQuestReact : DoodadPhaseFuncTemplate
{
    public uint QuestId { get; set; }
    public uint QuestStatusId { get; set; }
    public int NextPhase { get; set; }
    public uint QuestComponentId { get; set; }
    public bool BubbleOnce { get; set; }
    public uint BubbleId { get; set; }

    public override bool Use(BaseUnit caster, Doodad owner)
    {
        if (caster is not Character character || owner == null || QuestId == 0)
            return false;

        ReadQuestState(character, QuestId, QuestComponentId, out var actualStatus, out var activeComponentId,
            out var readyStepContainsRequiredComponent);

        if (!DoodadQuestReactRules.MatchesStatus(QuestStatusId, actualStatus) ||
            !DoodadQuestReactRules.MatchesComponent(QuestComponentId, activeComponentId, readyStepContainsRequiredComponent))
        {
            return false;
        }

        if (!DoodadQuestReactRules.ShouldAdvance(NextPhase, owner.FuncGroupId))
            return false;

        owner.OverridePhase = NextPhase;
        return true;
    }

    private static void ReadQuestState(
        Character character,
        uint questId,
        uint requiredComponentId,
        out QuestStatus actualStatus,
        out uint activeComponentId,
        out bool readyStepContainsRequiredComponent)
    {
        readyStepContainsRequiredComponent = false;
        activeComponentId = 0;

        if (character.Quests.ActiveQuests.TryGetValue(questId, out var quest))
        {
            actualStatus = quest.Status;
            activeComponentId = quest.CurrentComponentId != 0 ? quest.CurrentComponentId : quest.ComponentId;
            readyStepContainsRequiredComponent = requiredComponentId != 0 &&
                                                 actualStatus == QuestStatus.Ready &&
                                                 quest.Step == QuestComponentKind.Ready &&
                                                 quest.CurrentStep?.Components.ContainsKey(requiredComponentId) == true;
            return;
        }

        actualStatus = character.Quests.HasQuestCompleted(questId)
            ? QuestStatus.Completed
            : QuestStatus.Invalid;
    }
}
