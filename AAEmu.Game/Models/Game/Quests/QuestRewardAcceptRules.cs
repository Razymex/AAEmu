using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Reward AcceptComponent rows start the next quest after this one is marked complete.
/// </summary>
public static class QuestRewardAcceptRules
{
    public static IEnumerable<uint> NextQuestIds(IQuestTemplate template)
    {
        if (template == null)
            yield break;

        var reward = template.GetComponents(QuestComponentKind.Reward);
        if (reward == null)
            yield break;

        foreach (var component in reward)
        {
            if (component?.ActTemplates == null)
                continue;
            foreach (var act in component.ActTemplates)
            {
                if (act is QuestActConAcceptComponent accept && accept.QuestContextId != 0
                    && accept.QuestContextId != template.Id)
                    yield return accept.QuestContextId;
            }
        }
    }
}
