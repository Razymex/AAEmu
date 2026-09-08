using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Progress: player kills after accept, within the configured level gap.
/// </summary>
public class QuestActObjPcKill(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public override bool CountsAsAnObjective => true;
    public int LevelGap { get; set; }
    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }
    public bool TeamShare { get; set; }
    public bool IsParty { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest {2}, Owner {3}, PK {4}/{5}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name, currentObjectiveCount, Count);
        return QuestProgressActRules.ObjectiveMet(currentObjectiveCount, Count, ParentQuestTemplate.Score);
    }

    public override void InitializeAction(Quest quest, QuestAct questAct)
    {
        base.InitializeAction(quest, questAct);
        quest.Owner.Events.OnKill += questAct.OnKill;
    }

    public override void FinalizeAction(Quest quest, QuestAct questAct)
    {
        quest.Owner.Events.OnKill -= questAct.OnKill;
        base.FinalizeAction(quest, questAct);
    }

    public override void OnKill(QuestAct questAct, object sender, OnKillArgs e)
    {
        if (questAct.Template.ActId != ActId)
            return;
        if (e.Victim is not Character victim)
            return;

        var owner = questAct.QuestComponent.Parent.Parent.Owner;
        if (victim.Id == owner.Id)
            return;
        if (!QuestProgressActRules.PcLevelGapOk(owner.Level, victim.Level, LevelGap))
            return;

        AddObjective(questAct, 1);
    }
}
