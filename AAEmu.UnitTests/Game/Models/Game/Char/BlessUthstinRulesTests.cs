using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Char;
using AAEmu.UnitTests.Game.GameData;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class BlessUthstinRulesTests
{
    [Before(Test)]
    public void SeedContent() => ContentConfigTestSeed.BlessAndArchePass();

    [Test]
    public async Task EmptyList_StillWritesOneDefaultPage()
    {
        var body = Write(null, selectPageIndex: 0);

        await Assert.That(BitConverter.ToInt32(body, 0)).IsEqualTo(BlessUthstinRules.DefaultPageCount);
        await Assert.That(body.Length).IsEqualTo(sizeof(int) + BlessUthstinRules.PageWireBytes + 12);
        for (var i = 4; i < body.Length; i++)
            await Assert.That(body[i]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task SeededCharacter_WritesOneEmptyPageAndSelectZero()
    {
        var state = new CharacterBlessUthstin();
        var body = state.WriteBytes();

        await Assert.That(BitConverter.ToInt32(body, 0)).IsEqualTo(1);
        await Assert.That(BitConverter.ToInt32(body, 4 + BlessUthstinRules.PageWireBytes)).IsEqualTo(0);
    }

    [Test]
    public async Task AppliedStats_AreSignedOnTheWire()
    {
        var page = new BlessUthstinPage { Strength = -3, Spirit = 7, ApplyNormalCount = 2 };
        var body = Write([page], selectPageIndex: 0);

        await Assert.That(BitConverter.ToInt32(body, 4)).IsEqualTo(-3);
        await Assert.That(BitConverter.ToInt32(body, 20)).IsEqualTo(7);
        await Assert.That(BitConverter.ToInt32(body, 24)).IsEqualTo(2);
    }

    [Test]
    public async Task ActivatedPageNumber_IsOneBased()
    {
        await Assert.That(BlessUthstinRules.ActivatedPageNumber(0)).IsEqualTo(1);
        await Assert.That(BlessUthstinRules.ActivatedPageNumber(2)).IsEqualTo(3);
        await Assert.That(BlessUthstinRules.ActivatedPageNumber(-1)).IsEqualTo(1);
    }

    [Test]
    public async Task PagesForWire_ClampsToMaxThree()
    {
        var pages = Enumerable.Range(0, 5).Select(_ => new BlessUthstinPage()).ToList();
        var wire = BlessUthstinRules.PagesForWire(pages);
        await Assert.That(wire.Count).IsEqualTo(BlessUthstinRules.MaxPageCount);
    }

    [Test]
    public async Task ConsumeItemCount_FirstApplyIsOne()
    {
        await Assert.That(BlessUthstinRules.ConsumeItemCount(0, _ => 9)).IsEqualTo(1);
        await Assert.That(BlessUthstinRules.ConsumeItemCount(2, n => n + 3)).IsEqualTo(5);
    }

    [Test]
    public async Task SelectCost_IsLevelTimesBase()
    {
        await Assert.That(BlessUthstinRules.SelectCost(10)).IsEqualTo(10 * BlessUthstinRules.SelectCostBase);
    }

    [Test]
    public async Task CopyCost_IsPositiveAppliedTimesBase()
    {
        await Assert.That(BlessUthstinRules.CopyCost(7)).IsEqualTo(7 * BlessUthstinRules.CopyCostBase);
        await Assert.That(BlessUthstinRules.CopyCost(-3)).IsEqualTo(0);
    }

    [Test]
    public async Task CanExtend_StopsWhenAnotherStepWouldNotRaiseMaxStats()
    {
        await Assert.That(BlessUthstinRules.BaseStats).IsEqualTo(200);
        await Assert.That(BlessUthstinRules.MaxStatsLimit).IsEqualTo(300);
        await Assert.That(BlessUthstinRules.ExtendPerPoint).IsEqualTo(20);
        await Assert.That(BlessUthstinRules.CanExtend(80)).IsTrue();
        await Assert.That(BlessUthstinRules.CanExtend(100)).IsFalse();
    }

    [Test]
    public async Task TryRoll_PicksRiseThenDropExcludingRise()
    {
        var item = new BlessUthstinItem
        {
            RiseCount = 2,
            DropCount = 1,
            RiseWeights = [1, 0, 0, 0, 0],
            DropWeights = [1, 1, 0, 0, 0]
        };

        await Assert.That(BlessUthstinRules.TryRoll(item, _ => 0, out var inc, out var dec)).IsTrue();
        await Assert.That(inc).IsEqualTo(0);
        await Assert.That(dec).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyRefuseReason_RejectsWhenNoDropWeightRemains()
    {
        var page = new BlessUthstinPage();
        var item = new BlessUthstinItem
        {
            RiseCount = 2,
            DropCount = 1,
            RiseWeights = [1, 0, 0, 0, 0],
            DropWeights = [0, 0, 0, 0, 0]
        };

        await Assert.That(BlessUthstinRules.ApplyRefuseReason(page, item, [50, 50, 50, 50, 50], 0)).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyRefuseReason_RejectsWhenTheRiseWouldPassTheCap()
    {
        var page = new BlessUthstinPage { Strength = BlessUthstinRules.MaxStats(0) };
        var item = new BlessUthstinItem
        {
            RiseCount = 2,
            DropCount = 1,
            RiseWeights = [1, 0, 0, 0, 0],
            DropWeights = [0, 1, 0, 0, 0]
        };

        await Assert.That(BlessUthstinRules.ApplyRefuseReason(page, item, [50, 50, 50, 50, 50], 0)).IsEqualTo(3);
    }

    private static byte[] Write(IReadOnlyList<BlessUthstinPage> pages, int selectPageIndex)
    {
        var stream = new PacketStream();
        BlessUthstinRules.WritePageInfos(stream, pages, selectPageIndex, 0, 0);
        return stream.GetBytes();
    }
}

file static class BlessUthstinTestExtensions
{
    public static byte[] WriteBytes(this CharacterBlessUthstin state)
    {
        var stream = new PacketStream();
        state.WritePageInfos(stream);
        return stream.GetBytes();
    }
}
