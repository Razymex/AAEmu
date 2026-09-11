namespace AAEmu.Game.Models.Game.Units;

/// <summary>
/// <c>unit_reqs</c> kind TargetNpcGroup: <c>value1</c> is the group id.
/// <c>value2 == 0</c> requires membership; any other <c>value2</c> inverts
/// (the target must not be in that group). A non-NPC target never passes.
/// </summary>
public static class UnitReqTargetNpcGroupRules
{
    public static bool Passes(bool isNpcTarget, bool inGroup, uint value2)
    {
        if (!isNpcTarget)
            return false;

        return value2 == 0 ? inGroup : !inGroup;
    }
}
