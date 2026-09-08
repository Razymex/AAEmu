using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class AttributeGainRulesTests
{
    [Test]
    public async Task ApplyPercentPoints_TreatsValueAsPercentPoints()
    {
        await Assert.That(AttributeGainRules.ApplyPercentPoints(100, 12)).IsEqualTo(112);
        await Assert.That(AttributeGainRules.ApplyPercentPoints(2, -50)).IsEqualTo(1);
        await Assert.That(AttributeGainRules.ApplyPercentPoints(2, -110)).IsEqualTo(0);
        await Assert.That(AttributeGainRules.ApplyPercentPoints(0, 12)).IsEqualTo(0);
    }

    [Test]
    public async Task ApplyGain_AddsThenScales()
    {
        await Assert.That(AttributeGainRules.ApplyGain(0, 120, 0)).IsEqualTo(120);
        await Assert.That(AttributeGainRules.ApplyGain(10, 6, 0)).IsEqualTo(16);
        await Assert.That(AttributeGainRules.ApplyGain(20, 2, 0)).IsEqualTo(22);
        await Assert.That(AttributeGainRules.ApplyGain(20, 2, -50)).IsEqualTo(11);
    }
}
