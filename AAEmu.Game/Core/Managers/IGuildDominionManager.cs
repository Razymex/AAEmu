using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Guild claim path for zone groups with no <c>siege_zones</c> row (Exeloch / Sungold, 54 / 56).
/// Own MySQL table; same <see cref="DominionData"/> wire as Hero claims. No weekly siege window
/// and no invented housing registry — territory buildings still use shipped <c>dominion_housings</c>.
/// </summary>
public interface IGuildDominionManager : ILoadable
{
    IEnumerable<DominionData> GuildDominions { get; }
    DominionData GetByZoneId(ushort zoneId);

    /// <summary>The claimed guild dominion whose territory (TerritoryData.RadiusDominion around its X/Y) contains this world position, or null.</summary>
    DominionData GetDominionAtPosition(ushort zoneId, float x, float y);

    /// <summary>Sends every currently-claimed guild dominion to <paramref name="connection"/> (zone-enter / on-request push) - reuses SCDominionDataPacket, the same wire sender DominionManager uses.</summary>
    void SendAllDominionsTo(GameConnection connection);

    /// <summary>Real claim path (used by DeclareDominion.cs, the skill-driven flow, and the /claimterritory GM command) for the guild-owned zones - see GuildDominionManager.Declare's doc comment.</summary>
    DominionData Declare(ushort zoneId, uint expeditionId, House lodestone, Models.Game.Char.Character declarer);

    /// <summary>GM/testing counterpart to DominionManager.ClaimTerritory, guild-owned zones only.</summary>
    DominionData ClaimTerritory(ushort zoneId, Models.Game.Expeditions.Expedition expedition, Models.Game.Char.Character declarer);

    /// <summary>GM/testing tool - releases a claimed guild dominion back to unclaimed.</summary>
    bool UnclaimTerritory(ushort zoneId);

    /// <summary>Re-announces every claimed guild dominion in <paramref name="rawZoneId"/> to a (re)loaded Zone process - wire this to the same ZwOpcodes.ZoneLoaded hook DominionManager.RelayAllToZone uses.</summary>
    void RelayAllToZone(uint rawZoneId);

    /// <summary>Changes a claimed guild dominion's local tax rate; only a member of the owning Expedition may call this.</summary>
    void UpdateTaxRate(GameConnection connection, ushort zoneId, int taxRate);
}
