using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests.Acts;

public class QuestActSupplyArchePassPoint(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public int Point { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        Logger.Debug(
            "{0}({1}).RunAct: Quest: {2}, Owner {3} ({4}), Point {5}",
            QuestActTemplateName, DetailId, quest.TemplateId, quest.Owner.Name, quest.Owner.Id, Point);
        if (quest.Owner is Character player && Point > 0)
            player.ArchePass?.TryAddPoints(Point);
        return true;
    }
}
