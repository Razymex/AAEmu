using AAEmu.Game.Models.Game.Quests;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestCinemaBindRulesTests
{
    [Test]
    public async Task NamedEvent_BindsWhenItMatchesTheAct()
    {
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(40, 40, 0)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(41, 40, 40)).IsFalse();
    }

    [Test]
    public async Task EmptyEvent_BindsOnlyWhenAlreadyPlayingThatCinema()
    {
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(0, 40, 40)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(0, 40, 0)).IsFalse();
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(0, 40, 9)).IsFalse();
    }

    [Test]
    public async Task MissingAct_DoesNotBind()
    {
        await Assert.That(QuestCinemaBindRules.ShouldBindStarted(40, 0, 40)).IsFalse();
    }

    [Test]
    public async Task EnterProgress_CreditsOnlyTheCinemaAlreadyPlaying()
    {
        await Assert.That(QuestCinemaBindRules.ShouldCreditOnEnterProgress(40, 40)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldCreditOnEnterProgress(0, 40)).IsFalse();
        await Assert.That(QuestCinemaBindRules.ShouldCreditOnEnterProgress(9, 40)).IsFalse();
        await Assert.That(QuestCinemaBindRules.ShouldCreditOnEnterProgress(40, 0)).IsFalse();
    }

    [Test]
    public async Task NextQuest_DoesNotReplaceThePlayingCinema()
    {
        await Assert.That(QuestCinemaBindRules.ShouldReplacePlaying(167, 78)).IsFalse();
        await Assert.That(QuestCinemaBindRules.ShouldReplacePlaying(167, 167)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldReplacePlaying(0, 167)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldReplacePlaying(167, 0)).IsFalse();
        await Assert.That(QuestCinemaBindRules.ShouldReplacePlaying(0, 0)).IsFalse();
    }

    [Test]
    public async Task SkipWithEmptyBody_UsesTheSingleDeferredCinema()
    {
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, [167])).IsEqualTo(167u);
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, [167, 167])).IsEqualTo(167u);
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, [0, 167])).IsEqualTo(167u);
    }

    [Test]
    public async Task SkipKeepsThePlayingIdWhenSet()
    {
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(167, [78])).IsEqualTo(167u);
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, [167, 78])).IsEqualTo(0u);
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, [])).IsEqualTo(0u);
        await Assert.That(QuestCinemaBindRules.ResolveCompletedCinema(0, null)).IsEqualTo(0u);
    }

    [Test]
    public async Task DroppedQuest_DoesNotApplyCinemaEnd()
    {
        await Assert.That(QuestCinemaBindRules.ShouldApplyCinemaEndEffect(true)).IsTrue();
        await Assert.That(QuestCinemaBindRules.ShouldApplyCinemaEndEffect(false)).IsFalse();
        await Assert.That(QuestCinemaBindRules.CinemaEndBelongsToQuest(3901, 3901)).IsTrue();
        await Assert.That(QuestCinemaBindRules.CinemaEndBelongsToQuest(3901, 2385)).IsFalse();
        await Assert.That(QuestCinemaBindRules.CinemaEndBelongsToQuest(0, 3901)).IsFalse();
    }
}
