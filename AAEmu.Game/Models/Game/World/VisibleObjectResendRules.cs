namespace AAEmu.Game.Models.Game.World;

/// <summary>
/// Cinema can drop units the server still marks streamed. Teleport / login
/// already painted from region enter — a second UnitState without a remove
/// is a duplicate id on the client.
/// </summary>
public static class VisibleObjectResendRules
{
    public static bool ShouldRepaintMirror(bool alreadyStreamed, bool clientDroppedVisibility)
    {
        if (clientDroppedVisibility)
            return true;
        return !alreadyStreamed;
    }

    public static bool ShouldResendDoodadCreates(bool clientDroppedVisibility) =>
        clientDroppedVisibility;
}
