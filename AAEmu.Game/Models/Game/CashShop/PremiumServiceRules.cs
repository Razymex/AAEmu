namespace AAEmu.Game.Models.Game.CashShop;

/// <summary>
/// Purchase-tab rows. Compact has no buy catalog; the listed SKUs are the ArcheLife
/// duration tickets (<c>items</c> 49183 / 49187–49193). Days come from those item names.
/// Price stays 0 — Patron is granted, and vendor <c>item_prices</c> are copper, not AA cash.
/// </summary>
public static class PremiumServiceRules
{
    public readonly record struct Pass(uint ItemId, int Days);

    public static IReadOnlyList<Pass> Passes { get; } =
    [
        new(49183, 1),
        new(49187, 3),
        new(49188, 7),
        new(49189, 15),
        new(49190, 30),
        new(49191, 90),
        new(49192, 180),
        new(49193, 365)
    ];

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
