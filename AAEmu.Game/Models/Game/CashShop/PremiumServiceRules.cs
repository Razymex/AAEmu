using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.Game.Models.Game.CashShop;

/// <summary>
/// Purchase-tab rows. Compact has no buy catalog; the listed SKUs are items whose use skill
/// applies <see cref="SpecialType.BuyPremium"/>. Days come from that effect's <c>value1</c>.
/// Price stays 0 — Patron is granted, and vendor <c>item_prices</c> are copper, not AA cash.
/// </summary>
public static class PremiumServiceRules
{
    public readonly record struct Pass(uint ItemId, int Days);

    public readonly record struct BuyPremiumEffect(uint ItemId, int SpecialTypeId, int Days);

    public static IReadOnlyList<Pass> Passes { get; private set; } = [];

    public static void ReplacePasses(IEnumerable<Pass> passes) =>
        Passes = (passes ?? []).ToList();

    /// <summary>
    /// Keeps rows whose special type is <see cref="SpecialType.BuyPremium"/> and whose days
    /// are positive, ordered by duration then item id.
    /// </summary>
    public static IReadOnlyList<Pass> FromBuyPremiumEffects(IEnumerable<BuyPremiumEffect> effects)
    {
        if (effects == null)
            return [];

        return effects
            .Where(row => row.SpecialTypeId == (int)SpecialType.BuyPremium && row.Days > 0)
            .Select(row => new Pass(row.ItemId, row.Days))
            .OrderBy(pass => pass.Days)
            .ThenBy(pass => pass.ItemId)
            .ToList();
    }

    /// <summary>UI days = <c>ptime / 24</c>.</summary>
    public static int Hours(int days) => days > 0 ? days * 24 : 0;

    /// <summary>Non-zero so the Buy button is enabled. Clicking still fails.</summary>
    public const int ListedBuyLimit = 1;

    public const byte PriceTypeAaCash = 0;

    public static PremiumDetail CreateDetail(Pass pass, ushort productId, string name) =>
        new()
        {
            CId = (int)pass.ItemId,
            CName = name ?? string.Empty,
            PId = productId,
            IsSell = 1,
            IsHidden = 0,
            PTime = Hours(pass.Days),
            PType = PriceTypeAaCash,
            Price = 0,
            Id = 0,
            BCount = 0,
            Url = string.Empty,
            DiscountPrice = 0,
            BuyLimit = ListedBuyLimit
        };

    public static IReadOnlyList<PremiumDetail> BuildListed(Func<uint, string> nameOf)
    {
        var rows = new List<PremiumDetail>(Passes.Count);
        ushort productId = 1;
        foreach (var pass in Passes)
        {
            var name = nameOf?.Invoke(pass.ItemId);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            rows.Add(CreateDetail(pass, productId++, name.Trim()));
        }

        return rows;
    }
}
