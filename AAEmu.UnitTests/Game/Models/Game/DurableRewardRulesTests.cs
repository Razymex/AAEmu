using AAEmu.Game.Models.Game;

namespace AAEmu.UnitTests.Game.Models.Game;

public class DurableRewardRulesTests
{
    [Test]
    public async Task KeepClaim_WhenTheGrantLanded()
    {
        await Assert.That(DurableRewardRules.KeepClaim(true, false, true)).IsTrue();
        await Assert.That(DurableRewardRules.KeepClaim(true, true, false)).IsTrue();
    }

    [Test]
    public async Task KeepClaim_WhenAnythingWasDelivered()
    {
        await Assert.That(DurableRewardRules.KeepClaim(false, true, true)).IsTrue();
        await Assert.That(DurableRewardRules.KeepClaim(false, true, false)).IsTrue();
    }

    [Test]
    public async Task KeepClaim_OnlyACleanZeroDeliveryRollbackIsRetryable()
    {
        await Assert.That(DurableRewardRules.KeepClaim(false, false, true)).IsFalse();
        await Assert.That(DurableRewardRules.KeepClaim(false, false, false)).IsTrue();
    }
}
