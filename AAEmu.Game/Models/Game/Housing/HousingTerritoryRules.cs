namespace AAEmu.Game.Models.Game.Housing;

/// <summary>
/// Territory-pad vs regular-plot placement. Pads are <c>housing_groups</c> rows that cannot
/// extend and are not houseless (Auroria inop groups). Ordinary plots stay on group 1.
/// </summary>
public static class HousingTerritoryRules
{
    /// <summary>Shipped <c>housing_groups.id</c> for ordinary player plots.</summary>
    public const uint RegularHousingGroupId = 1;

    /// <summary>
    /// Castle / dominion pads: <c>can_extend</c> is false and <c>houseless</c> is false.
    /// Scarecrow and expandable housing groups are not pads.
    /// </summary>
    public static bool IsTerritoryPadGroup(bool canExtend, bool houseless) =>
        !canExtend && !houseless;

    /// <summary>
    /// True when a house category is allowed by a territory-pad group on this zone.
    /// Farm / altar / workshop / walls sit on those groups; cottages stay on group 1.
    /// </summary>
    public static bool IsTerritoryCategory(
        uint categoryId,
        IEnumerable<(uint GroupId, IReadOnlyCollection<uint> Categories, bool IsTerritoryPad)> zoneGroups)
    {
        foreach (var (_, categories, isTerritoryPad) in zoneGroups)
        {
            if (!isTerritoryPad || categories == null)
                continue;
            if (categories.Contains(categoryId))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Unique <c>dominion_housings</c> and fortification drawings (walls, gates, towers) use the
    /// zone-group claim, not the lodestone circle. Hero claims need the seated Hero; guild claims
    /// need a member of the owning expedition.
    /// </summary>
    public static bool MayPlaceTerritoryBuilding(
        bool heroClaim,
        bool guildClaim,
        bool isCurrentHero,
        bool isOwnerGuildMember)
    {
        if (heroClaim)
            return isCurrentHero;
        if (guildClaim)
            return isOwnerGuildMember;
        return false;
    }

    /// <summary>
    /// Category check keyed by the live zone name, then the zone-group name. <c>housing_areas.name</c>
    /// is the group name (<c>o_nuimari</c>), which is not always <c>zones.name</c> for every cell.
    /// </summary>
    public static bool CategoryAllowed(bool zoneNameAllows, bool zoneGroupNameAllows) =>
        zoneNameAllows || zoneGroupNameAllows;
}
