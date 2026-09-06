using AAEmu.Game.Models.Game.Dominions;

namespace AAEmu.UnitTests.Game.Models.Game.Dominions;

public class TerritoryAgentPlacementTests
{
    // house.g design 187 (zone 205) vs npc_spawners.g 22076 — the only pad within 40 m.
    private const float NuimariHouseX = 969.96f;
    private const float NuimariHouseY = 1682.30f;
    private static readonly TerritoryAgentStandPad NuimariTransport = new(22076, 982f, 1688f, 154.4f, 1.5708f);

    [Test]
    public async Task PicksTheNearbyAuthoredPad_WhenMerchantHasNoPlacement()
    {
        var ok = TerritoryAgentPlacement.TryPickStand(
            NuimariHouseX,
            NuimariHouseY,
            [NuimariTransport],
            [17525],
            out var stand);

        await Assert.That(ok).IsTrue();
        await Assert.That(stand.SpawnerType).IsEqualTo(22076u);
        await Assert.That(stand.X).IsEqualTo(982f);
        await Assert.That(stand.Y).IsEqualTo(1688f);
        await Assert.That(stand.ZRot).IsEqualTo(1.5708f);
    }

    [Test]
    public async Task PrefersMerchantSpawnerType_WhenThatPadIsInRange()
    {
        var merchantPad = new TerritoryAgentStandPad(17525, 984f, 1686f, 154.4f, 0f);
        var ok = TerritoryAgentPlacement.TryPickStand(
            NuimariHouseX,
            NuimariHouseY,
            [NuimariTransport, merchantPad],
            [17525],
            out var stand);

        await Assert.That(ok).IsTrue();
        await Assert.That(stand.SpawnerType).IsEqualTo(17525u);
    }

    [Test]
    public async Task IgnoresPreferredPad_WhenItIsOutsideTheBand()
    {
        var farPreferred = new TerritoryAgentStandPad(17525, 1547f, 1160f, 100f, 0f);
        var ok = TerritoryAgentPlacement.TryPickStand(
            NuimariHouseX,
            NuimariHouseY,
            [NuimariTransport, farPreferred],
            [17525],
            out var stand);

        await Assert.That(ok).IsTrue();
        await Assert.That(stand.SpawnerType).IsEqualTo(22076u);
    }

    [Test]
    public async Task RejectsHouseOriginAndPadsBeyondTwentyMetres()
    {
        TerritoryAgentStandPad[] pads =
        [
            new(1, NuimariHouseX, NuimariHouseY, 154.38f, 0f),
            new(2, NuimariHouseX + 21f, NuimariHouseY, 154.38f, 0f)
        ];

        var ok = TerritoryAgentPlacement.TryPickStand(
            NuimariHouseX,
            NuimariHouseY,
            pads,
            [],
            out _);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task EmptyCatalog_DoesNotInventAStand()
    {
        var ok = TerritoryAgentPlacement.TryPickStand(
            NuimariHouseX,
            NuimariHouseY,
            [],
            [17525],
            out _);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task AllFourNationPads_SitInsideTheBand()
    {
        (float Hx, float Hy, TerritoryAgentStandPad Pad)[] shipped =
        [
            (1518.09f, 1742.23f, new TerritoryAgentStandPad(22075, 1530f, 1748f, 142.6f, 1.5708f)),
            (NuimariHouseX, NuimariHouseY, NuimariTransport),
            (1217.92f, 513.97f, new TerritoryAgentStandPad(22077, 1230f, 520f, 164.5f, 1.5708f)),
            (1469.93f, 314.04f, new TerritoryAgentStandPad(22078, 1482f, 320f, 206.4f, 1.5708f))
        ];

        foreach (var (hx, hy, pad) in shipped)
        {
            var ok = TerritoryAgentPlacement.TryPickStand(hx, hy, [pad], [], out var stand);
            await Assert.That(ok).IsTrue();
            await Assert.That(stand.SpawnerType).IsEqualTo(pad.SpawnerType);
        }
    }
}
