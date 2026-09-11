using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Taxations;

namespace AAEmu.UnitTests.Game.Models.Game.Taxations;

public class HeavyTaxRulesTests
{
    // Fix: TUnit runs methods in parallel; Clear-then-fill seeding races. Gate it.
    private static readonly SemaphoreSlim _seedGate = new(1, 1);
    private static void SeedRetail()
    {
        HeavyTaxRules.LoadRows([(1, 0f), (2, 0f), (3, 2f), (4, 3f), (5, 4f), (6, 5f), (10, 5f)]);
    }

    [Test]
    public async Task MultiplierFor_UsesLargestCountAtOrBelow()
    {
        await _seedGate.WaitAsync();
        try
        {
        SeedRetail();
        await Assert.That(HeavyTaxRules.MultiplierFor(0)).IsEqualTo(0f);
        await Assert.That(HeavyTaxRules.MultiplierFor(1)).IsEqualTo(0f);
        await Assert.That(HeavyTaxRules.MultiplierFor(2)).IsEqualTo(0f);
        await Assert.That(HeavyTaxRules.MultiplierFor(3)).IsEqualTo(2f);
        await Assert.That(HeavyTaxRules.MultiplierFor(4)).IsEqualTo(3f);
        await Assert.That(HeavyTaxRules.MultiplierFor(5)).IsEqualTo(4f);
        await Assert.That(HeavyTaxRules.MultiplierFor(6)).IsEqualTo(5f);
        await Assert.That(HeavyTaxRules.MultiplierFor(9)).IsEqualTo(5f);
        await Assert.That(HeavyTaxRules.MultiplierFor(100)).IsEqualTo(5f);
        }
        finally { _seedGate.Release(); }
    }

    [Test]
    public async Task WeeklyTax_FollowsTargetDataByDefault()
    {
        await _seedGate.WaitAsync();
        try
        {
        SeedRetail();
        var world = AppConfiguration.Instance.World;
        var original = world.HeavyTaxMode;
        try
        {
            world.HeavyTaxMode = HeavyTaxMode.TargetData;
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 0)).IsEqualTo(150000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 2)).IsEqualTo(150000);
            // Target data: heavy_taxes applied as authored, so the multiplier IS the factor
            // (3 -> 2.0x, 4 -> 3.0x, 5 -> 4.0x, 6+ -> the table caps at 5 -> 5.0x). No conversion.
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 3)).IsEqualTo(300000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 4)).IsEqualTo(450000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 5)).IsEqualTo(600000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 6)).IsEqualTo(750000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 7)).IsEqualTo(750000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 8)).IsEqualTo(750000);
        }
        finally { world.HeavyTaxMode = original; }
        }
        finally { _seedGate.Release(); }
    }

    [Test]
    public async Task WeeklyTax_RetailCurveOption_Matches3_0Notes()
    {
        await _seedGate.WaitAsync();
        try
        {
        SeedRetail();
        var world = AppConfiguration.Instance.World;
        var original = world.HeavyTaxMode;
        try
        {
            world.HeavyTaxMode = HeavyTaxMode.RetailCurve;
            // 3.0 Revelations curve, keyed by HEAVY-TAX buildings owned (rounded up):
            // 3 -> 2.0x, 4 -> 2.5x, 5 -> 3.0x, 6 -> 3.5x, 7 -> 5.0x, 8+ -> 6.0x.
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 3)).IsEqualTo(300000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 4)).IsEqualTo(375000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 6)).IsEqualTo(525000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 7)).IsEqualTo(750000);
            await Assert.That(HeavyTaxRules.WeeklyTax(150000, 8)).IsEqualTo(900000);
        }
        finally { world.HeavyTaxMode = original; }
        }
        finally { _seedGate.Release(); }
    }
}
