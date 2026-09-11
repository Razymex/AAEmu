using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Start-act acceptor used when the caller did not name one (GM /quest add).
/// </summary>
public static class QuestAcceptRules
{
    public static bool TryResolveStartAcceptor(
        IQuestTemplate template,
        out QuestAcceptorType acceptorType,
        out uint acceptorId)
    {
        acceptorType = QuestAcceptorType.Unknown;
        acceptorId = 0;
        if (template == null)
            return false;

        foreach (var component in template.GetComponents(QuestComponentKind.Start))
        {
            if (component?.ActTemplates == null)
                continue;

            foreach (var act in component.ActTemplates)
            {
                switch (act)
                {
                    case QuestActConAcceptNpc npc when npc.NpcId != 0:
                        acceptorType = QuestAcceptorType.Npc;
                        acceptorId = npc.NpcId;
                        return true;
                    case QuestActConAcceptDoodad doodad when doodad.DoodadId != 0:
                        acceptorType = QuestAcceptorType.Doodad;
                        acceptorId = doodad.DoodadId;
                        return true;
                    case QuestActConAcceptSphere sphere when sphere.SphereId != 0:
                        acceptorType = QuestAcceptorType.Sphere;
                        acceptorId = sphere.SphereId;
                        return true;
                    case QuestActConAcceptSkill skill when skill.SkillId != 0:
                        acceptorType = QuestAcceptorType.Skill;
                        acceptorId = skill.SkillId;
                        return true;
                    case QuestActConAcceptItem item when item.ItemId != 0:
                        acceptorType = QuestAcceptorType.Item;
                        acceptorId = item.ItemId;
                        return true;
                }
            }
        }

        return false;
    }

    public static void FillUnknownAcceptor(
        IQuestTemplate template,
        ref QuestAcceptorType acceptorType,
        ref uint acceptorId)
    {
        if (acceptorType != QuestAcceptorType.Unknown || acceptorId != 0)
            return;
        if (TryResolveStartAcceptor(template, out var type, out var id))
        {
            acceptorType = type;
            acceptorId = id;
        }
    }
}
