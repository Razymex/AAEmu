using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.UnitTests.Game.Models.Game.CashShop;

public class PremiumServiceRulesTests
{
    [Test]
    public async Task FromBuyPremiumEffects_KeepsPositiveDurationRows()
    {
        var passes = PremiumServiceRules.FromBuyPremiumEffects(
        [
            new(49190, 180, 30),
            new(49183, 180, 1),
            new(9, 179, 5),
            new(8, 180, 0)
        ]);

        await Assert.That(passes).IsEquivalentTo(new PremiumServiceRules.Pass[]
        {
            new(49183, 1),
            new(49190, 30)
        });
    }

    [Test]
    public async Task Hours_AreTwentyFourTimesTheNamedDays()
    {
        await Assert.That(PremiumServiceRules.Hours(1)).IsEqualTo(24);
        await Assert.That(PremiumServiceRules.Hours(30)).IsEqualTo(720);
        await Assert.That(PremiumServiceRules.Hours(0)).IsEqualTo(0);
        await Assert.That(PremiumServiceRules.Hours(-3)).IsEqualTo(0);
    }

    [Test]
    public async Task CreateDetail_IsDisplayOnly()
    {
        var detail = PremiumServiceRules.CreateDetail(new PremiumServiceRules.Pass(49190, 30), 5, "Patron Ticket: 30 Days");

        await Assert.That(detail.CId).IsEqualTo(49190);
        await Assert.That(detail.CName).IsEqualTo("Patron Ticket: 30 Days");
        await Assert.That(detail.PId).IsEqualTo((ushort)5);
        await Assert.That(detail.IsSell).IsEqualTo((byte)1);
        await Assert.That(detail.IsHidden).IsEqualTo((byte)0);
        await Assert.That(detail.PTime).IsEqualTo(720);
        await Assert.That(detail.PType).IsEqualTo(PremiumServiceRules.PriceTypeAaCash);
        await Assert.That(detail.Price).IsEqualTo(0);
        await Assert.That(detail.Id).IsEqualTo(0u);
        await Assert.That(detail.BCount).IsEqualTo(0);
        await Assert.That(detail.Url).IsEqualTo(string.Empty);
        await Assert.That(detail.DiscountPrice).IsEqualTo(0);
        await Assert.That(detail.BuyLimit).IsEqualTo(PremiumServiceRules.ListedBuyLimit);
    }

    [Test]
    public async Task BuildListed_SkipsMissingNamesAndKeepsProductIdsDense()
    {
        PremiumServiceRules.ReplacePasses(
        [
            new(49183, 1),
            new(49190, 30)
        ]);
        var rows = PremiumServiceRules.BuildListed(id => id == 49190 ? "thirty" : id == 49183 ? "one" : null);

        await Assert.That(rows).HasCount().EqualTo(2);
        await Assert.That(rows[0].CId).IsEqualTo(49183);
        await Assert.That(rows[0].PId).IsEqualTo((ushort)1);
        await Assert.That(rows[1].CId).IsEqualTo(49190);
        await Assert.That(rows[1].PId).IsEqualTo((ushort)2);
    }
}
