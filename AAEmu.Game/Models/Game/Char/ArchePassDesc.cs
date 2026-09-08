namespace AAEmu.Game.Models.Game.Char;

/// <summary>One row from compact <c>arche_passes</c>.</summary>
public sealed class ArchePassDesc
{
    public uint Id { get; init; }
    public uint CategoryId { get; init; }
    public string Name { get; init; } = "";
    public uint CurrencyId { get; init; }
    public int CurrencyValue { get; init; }
    public uint UpgradeItemId { get; init; }
    public int MaxTier { get; init; }
    public int EndYear { get; init; }
    public int EndMonth { get; init; }
    public int EndDay { get; init; }
    public int EndHour { get; init; }
    public int EndMinute { get; init; }
}
