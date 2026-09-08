using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// Death durability and experience from <c>item_configs.death_durability_loss_ratio</c>,
/// formulas 8/9, and unit attributes 155-157.
/// </summary>
public static class CharacterDeathRules
{
    public static int DurabilityLoss(int current, int max, int deathRatio, int mul)
    {
        if (current <= 0 || max <= 0 || deathRatio <= 0)
            return 0;
        var raw = max * deathRatio / 100;
        return Math.Min(current, AttributeGainRules.ApplyPercentPoints(raw, mul));
    }

    public static int ClampLostExp(int experience, int levelFloor, int lost)
    {
        if (lost <= 0)
            return 0;
        var intoLevel = Math.Max(0, experience - Math.Max(0, levelFloor));
        return Math.Min(intoLevel, lost);
    }
}
