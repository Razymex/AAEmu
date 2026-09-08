using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Progress: experience gained after accept.
/// </summary>
public class QuestActObjGainExpPoint(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public override bool CountsAsAnObjective => true;
    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest {2}, Owner {3}, Exp {4}/{5}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name, currentObjectiveCount, Count);
        return QuestProgressActRules.ObjectiveMet(currentObjectiveCount, Count, ParentQuestTemplate.Score);
    }

    public override void InitializeAction(Quest quest, QuestAct questAct)
    {
        base.InitializeAction(quest, questAct);
        quest.Owner.Events.OnQuestProgressStat += questAct.OnQuestProgressStat;
    }

    public override void FinalizeAction(Quest quest, QuestAct questAct)
    {
        quest.Owner.Events.OnQuestProgressStat -= questAct.OnQuestProgressStat;
        base.FinalizeAction(quest, questAct);
    }

    public override void OnQuestProgressStat(QuestAct questAct, object sender, OnQuestProgressStatArgs e)
    {
        if (questAct.Template.ActId != ActId || e.Kind != QuestProgressStatKind.Exp || e.Amount <= 0)
            return;
        AddObjective(questAct, e.Amount);
    }
}
