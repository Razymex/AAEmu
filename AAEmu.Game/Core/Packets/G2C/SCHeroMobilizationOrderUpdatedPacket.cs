using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Mobilization Order counters for a Hero: action (u8), the rally flag's zone group (u32), the Hero's
/// character id (u64), today's count and the term total. The client only opens the issue dialog on a flag
/// whose zone group matches the one cached from this packet, so it is refreshed on login and on each flag
/// interaction.
/// </summary>
public class SCHeroMobilizationOrderUpdatedPacket(byte action, uint zoneGroupId, ulong characterId, uint todayCount, uint totalCount)
    : GamePacket(SCOffsets.SCHeroMobilizationOrderUpdatedPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(action);
        stream.Write(zoneGroupId);
        stream.Write(characterId);
        stream.Write(todayCount);
        stream.Write(totalCount);
        return stream;
    }
}
