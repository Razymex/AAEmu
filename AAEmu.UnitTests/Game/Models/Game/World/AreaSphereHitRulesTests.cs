using System.Numerics;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.World;

public class AreaSphereHitRulesTests
{
    private static SphereQuest Area(uint sphereId, Vector3 center, float radius) =>
        new() { SphereId = sphereId, Xyz = center, Radius = radius };

    private static SphereQuest Sign(uint componentId, Vector3 center, float radius) =>
        new() { ComponentId = componentId, Xyz = center, Radius = radius };

    [Test]
    public async Task AreaSphereId_HitsEvenWhenSignQuestIsEmpty()
    {
        var pos = new Vector3(10f, 20f, 30f);
        var hit = AreaSphereHitRules.FindHit(
            1448,
            pos,
            requiredComponentId: 18562,
            areaSpheresAtPosition: [Area(1448, pos, 8f)],
            signSpheresForLinkedQuest: []);

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit.SphereId).IsEqualTo(1448u);
    }

    [Test]
    public async Task WrongAreaId_DoesNotHit()
    {
        var pos = new Vector3(10f, 20f, 30f);
        var hit = AreaSphereHitRules.FindHit(
            1448,
            pos,
            requiredComponentId: 0,
            areaSpheresAtPosition: [Area(708, pos, 8f)],
            signSpheresForLinkedQuest: []);

        await Assert.That(hit).IsNull();
    }

    [Test]
    public async Task OutsideRadius_Misses()
    {
        var center = new Vector3(10f, 20f, 30f);
        var hit = AreaSphereHitRules.FindHit(
            1448,
            center + new Vector3(50f, 0f, 0f),
            requiredComponentId: 0,
            areaSpheresAtPosition: [Area(1448, center, 8f)],
            signSpheresForLinkedQuest: []);

        await Assert.That(hit).IsNull();
    }

    [Test]
    public async Task SignFallback_NeedsMatchingComponentWhenAsked()
    {
        var pos = new Vector3(1f, 2f, 3f);
        var miss = AreaSphereHitRules.FindHit(
            1448,
            pos,
            requiredComponentId: 18562,
            areaSpheresAtPosition: [],
            signSpheresForLinkedQuest: [Sign(0, pos, 8f)]);
        var hit = AreaSphereHitRules.FindHit(
            1448,
            pos,
            requiredComponentId: 18562,
            areaSpheresAtPosition: [],
            signSpheresForLinkedQuest: [Sign(18562, pos, 8f)]);

        await Assert.That(miss).IsNull();
        await Assert.That(hit).IsNotNull();
    }

    [Test]
    public async Task ZeroSphereId_Misses()
    {
        var pos = new Vector3(1f, 2f, 3f);
        var hit = AreaSphereHitRules.FindHit(
            0,
            pos,
            requiredComponentId: 0,
            areaSpheresAtPosition: [Area(1448, pos, 8f)],
            signSpheresForLinkedQuest: []);

        await Assert.That(hit).IsNull();
    }
}
