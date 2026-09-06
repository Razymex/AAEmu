namespace AAEmu.Game.Models.Game.Dominions;

/// <summary>
/// One zone-local <c>npc_spawners.g</c> point. Used to stand the territory agent next to the lodestone.
/// </summary>
public readonly record struct TerritoryAgentStandPad(
    uint SpawnerType,
    float X,
    float Y,
    float Z,
    float ZRot);

/// <summary>
/// After a nation claim, World authors <c>siege_zones.dominion_merchant_id</c>. Those NPCs have
/// sqlite spawners but no <c>npc_spawners.g</c> rows. Housing <c>AutoZOffset</c> on the lodestones
/// is 0,0,0, so origin plants the agent inside the tower. Each shipped lodestone has exactly one
/// nearby pad (the territory transport stand, ~13.3 m). Prefer the merchant's own spawner types
/// when those pads exist; otherwise take that nearest authored stand.
/// </summary>
public static class TerritoryAgentPlacement
{
    /// <summary>Covers the four nation pads (~13.3 m) without reaching coastal HQ stands.</summary>
    public const float MaxStandDistanceMetres = 20f;

    /// <summary>Ignore a pad sitting on the house origin itself.</summary>
    public const float MinStandDistanceMetres = 0.5f;

    public static bool TryPickStand(
        float houseLocalX,
        float houseLocalY,
        IEnumerable<TerritoryAgentStandPad> pads,
        IReadOnlyCollection<uint> preferredSpawnerTypes,
        out TerritoryAgentStandPad stand)
    {
        stand = default;
        if (pads == null)
            return false;

        var min2 = MinStandDistanceMetres * MinStandDistanceMetres;
        var max2 = MaxStandDistanceMetres * MaxStandDistanceMetres;
        var bestPreferredD2 = float.MaxValue;
        var bestAnyD2 = float.MaxValue;
        var bestPreferred = default(TerritoryAgentStandPad);
        var bestAny = default(TerritoryAgentStandPad);
        var foundPreferred = false;
        var foundAny = false;

        foreach (var pad in pads)
        {
            var dx = pad.X - houseLocalX;
            var dy = pad.Y - houseLocalY;
            var d2 = dx * dx + dy * dy;
            if (d2 < min2 || d2 > max2)
                continue;

            if (!foundAny || d2 < bestAnyD2)
            {
                bestAnyD2 = d2;
                bestAny = pad;
                foundAny = true;
            }

            if (!ContainsType(preferredSpawnerTypes, pad.SpawnerType))
                continue;
            if (foundPreferred && d2 >= bestPreferredD2)
                continue;

            bestPreferredD2 = d2;
            bestPreferred = pad;
            foundPreferred = true;
        }

        if (foundPreferred)
        {
            stand = bestPreferred;
            return true;
        }

        if (foundAny)
        {
            stand = bestAny;
            return true;
        }

        return false;
    }

    private static bool ContainsType(IReadOnlyCollection<uint> types, uint spawnerType)
    {
        if (types == null || types.Count == 0)
            return false;
        foreach (var type in types)
        {
            if (type == spawnerType)
                return true;
        }

        return false;
    }
}
