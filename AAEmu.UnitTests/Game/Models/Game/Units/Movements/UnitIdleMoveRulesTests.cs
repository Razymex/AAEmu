using AAEmu.Game.Models.Game.Units.Movements;

namespace AAEmu.UnitTests.Game.Models.Game.Units.Movements;

public class UnitIdleMoveRulesTests
{
    [Test]
    public async Task StationaryStand_AtKnownPose_IsSuppressed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19628f, 28276f, 295f, 0, 0, 10,
            0, 0, 0, 0, 0, 0)).IsTrue();
    }

    [Test]
    public async Task QuantizedNoise_InsideBand_IsSuppressed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19628.1f, 28276.05f, 295.02f, 0, 0, 10,
            0, 0, 0, 0, 0, 0)).IsTrue();
    }

    [Test]
    public async Task WrappedHeading_IsStillStationary()
    {
        // 85 and -42 are one step apart on the 128-step heading circle.
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 85, 0, 10,
            19628f, 28276f, 295f, -42, 0, 10,
            0, 0, 0, 0, 0, 0)).IsTrue();
    }

    [Test]
    public async Task OppositeHeading_IsRelayed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19628f, 28276f, 295f, 64, 0, 10,
            0, 0, 0, 0, 0, 0)).IsFalse();
    }

    [Test]
    public async Task WalkVelocity_IsRelayed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19628f, 28276f, 295f, 0, 0, 10,
            40, 0, 0, 0, 0, 0)).IsFalse();
    }

    [Test]
    public async Task RealStep_IsRelayed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19629f, 28276f, 295f, 0, 0, 10,
            0, 0, 0, 0, 0, 0)).IsFalse();
    }

    [Test]
    public async Task TurnInPlace_IsRelayed()
    {
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19628f, 28276f, 295f, 0, 0, 10,
            19628f, 28276f, 295f, 0, 0, 40,
            0, 0, 0, 0, 0, 0)).IsFalse();
    }

    [Test]
    public async Task SectorLine_DoesNotSuppressARealCrossing()
    {
        // 28288 is a 64 m region edge. A metre of travel must stay live.
        await Assert.That(UnitIdleMoveRules.ShouldSuppress(
            19660f, 28287.6f, 295f, 0, 0, 0,
            19660f, 28288.8f, 295f, 0, 0, 0,
            0, 0, 0, 0, 0, 0)).IsFalse();
    }
}
