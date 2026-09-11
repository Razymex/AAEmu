using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadQuestFuncRulesTests
{
    private readonly record struct Row(uint Kind, uint QuestId);

    // 14178 phase: report 2387, accept/report 2388, accept/report 2396, accept 2401
    private static readonly Row[] Deltokin =
    [
        new(DoodadQuestFuncRules.ReportKind, 2387),
        new(DoodadQuestFuncRules.AcceptKind, 2388),
        new(DoodadQuestFuncRules.ReportKind, 2388),
        new(DoodadQuestFuncRules.AcceptKind, 2396),
        new(DoodadQuestFuncRules.ReportKind, 2396),
        new(DoodadQuestFuncRules.AcceptKind, 2401)
    ];

    [Test]
    public async Task AfterReportComplete_PicksNextAccept()
    {
        var picked = Pick(has: [], done: [2387]);

        await Assert.That(picked.Kind).IsEqualTo(DoodadQuestFuncRules.AcceptKind);
        await Assert.That(picked.QuestId).IsEqualTo(2388u);
    }

    [Test]
    public async Task InProgressReport_BeatsLaterAccept()
    {
        var picked = Pick(has: [2387], done: []);

        await Assert.That(picked.Kind).IsEqualTo(DoodadQuestFuncRules.ReportKind);
        await Assert.That(picked.QuestId).IsEqualTo(2387u);
    }

    [Test]
    public async Task CompletedAcceptRow_IsNotOfferedAgain()
    {
        await Assert.That(DoodadQuestFuncRules.ShouldOfferAccept(
            DoodadQuestFuncRules.AcceptKind, hasQuest: false, completed: true, repeatable: false)).IsFalse();
        await Assert.That(DoodadQuestFuncRules.ShouldOfferAccept(
            DoodadQuestFuncRules.ReportKind, hasQuest: false, completed: true, repeatable: false)).IsFalse();
    }

    [Test]
    public async Task CanStartFalse_SkipsThatAccept()
    {
        var picked = Pick(has: [], done: [2387], canStart: id => id != 2388);

        await Assert.That(picked.QuestId).IsEqualTo(2396u);
    }

    [Test]
    public async Task ReadyReport_OffersComplete_InProgressDoesNot()
    {
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.QuestComplete, letItDone: false)).IsTrue();
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.NotReady, letItDone: false)).IsFalse();
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.CanEarlyComplete, letItDone: false)).IsFalse();
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.CanEarlyComplete, letItDone: true)).IsTrue();
    }

    [Test]
    public async Task ReadyStep_OffersCompleteEvenIfObjectivesReadNotReady()
    {
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.NotReady,
            letItDone: false,
            QuestStatus.Ready,
            QuestComponentKind.Ready)).IsTrue();
        await Assert.That(DoodadQuestFuncRules.ShouldOfferComplete(
            QuestObjectiveStatus.NotReady,
            letItDone: false,
            QuestStatus.Progress,
            QuestComponentKind.Progress)).IsFalse();
    }

    [Test]
    public async Task Interaction_CountsOnlyWhenUseRanAFunc()
    {
        await Assert.That(DoodadQuestFuncRules.ShouldCountInteraction(true)).IsTrue();
        await Assert.That(DoodadQuestFuncRules.ShouldCountInteraction(false)).IsFalse();
    }

    [Test]
    public async Task NothingStartable_ReturnsDefault()
    {
        var picked = Pick(has: [], done: [2387, 2388, 2396, 2401]);

        await Assert.That(picked).IsEqualTo(default(Row));
    }

    private static Row Pick(uint[] has, uint[] done, Func<uint, bool> canStart = null)
    {
        var have = has.ToHashSet();
        var completed = done.ToHashSet();
        return DoodadQuestFuncRules.Select(
            Deltokin,
            r => r.Kind,
            r => r.QuestId,
            have.Contains,
            completed.Contains,
            _ => false,
            canStart);
    }
}
