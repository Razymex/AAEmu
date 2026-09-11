using System.Numerics;

namespace AAEmu.Game.Models.Game.World;

/// <summary>
/// Skill AreaSphere (kind 35) value1 is compact <c>spheres.id</c>, the same id as
/// <c>quest_area_sphere.g</c> <c>stype</c>. Sign-sphere rows are keyed by a different
/// quest id and must not be the only geometry used for that check.
/// </summary>
public static class AreaSphereHitRules
{
    public static SphereQuest FindHit(
        uint requiredSphereId,
        Vector3 worldPosition,
        uint requiredComponentId,
        IEnumerable<SphereQuest> areaSpheresAtPosition,
        IEnumerable<SphereQuest> signSpheresForLinkedQuest)
    {
        if (requiredSphereId == 0)
            return null;

        if (areaSpheresAtPosition != null)
        {
            foreach (var area in areaSpheresAtPosition)
            {
                if (area == null || area.SphereId != requiredSphereId)
                    continue;
                if (area.Contains(worldPosition))
                    return area;
            }
        }

        if (signSpheresForLinkedQuest == null)
            return null;

        foreach (var sign in signSpheresForLinkedQuest)
        {
            if (sign == null || !sign.Contains(worldPosition))
                continue;
            if (requiredComponentId != 0 && sign.ComponentId != requiredComponentId)
                continue;
            return sign;
        }

        return null;
    }
}
