using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.UnitTests.Game.Models.Game.Housing;

public class HouseGPlacementTests
{
    [Test]
    public async Task WorldFromStoredXy_DividesByTwoToTheFortyFour()
    {
        await Assert.That(HouseGPlacement.WorldFromStoredXy(HouseGPlacement.StoredXyScale)).IsEqualTo(1f);
        await Assert.That(HouseGPlacement.WorldFromStoredXy(0)).IsEqualTo(0f);
    }

    [Test]
    public async Task Parse_ReadsPlacedAndRemovedDesigns()
    {
        const string body = """
            house
                id 205000002
                removed false
                faction 2
                design 187
                posX 17592186044416
                posY 35184372088832
                posZ 154.376
                rotZ -0.205
            house
                id 205000001
                removed true
                faction 2
                design 186
                posX 0
                posY 0
                posZ 164.69
                rotZ 0.31
            """;

        var rows = HouseGPlacement.Parse(body, 205);
        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0].DesignId).IsEqualTo(187u);
        await Assert.That(rows[0].ZoneKey).IsEqualTo(205u);
        await Assert.That(rows[0].Removed).IsFalse();
        await Assert.That(rows[0].X).IsEqualTo(1f);
        await Assert.That(rows[0].Y).IsEqualTo(2f);
        await Assert.That(rows[0].Z).IsEqualTo(154.376f);
        await Assert.That(rows[0].Yaw).IsEqualTo(-0.205f);
        await Assert.That(rows[1].Removed).IsTrue();
        await Assert.That(rows[1].DesignId).IsEqualTo(186u);
    }

    [Test]
    public async Task ShouldApply_OnlyUnownedLiveLodestone()
    {
        await Assert.That(HouseGPlacement.ShouldApplyToUnownedLodestone(true, 0, 0, false)).IsTrue();
        await Assert.That(HouseGPlacement.ShouldApplyToUnownedLodestone(true, 0, 0, true)).IsFalse();
        await Assert.That(HouseGPlacement.ShouldApplyToUnownedLodestone(true, 8, 0, false)).IsFalse();
        await Assert.That(HouseGPlacement.ShouldApplyToUnownedLodestone(false, 0, 0, false)).IsFalse();
    }

    [Test]
    public async Task IndexLiveByDesign_SkipsRemoved()
    {
        var live = HouseGPlacementCatalog.IndexLiveByDesign(
        [
            new HouseGPlacement(1, 187, 205, false, 1, 2, 3, 0),
            new HouseGPlacement(2, 186, 205, true, 4, 5, 6, 0)
        ]);
        await Assert.That(live.ContainsKey(187)).IsTrue();
        await Assert.That(live.ContainsKey(186)).IsFalse();
    }
}
