using AAEmu.Game.Models.Game.Items;

namespace AAEmu.UnitTests.Game.Models.Game.Items;

public class ItemWalletRulesTests
{
    [Test]
    public async Task BmMileage_IsTheClientWalletItem()
    {
        await Assert.That(Item.BmMileage).IsEqualTo(28586u);
        await Assert.That(ItemWalletRules.IsBmMileage(28586)).IsTrue();
        await Assert.That(ItemWalletRules.IsBmMileage(29911)).IsFalse();
        await Assert.That(ItemWalletRules.IsBmMileage(Item.Coins)).IsFalse();
    }

    [Test]
    public async Task CreditOnAcquire_OnlyBagAndBank()
    {
        await Assert.That(ItemWalletRules.CreditOnAcquire(28586, SlotType.Inventory)).IsTrue();
        await Assert.That(ItemWalletRules.CreditOnAcquire(28586, SlotType.Bank)).IsTrue();
        await Assert.That(ItemWalletRules.CreditOnAcquire(28586, SlotType.Mail)).IsFalse();
        await Assert.That(ItemWalletRules.CreditOnAcquire(28586, SlotType.Auction)).IsFalse();
        await Assert.That(ItemWalletRules.CreditOnAcquire(29911, SlotType.Inventory)).IsFalse();
    }

    [Test]
    public async Task LoyaltyFromCount_IgnoresNonPositive()
    {
        await Assert.That(ItemWalletRules.LoyaltyFromCount(5)).IsEqualTo(5);
        await Assert.That(ItemWalletRules.LoyaltyFromCount(0)).IsEqualTo(0);
        await Assert.That(ItemWalletRules.LoyaltyFromCount(-3)).IsEqualTo(0);
    }

    [Test]
    public async Task CreditsFromEffect_ScalesTheCouponValue()
    {
        await Assert.That(ItemWalletRules.CreditsFromEffect(200, 1)).IsEqualTo(200);
        await Assert.That(ItemWalletRules.CreditsFromEffect(500, 2)).IsEqualTo(1000);
        await Assert.That(ItemWalletRules.CreditsFromEffect(0, 1)).IsEqualTo(0);
        await Assert.That(ItemWalletRules.CreditsFromEffect(200, 0)).IsEqualTo(0);
    }
}
