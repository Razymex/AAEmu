using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

/// <summary>
/// Per-caster use credit: the map is keyed by the caster, so a consume can only
/// ever return that caster's own recorded result.
/// </summary>
public class DoodadUseCreditTests
{
    [Test]
    public async Task Consume_ZeroCaster_IsFalse()
    {
        await Assert.That(new Doodad().ConsumeUseAppliedFunc(0)).IsFalse();
    }

    [Test]
    public async Task Consume_WithoutThisCastersUse_IsFalse()
    {
        await Assert.That(new Doodad().ConsumeUseAppliedFunc(4321)).IsFalse();
    }
}
