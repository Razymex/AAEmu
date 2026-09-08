namespace AAEmu.Game.Models.Game.Char;

/// <summary>One row from compact <c>arche_pass_tiers</c>.</summary>
public sealed class ArchePassTierDesc
{
    public uint Id { get; init; }
    public uint PassId { get; init; }
    public uint Tier { get; init; }
    public int Point { get; init; }
    public uint RewardItemId { get; init; }
    public int RewardItemCount { get; init; }
    public uint PremiumRewardItemId { get; init; }
    public int PremiumRewardItemCount { get; init; }

    public bool HasFreeReward => RewardItemId != 0 && RewardItemCount > 0;

    public bool HasPremiumReward => PremiumRewardItemId != 0 && PremiumRewardItemCount > 0;
}
