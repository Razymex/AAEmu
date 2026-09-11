using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.UnitTests.Game.Models.Game.Housing;

public class SalePriceRulesTests
{
    [Test]
    public async Task IsListablePrice_AcceptsPositiveUpToCap()
    {
        await Assert.That(SalePriceRules.IsListablePrice(0)).IsFalse();
        await Assert.That(SalePriceRules.IsListablePrice(1)).IsTrue();
        await Assert.That(SalePriceRules.IsListablePrice(100000)).IsTrue();
        await Assert.That(SalePriceRules.IsListablePrice((ulong)SalePriceRules.MaxSalePrice)).IsTrue();
        await Assert.That(SalePriceRules.IsListablePrice((ulong)SalePriceRules.MaxSalePrice + 1)).IsFalse();
        await Assert.That(SalePriceRules.IsListablePrice(ulong.MaxValue)).IsFalse();
    }
}
