using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Progress: complete another quest from a context group after accept.
/// </summary>
public class QuestActObjCompleteQuestGroup(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public override bool CountsAsAnObjective => true;
    public uint QuestContextGroupId { get; set; }
    public bool AcceptWith { get; set; }
    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest {2}, Owner {3}, Group {4}, {5}/{6}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name,
            QuestContextGroupId, currentObjectiveCount, Count);
        return QuestProgressActRules.ObjectiveMet(currentObjectiveCount, Count, ParentQuestTemplate.Score);
    }

    public override void InitializeAction(Quest quest, QuestAct questAct)
    {
        base.InitializeAction(quest, questAct);
        quest.Owner.Events.OnQuestComplete += questAct.OnQuestComplete;
    }

    public override void FinalizeAction(Quest quest, QuestAct questAct)
    {
        quest.Owner.Events.OnQuestComplete -= questAct.OnQuestComplete;
        base.FinalizeAction(quest, questAct);
    }

    public override void OnQuestComplete(QuestAct questAct, object sender, OnQuestCompleteArgs e)
    {
        if (questAct.Template.ActId != ActId)
            return;

        var selfId = questAct.QuestComponent.Parent.Parent.TemplateId;
        if (!QuestProgressActRules.CountsTowardCompleteQuestGroup(e.QuestId, selfId))
            return;
        if (!QuestManager.Instance.CheckContextGroup(QuestContextGroupId, e.QuestId))
            return;

        AddObjective(questAct, 1);
    }
}
