using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Progress: use items that belong to a quest item group after accept.
/// </summary>
public class QuestActObjItemGroupUse(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public override bool CountsAsAnObjective => true;
    public uint ItemGroupId { get; set; }
    public uint HighlightDoodadId { get; set; }
    public int HighlightDoodadPhase { get; set; }
    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }
    public bool DropWhenDestroy { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest {2}, Owner {3}, Group {4}, {5}/{6}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name,
            ItemGroupId, currentObjectiveCount, Count);
        return QuestProgressActRules.ObjectiveMet(currentObjectiveCount, Count, ParentQuestTemplate.Score);
    }

    public override void InitializeAction(Quest quest, QuestAct questAct)
    {
        base.InitializeAction(quest, questAct);
        quest.Owner.Events.OnItemUse += questAct.OnItemUse;
    }

    public override void FinalizeAction(Quest quest, QuestAct questAct)
    {
        quest.Owner.Events.OnItemUse -= questAct.OnItemUse;
        base.FinalizeAction(quest, questAct);
    }

    public override void OnItemUse(QuestAct questAct, object sender, OnItemUseArgs args)
    {
        if (questAct.Template.ActId != ActId)
            return;
        if (!QuestManager.Instance.CheckGroupItem(ItemGroupId, args.ItemId))
            return;
        AddObjective(questAct, 1);
    }
}
