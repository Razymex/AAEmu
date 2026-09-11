namespace AAEmu.Game.Models.Game.Units.Movements;

/// <summary>
/// Dedicate can emit a stand for every unit every tick. Those must not become
/// SCUnitMovements or rewrite World transforms — the client treats a repeat
/// stand as a pose reset (flicker), and a sub-metre rewrite can hop a 64 m
/// region line.
/// </summary>
public static class UnitIdleMoveRules
{
    /// <summary>
    /// Tolerance for treating a zone report as a repeat of the known pose.
    /// Callers fail open: any wider delta is handled as real movement.
    /// </summary>
    public const float SamePositionMetres = 0.15f;

    public static bool IsStationary(
        short velX, short velY, short velZ,
        sbyte deltaX, sbyte deltaY, sbyte deltaZ)
    {
        return velX == 0 && velY == 0 && velZ == 0
               && deltaX == 0 && deltaY == 0 && deltaZ == 0;
    }

    public static bool IsSamePosition(
        float knownX, float knownY, float knownZ,
        float moveX, float moveY, float moveZ,
        float epsilonMetres = SamePositionMetres)
    {
        var dx = knownX - moveX;
        var dy = knownY - moveY;
        var dz = knownZ - moveZ;
        var max = epsilonMetres < 0f ? 0f : epsilonMetres;
        return dx * dx + dy * dy + dz * dz <= max * max;
    }

    public static bool IsSameFacing(
        sbyte knownX, sbyte knownY, sbyte knownZ,
        sbyte moveX, sbyte moveY, sbyte moveZ,
        int tolerance = 1)
    {
        if (tolerance < 0)
            tolerance = 0;
        return AbsDelta(knownX, moveX) <= tolerance
               && AbsDelta(knownY, moveY) <= tolerance
               && AbsDelta(knownZ, moveZ) <= tolerance;
    }

    public static bool ShouldSuppress(
        float knownX, float knownY, float knownZ,
        sbyte knownRx, sbyte knownRy, sbyte knownRz,
        float moveX, float moveY, float moveZ,
        sbyte moveRx, sbyte moveRy, sbyte moveRz,
        short velX, short velY, short velZ,
        sbyte deltaX, sbyte deltaY, sbyte deltaZ)
    {
        if (!IsStationary(velX, velY, velZ, deltaX, deltaY, deltaZ))
            return false;
        if (!IsSamePosition(knownX, knownY, knownZ, moveX, moveY, moveZ))
            return false;
        return IsSameFacing(knownRx, knownRy, knownRz, moveRx, moveRy, moveRz);
    }

    private static int AbsDelta(sbyte a, sbyte b)
    {
        var d = a - b;
        return d < 0 ? -d : d;
    }
}
