using AAEmu.Game.Models.Game.Teleport;

namespace AAEmu.UnitTests.Game.Models.Game.Teleport;

public class ReturnTeleportRulesTests
{
    [Test]
    public async Task SameInstance_DoesNotLoad()
    {
        await Assert.That(ReturnTeleportRules.NeedsInstanceLoad(0, 0)).IsFalse();
        await Assert.That(ReturnTeleportRules.NeedsInstanceLoad(1, 1)).IsFalse();
        await Assert.That(ReturnTeleportRules.NeedsInstanceLoad(0, 1)).IsTrue();
        await Assert.That(ReturnTeleportRules.NeedsInstanceLoad(2, 0)).IsTrue();
    }

    [Test]
    public async Task Origin_IsNeverAReturnDestination()
    {
        await Assert.That(ReturnTeleportRules.HasValidDestination(0f, 0f, 0f)).IsFalse();
        await Assert.That(ReturnTeleportRules.HasValidDestination(11791.4f, 11799.6f, 136.5f)).IsTrue();
        await Assert.That(ReturnTeleportRules.HasValidDestination(21204.3f, 10428.6f, 107.5f)).IsTrue();
        await Assert.That(ReturnTeleportRules.HasValidDestination(0f, 1f, 0f)).IsTrue();
        await Assert.That(ReturnTeleportRules.ShouldApplyEndedPosition(0f, 0f, 0f)).IsFalse();
        await Assert.That(ReturnTeleportRules.ShouldApplyEndedPosition(11791.4f, 11799.6f, 136.5f)).IsTrue();
    }

    [Test]
    public async Task LoadWorld_UsesPortalWhenSet()
    {
        await Assert.That(ReturnTeleportRules.LoadWorldId(0, 0)).IsEqualTo(0u);
        await Assert.That(ReturnTeleportRules.LoadWorldId(1, 0)).IsEqualTo(1u);
        await Assert.That(ReturnTeleportRules.LoadWorldId(0, 1)).IsEqualTo(1u);
    }
}
