using AAEmu.Game.Models.Game.Quests;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestComponentEffectRulesTests
{
    [Test]
    public async Task FirstSuccess_MarksApplied()
    {
        var applied = new HashSet<uint>();
        await Assert.That(QuestComponentEffectRules.TryMarkApplied(10, applied)).IsTrue();
        await Assert.That(applied.Contains(10)).IsTrue();
    }

    [Test]
    public async Task SecondSuccess_DoesNotApplyAgain()
    {
        var applied = new HashSet<uint> { 10 };
        await Assert.That(QuestComponentEffectRules.TryMarkApplied(10, applied)).IsFalse();
    }

    [Test]
    public async Task MissingIdOrSet_DoesNotApply()
    {
        await Assert.That(QuestComponentEffectRules.TryMarkApplied(0, new HashSet<uint>())).IsFalse();
        await Assert.That(QuestComponentEffectRules.TryMarkApplied(10, null)).IsFalse();
    }

    [Test]
    public async Task PlayCinemaBeforeBubble_DefersBuffUntilCinema()
    {
        await Assert.That(QuestComponentEffectRules.ShouldDeferUntilCinema(true, 99, 40)).IsTrue();
        await Assert.That(QuestComponentEffectRules.ShouldDeferUntilCinema(true, 0, 40)).IsFalse();
        await Assert.That(QuestComponentEffectRules.ShouldDeferUntilCinema(true, 99, 0)).IsFalse();
        await Assert.That(QuestComponentEffectRules.ShouldDeferUntilCinema(false, 99, 40)).IsFalse();
    }
}
