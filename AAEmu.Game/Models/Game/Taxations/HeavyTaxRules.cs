using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

using NLog;

namespace AAEmu.Game.Models.Game.Taxations;

/// <summary>
/// Retail heavy-tax multipliers from the <c>heavy_taxes</c> table (count → multiplier),
/// using largest-count-less-than-or-equal semantics. The multiplier is applied as authored;
/// this is the only client-side source for the surcharge. Replaces the old 1.2 guess
/// (min(heavy,10) * 0.5, free under 3 heavies).
/// </summary>
public static class HeavyTaxRules
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<int, float> Multipliers = [];

    public static void Load(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM heavy_taxes";
        command.Prepare();
        using var reader = new SQLiteWrapperReader(command.ExecuteReader());
        var rows = new List<(int Count, float Multiplier)>();
        while (reader.Read())
            rows.Add((reader.GetInt32("count"), reader.GetFloat("multiplier")));
        LoadRows(rows);
        Logger.Info($"Loaded {Multipliers.Count} heavy tax multipliers");
    }

    public static void LoadRows(IEnumerable<(int Count, float Multiplier)> rows)
    {
        Multipliers.Clear();
        foreach (var (count, mult) in rows)
            Multipliers[count] = mult;
    }

    /// <summary>
    /// Optional 3.0 Revelations curve by HEAVY-TAX buildings owned on the account:
    /// 1-2 -> 1.0x, 3 -> 2.0x, 4 -> 2.5x, 5 -> 3.0x, 6 -> 3.5x, 7 -> 5.0x, 8+ -> 6.0x (rounded up).
    /// Off by default: the shipped heavy_taxes table is applied as authored, which is what the
    /// target data says. Servers that follow the 3.0 Revelations notes (e.g. ArcheRage-style
    /// 10.x setups) can set World.HeavyTaxMode = RetailCurve.
    /// </summary>
    private static readonly (int Count, double Multiplier)[] RetailHeavyTaxModifiers =
    [
        (1, 1.0), (3, 2.0), (4, 2.5), (5, 3.0), (6, 3.5), (7, 5.0), (8, 6.0),
    ];

    /// <summary>Heavy-tax modifier for a heavy-tax-buildings count (1.0x below the first step).</summary>
    public static double ModifierForHeavyBuildings(int heavyBuildings)
    {
        var best = 1.0;
        foreach (var (count, multiplier) in RetailHeavyTaxModifiers)
            if (heavyBuildings >= count)
                best = multiplier;
        return best;
    }

    /// <summary>Multiplier for a heavy-property count (0 when below the first taxed count).</summary>
    public static float MultiplierFor(int heavyCount)
    {
        // Fix: Dictionary order is undefined; keep the largest count at or below.
        var best = 0f;
        var bestCount = -1;
        foreach (var (count, mult) in Multipliers)
            if (count <= heavyCount && count > bestCount)
            {
                bestCount = count;
                best = mult;
            }
        return best;
    }

    /// <summary>
    /// One week of tax for a heavy-tax property. Below 3 heavy properties there is no surcharge.
    /// By default the charge comes straight from the loaded heavy_taxes table (largest count at or
    /// below the owned total); with <c>World.HeavyTaxMode = RetailCurve</c> the 3.0 Revelations
    /// curve is used instead.
    /// </summary>
    public static int WeeklyTax(uint baseTax, int heavyCount)
    {
        if (heavyCount < 3)
            return (int)baseTax;

        if (AppConfiguration.Instance.World.HeavyTaxMode == HeavyTaxMode.RetailCurve)
            return (int)Math.Ceiling(baseTax * ModifierForHeavyBuildings(heavyCount));

        // Target data: heavy_taxes.multiplier, applied as authored. The client only renders the
        // rate the server sends (build_window.lua takes bTax/hTax/heavyTaxHouseCount as
        // parameters) and this build ships no housing-tax formula, so the table is the only
        // client-side source for the surcharge. 0 = "not a heavy property" -> no surcharge.
        return (int)Math.Ceiling(baseTax * Math.Max(1.0, MultiplierFor(heavyCount)));
    }
}
