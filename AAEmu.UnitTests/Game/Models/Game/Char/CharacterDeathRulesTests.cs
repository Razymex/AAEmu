using AAEmu.Game.Models.Game.Char;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterDeathRulesTests
{
    [Test]
    public async Task DurabilityLoss_UsesConfigRatioThenPatronMul()
    {
        await Assert.That(CharacterDeathRules.DurabilityLoss(80, 80, 2, 0)).IsEqualTo(1);
        await Assert.That(CharacterDeathRules.DurabilityLoss(200, 200, 2, -50)).IsEqualTo(2);
        await Assert.That(CharacterDeathRules.DurabilityLoss(80, 80, 2, -110)).IsEqualTo(0);
        await Assert.That(CharacterDeathRules.DurabilityLoss(0, 80, 2, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task ClampLostExp_NeverDropsBelowTheLevelFloor()
    {
        await Assert.That(CharacterDeathRules.ClampLostExp(1200, 1000, 300)).IsEqualTo(200);
        await Assert.That(CharacterDeathRules.ClampLostExp(1000, 1000, 50)).IsEqualTo(0);
        await Assert.That(CharacterDeathRules.ClampLostExp(1500, 1000, 40)).IsEqualTo(40);
    }
}
