using AAEmu.Game.Models.Game.Dominions;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Housing;

public class HousingTerritoryRulesTests
{
    private static readonly (uint GroupId, IReadOnlyCollection<uint> Categories, bool IsTerritoryPad)[] NuimariGroups =
    [
        (1, new uint[] { 1, 9, 10 }, false),
        (19, new uint[] { 2, 3, 4, 14 }, true),
        (20, new uint[] { 20 }, true),
        (21, new uint[] { 6 }, true),
        (22, new uint[] { 13 }, true),
        (23, new uint[] { 34 }, true),
        (24, new uint[] { 35 }, true)
    ];

    [Test]
    public async Task TerritoryPadGroup_NonExtendableOccupiedOnly()
    {
        await Assert.That(HousingTerritoryRules.IsTerritoryPadGroup(canExtend: false, houseless: false)).IsTrue();
        await Assert.That(HousingTerritoryRules.IsTerritoryPadGroup(canExtend: true, houseless: false)).IsFalse();
        await Assert.That(HousingTerritoryRules.IsTerritoryPadGroup(canExtend: false, houseless: true)).IsFalse();
    }

    [Test]
    public async Task TerritoryCategory_FarmAltarAndWalls_NotCottages()
    {
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(6, NuimariGroups)).IsTrue();
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(20, NuimariGroups)).IsTrue();
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(2, NuimariGroups)).IsTrue();
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(1, NuimariGroups)).IsFalse();
    }

    [Test]
    public async Task TerritoryCategory_IgnoresExtendableHousingGroups()
    {
        (uint, IReadOnlyCollection<uint>, bool)[] twoCrowns =
        [
            (1, new uint[] { 1, 9, 10 }, false),
            (3, new uint[] { 1, 11 }, false)
        ];
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(1, twoCrowns)).IsFalse();
        await Assert.That(HousingTerritoryRules.IsTerritoryCategory(11, twoCrowns)).IsFalse();
    }

    [Test]
    public async Task CategoryAllowed_UsesZoneOrGroupName()
    {
        await Assert.That(HousingTerritoryRules.CategoryAllowed(false, true)).IsTrue();
        await Assert.That(HousingTerritoryRules.CategoryAllowed(true, false)).IsTrue();
        await Assert.That(HousingTerritoryRules.CategoryAllowed(false, false)).IsFalse();
    }

    [Test]
    public async Task MayPlace_HeroClaimNeedsTheSeatedHero_GuildClaimNeedsOwnerMember()
    {
        await Assert.That(HousingTerritoryRules.MayPlaceTerritoryBuilding(true, false, true, false)).IsTrue();
        await Assert.That(HousingTerritoryRules.MayPlaceTerritoryBuilding(true, false, false, true)).IsFalse();
        await Assert.That(HousingTerritoryRules.MayPlaceTerritoryBuilding(false, true, false, true)).IsTrue();
        await Assert.That(HousingTerritoryRules.MayPlaceTerritoryBuilding(false, true, true, false)).IsFalse();
        await Assert.That(HousingTerritoryRules.MayPlaceTerritoryBuilding(false, false, true, true)).IsFalse();
    }

    [Test]
    public async Task DeclareBackpack_OnlyCastleClaim()
    {
        await Assert.That(DeclareBackpackRules.ShouldConsumeOnDeclare(BackpackType.CastleClaim)).IsTrue();
        await Assert.That(DeclareBackpackRules.ShouldConsumeOnDeclare(BackpackType.SiegeDeclare)).IsFalse();
        await Assert.That(DeclareBackpackRules.ShouldConsumeOnDeclare(BackpackType.TradePack)).IsFalse();
        await Assert.That(DeclareBackpackRules.ShouldConsumeOnDeclare(BackpackType.Glider)).IsFalse();
    }
}
