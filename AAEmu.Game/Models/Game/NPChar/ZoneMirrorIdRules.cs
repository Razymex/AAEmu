namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// Process-wide object ids. A recycled id that still names a live unit from
/// another zone must not be treated as the same mirror.
/// </summary>
public static class ZoneMirrorIdRules
{
    public static bool IsIdempotentRemirror(
        uint existingZoneId,
        uint existingInstanceId,
        bool existingIsZoneMirror,
        uint incomingZoneId,
        uint incomingInstanceId)
    {
        return existingIsZoneMirror
               && existingZoneId != 0
               && incomingZoneId != 0
               && existingZoneId == incomingZoneId
               && existingInstanceId == incomingInstanceId;
    }
}
