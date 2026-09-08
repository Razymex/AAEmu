using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Progress: NPC kills after accept, filtered by victim level / grade and the player's heir level.
/// </summary>
public class QuestActObjNpcKill(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public override bool CountsAsAnObjective => true;
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    public int HeirLevelMin { get; set; }
    public int HeirLevelMax { get; set; }
    public bool GradeNormal { get; set; }
    public bool GradeStrong { get; set; }
    public bool GradeElite { get; set; }
    public bool GradeBossA { get; set; }
    public bool GradeBossB { get; set; }
    public bool GradeBossC { get; set; }
    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }
    public bool TeamShare { get; set; }
    public bool IsParty { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest {2}, Owner {3}, Kills {4}/{5}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name, currentObjectiveCount, Count);
        return QuestProgressActRules.ObjectiveMet(currentObjectiveCount, Count, ParentQuestTemplate.Score);
    }

    public override void InitializeAction(Quest quest, QuestAct questAct)
    {
        base.InitializeAction(quest, questAct);
        quest.Owner.Events.OnZoneKill += questAct.OnZoneKill;
    }

    public override void FinalizeAction(Quest quest, QuestAct questAct)
    {
        quest.Owner.Events.OnZoneKill -= questAct.OnZoneKill;
        base.FinalizeAction(quest, questAct);
    }

    public override void OnZoneKill(QuestAct questAct, object sender, OnZoneKillArgs e)
    {
        if (questAct.Template.ActId != ActId)
            return;
        if (e.Victim is not Npc npc)
            return;

        var owner = questAct.QuestComponent.Parent.Parent.Owner;
        var heirLevel = owner is Character player ? player.HeirLevel : 0;
        if (!QuestProgressActRules.LevelInRange(heirLevel, HeirLevelMin, HeirLevelMax))
            return;
        if (!QuestProgressActRules.LevelInRange(npc.Level, LevelMin, LevelMax))
            return;
        if (!QuestProgressActRules.NpcGradeAllowed(
                npc.Template.NpcGradeId,
                GradeNormal, GradeStrong, GradeElite, GradeBossA, GradeBossB, GradeBossC))
            return;

        AddObjective(questAct, 1);
    }
}
