using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestRewardAcceptRulesTests
{
    [Test]
    public async Task RewardAcceptComponent_YieldsNextQuest()
    {
        var template = new QuestTemplate { Id = 10 };
        var reward = new QuestComponentTemplate(template)
        {
            Id = 11,
            KindId = QuestComponentKind.Reward
        };
        reward.ActTemplates.Add(new QuestActConAcceptComponent(reward) { QuestContextId = 12 });
        template.Components[reward.Id] = reward;

        var ids = QuestRewardAcceptRules.NextQuestIds(template).ToArray();
        await Assert.That(ids).IsEquivalentTo(new[] { 12u });
    }

    [Test]
    public async Task SameQuestOrMissing_YieldsNothing()
    {
        var template = new QuestTemplate { Id = 10 };
        var reward = new QuestComponentTemplate(template)
        {
            Id = 11,
            KindId = QuestComponentKind.Reward
        };
        reward.ActTemplates.Add(new QuestActConAcceptComponent(reward) { QuestContextId = 10 });
        template.Components[reward.Id] = reward;

        await Assert.That(QuestRewardAcceptRules.NextQuestIds(template).ToArray()).IsEmpty();
        await Assert.That(QuestRewardAcceptRules.NextQuestIds(null).ToArray()).IsEmpty();
        await Assert.That(QuestRewardAcceptRules.NextQuestIds(new QuestTemplate { Id = 1 }).ToArray()).IsEmpty();
    }
}
