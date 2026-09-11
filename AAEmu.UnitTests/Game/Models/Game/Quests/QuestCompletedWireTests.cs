using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestCompletedWireTests
{
    [Test]
    public async Task CurrentComponent_Wins_WhenReadyHasNoCinema()
    {
        var template = TemplateWithRewardAndReady(10255, 10254, cinemaId: 0);
        var id = QuestCompletedWire.ComponentIdForCompletedPacket(10255, template);
        await Assert.That(id).IsEqualTo(10255u);
    }

    [Test]
    public async Task ReadyWithCinema_BeatsRewardCurrent()
    {
        // 2387 Ready cinema 56 — clearing that component unblocks next directing.
        var template = TemplateWithRewardAndReady(10264, 10263, cinemaId: 56);
        var id = QuestCompletedWire.ComponentIdForCompletedPacket(10264, template);
        await Assert.That(id).IsEqualTo(10263u);
    }

    [Test]
    public async Task ZeroCurrent_FallsBackToReward()
    {
        var template = TemplateWithRewardAndReady(10255, 10254, cinemaId: 0);
        var id = QuestCompletedWire.ComponentIdForCompletedPacket(0, template);
        await Assert.That(id).IsEqualTo(10255u);
    }

    [Test]
    public async Task MissingReward_FallsBackToReady()
    {
        var template = new QuestTemplate { Id = 2385 };
        var ready = new QuestComponentTemplate(template)
        {
            Id = 10254,
            KindId = QuestComponentKind.Ready
        };
        template.Components[ready.Id] = ready;

        var id = QuestCompletedWire.ComponentIdForCompletedPacket(0, template);
        await Assert.That(id).IsEqualTo(10254u);
    }

    [Test]
    public async Task NullTemplate_StaysZero()
    {
        await Assert.That(QuestCompletedWire.ComponentIdForCompletedPacket(0, null)).IsEqualTo(0u);
    }

    [Test]
    public async Task ProgressCinemaAct_BeatsRewardCurrent()
    {
        var template = TemplateWithRewardAndReady(30, 31, cinemaId: 0);
        var progress = new QuestComponentTemplate(template)
        {
            Id = 32,
            KindId = QuestComponentKind.Progress
        };
        progress.ActTemplates.Add(new QuestActObjCinema(progress) { CinemaId = 40 });
        template.Components[progress.Id] = progress;

        var id = QuestCompletedWire.ComponentIdForCompletedPacket(30, template);
        await Assert.That(id).IsEqualTo(32u);
    }

    private static QuestTemplate TemplateWithRewardAndReady(uint rewardId, uint readyId, uint cinemaId)
    {
        var template = new QuestTemplate { Id = 2385 };
        var reward = new QuestComponentTemplate(template)
        {
            Id = rewardId,
            KindId = QuestComponentKind.Reward
        };
        var ready = new QuestComponentTemplate(template)
        {
            Id = readyId,
            KindId = QuestComponentKind.Ready,
            CinemaId = cinemaId
        };
        template.Components[reward.Id] = reward;
        template.Components[ready.Id] = ready;
        return template;
    }
}
