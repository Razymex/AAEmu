namespace AAEmu.Game.Models.Game.Housing;

/// <summary>
/// Retail-mirroring sale-price guards (auction escrow uses the same cap).
/// </summary>
public static class SalePriceRules
{
    /// <summary>Maximum listable copper price (mirror of auction MaxEscrowCopper).</summary>
    public const long MaxSalePrice = int.MaxValue;

    public static bool IsListablePrice(ulong price) => price > 0 && price <= (ulong)MaxSalePrice;
}
