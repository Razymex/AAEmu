using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class UnitReqTargetNpcGroupRulesTests
{
    [Test]
    public async Task Value2Zero_RequiresMembership()
    {
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(true, true, 0)).IsTrue();
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(true, false, 0)).IsFalse();
    }

    [Test]
    public async Task Value2NonZero_InvertsMembership()
    {
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(true, false, 1)).IsTrue();
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(true, true, 1)).IsFalse();
    }

    [Test]
    public async Task NonNpcTarget_AlwaysFails()
    {
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(false, false, 0)).IsFalse();
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(false, false, 1)).IsFalse();
        await Assert.That(UnitReqTargetNpcGroupRules.Passes(false, true, 1)).IsFalse();
    }
}
