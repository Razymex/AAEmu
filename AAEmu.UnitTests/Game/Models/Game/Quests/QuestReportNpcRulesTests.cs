using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestReportNpcRulesTests
{
    [Test]
    public async Task NoneStepTalk_CompletesWithoutProgress()
    {
        await Assert.That(QuestReportNpcRules.TalkCompletesWithoutProgress(QuestComponentKind.None)).IsTrue();
        await Assert.That(QuestReportNpcRules.TalkCompletesWithoutProgress(QuestComponentKind.Ready)).IsFalse();
        await Assert.That(QuestReportNpcRules.TalkCompletesWithoutProgress(QuestComponentKind.Progress)).IsFalse();
    }

    [Test]
    public async Task ReadyOrProgressTalk_AdvancesToReady()
    {
        await Assert.That(QuestReportNpcRules.TalkAdvancesToReady(QuestComponentKind.Ready)).IsTrue();
        await Assert.That(QuestReportNpcRules.TalkAdvancesToReady(QuestComponentKind.Progress)).IsTrue();
        await Assert.That(QuestReportNpcRules.TalkAdvancesToReady(QuestComponentKind.None)).IsFalse();
    }

    [Test]
    public async Task SceneTalk_DoesNotCompleteFromAcceptTargetOrTownTalk()
    {
        await Assert.That(QuestReportNpcRules.CompletesFromCurrentTarget(QuestComponentKind.None, true, 167)).IsFalse();
        await Assert.That(QuestReportNpcRules.CompletesFromTalkEvent(QuestComponentKind.None, true, 167)).IsFalse();
    }

    [Test]
    public async Task HuntNoneTalk_StillCompletesFromTargetAndTalk()
    {
        await Assert.That(QuestReportNpcRules.CompletesFromCurrentTarget(QuestComponentKind.None, true, 0)).IsTrue();
        await Assert.That(QuestReportNpcRules.CompletesFromTalkEvent(QuestComponentKind.None, true, 0)).IsTrue();
        await Assert.That(QuestReportNpcRules.CompletesFromCurrentTarget(QuestComponentKind.Ready, true, 167)).IsTrue();
    }
}
