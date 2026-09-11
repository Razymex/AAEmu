using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Resident-map feed: the client decides townhall residency by looking up the zone
/// group in a map only server packets fill (lookup). Wire shape from the
/// 10.0.2.13 client: a single type u16.
/// Opcode 0x37 from the packet ctor (family chain 0x37 -> 0x38 -> 0x39
/// info -> 0x3A balance -> 0x3C member list).
/// </summary>
public class SCResidentMapPacket(short zoneGroup) : GamePacket(SCOffsets.SCResidentMapPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(zoneGroup);
        return stream;
    }
}
