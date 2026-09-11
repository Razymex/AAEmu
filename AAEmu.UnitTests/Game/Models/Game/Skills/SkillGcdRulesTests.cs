using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillGcdRulesTests
{
    [Test]
    public async Task SpellGcd_UsesCastSpeedNotAttackSpeed()
    {
        await Assert.That(SkillGcdRules.SharedGcdMultiplier(false, globalCooldownMul: 50f, castTimeMul: 1f))
            .IsEqualTo(1f);
        await Assert.That(SkillGcdRules.SharedGcdMultiplier(false, globalCooldownMul: 100f, castTimeMul: 0.8f))
            .IsEqualTo(0.8f);
    }

    [Test]
    public async Task WeaponGcd_UsesAttackSpeed()
    {
        await Assert.That(SkillGcdRules.SharedGcdMultiplier(true, globalCooldownMul: 50f, castTimeMul: 1f))
            .IsEqualTo(0.5f);
    }
}
