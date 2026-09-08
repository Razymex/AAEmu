namespace AAEmu.Game.Models.Game.Units;

/// <summary>
/// Compact <c>unit_modifiers</c> of type Value on a *Mul attribute are percent points
/// (ExpMul +12 is 112%). The same scale applies to death-loss and honor gains.
/// </summary>
public static class AttributeGainRules
{
    public static int ApplyPercentPoints(int amount, int percentPoints)
    {
        if (amount <= 0)
            return 0;
        var factor = 100 + percentPoints;
        if (factor <= 0)
            return 0;
        return (int)((long)amount * factor / 100);
    }

    public static int ApplyGain(int baseAmount, int add, int percentMul) =>
        ApplyPercentPoints(Math.Max(0, baseAmount + add), percentMul);
}
