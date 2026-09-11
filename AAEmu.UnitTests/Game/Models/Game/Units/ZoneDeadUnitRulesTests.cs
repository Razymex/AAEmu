using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class ZoneDeadUnitRulesTests
{
    [Test]
    public async Task AcceptsZoneSkill_OnlyWhileAlive()
    {
        await Assert.That(ZoneDeadUnitRules.AcceptsZoneSkill(1)).IsTrue();
        await Assert.That(ZoneDeadUnitRules.AcceptsZoneSkill(0)).IsFalse();
        await Assert.That(ZoneDeadUnitRules.AcceptsZoneSkill(-1)).IsFalse();
    }
}
