using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadPhaseApplyTests
{
    [Test]
    public async Task StayOnPhase_DoesNotRebroadcast()
    {
        await Assert.That(Doodad.ShouldApplyPhaseAfterSuccessfulFunc(
            completesFromClientPacket: false, toNextPhase: false)).IsFalse();
    }

    [Test]
    public async Task AdvancedPhase_StillApplies()
    {
        await Assert.That(Doodad.ShouldApplyPhaseAfterSuccessfulFunc(
            completesFromClientPacket: false, toNextPhase: true)).IsTrue();
    }

    [Test]
    public async Task ClientCompletedFunc_Waits()
    {
        await Assert.That(Doodad.ShouldApplyPhaseAfterSuccessfulFunc(
            completesFromClientPacket: true, toNextPhase: true)).IsFalse();
    }
}
