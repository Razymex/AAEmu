namespace AAEmu.Game.Models.Game.Housing;

/// <summary>
/// Housing-family checks. Guild residences are the <c>housings.family</c> prefix the compact already
/// uses for that set — not a compiled list of design ids.
/// </summary>
public static class HousingResidenceRules
{
    /// <summary>Shipped <c>housings.family</c> prefix for guild residences (complete + any later variants).</summary>
    public const string ExpeditionHouseFamilyPrefix = "hs_expedition_house";

    public static bool IsExpeditionResidenceFamily(string family) =>
        !string.IsNullOrEmpty(family)
        && family.StartsWith(ExpeditionHouseFamilyPrefix, StringComparison.Ordinal);
}
