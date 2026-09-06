namespace AAEmu.Game.Core.Managers;

/// <summary>
/// GM per-zone-group lock on new castle claims. Blocks new claims only: the skill-driven declare and the
/// /claimterritory command check it before creating anything. Existing claims and what derives from them
/// (tower progress, tier, buildings) are untouched. One shared list covers both the Hero/faction store
/// (DominionManager) and the guild store (GuildDominionManager); it is checked at those two entry points.
/// </summary>
public interface IDominionZoneLockManager : ILoadable
{
    IEnumerable<ushort> LockedZones { get; }
    bool IsLocked(ushort zoneId);
    void Lock(ushort zoneId);
    void Unlock(ushort zoneId);
}
