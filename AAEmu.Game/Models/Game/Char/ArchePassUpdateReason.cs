namespace AAEmu.Game.Models.Game.Char;

/// <summary>Reason byte on <c>SCUpdateArchePass</c>.</summary>
public enum ArchePassUpdateReason : byte
{
    Point = 1,
    RewardItem = 2,
    Dropped = 3,
    Started = 4,
    Owned = 5,
    Buy = 6,
    UpgradePremium = 7,
    Expired = 8,
    Completed = 9,
    Reseted = 10
}
