namespace AAEmu.Game.Models.Game.Char;

/// <summary>Live Arche Pass row the client keeps in its owned-pass map.</summary>
public sealed class ArchePassProgress
{
    public uint PassId { get; set; }
    public long Point { get; set; }
    public ArchePassStatus Status { get; set; }
    public bool Premium { get; set; }
    public uint LastRewardTier { get; set; }
    public uint LastPremiumRewardTier { get; set; }

    public ArchePassProgress Clone() => new()
    {
        PassId = PassId,
        Point = Point,
        Status = Status,
        Premium = Premium,
        LastRewardTier = LastRewardTier,
        LastPremiumRewardTier = LastPremiumRewardTier
    };
}
