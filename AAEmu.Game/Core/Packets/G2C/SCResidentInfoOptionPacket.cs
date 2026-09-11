using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Townhall info entry for a zone group. Wire shape from the 10.0.2.13 client applier
///: type u16 followed by an option byte.
/// Opcode 0x38 from the packet ctor. The vtable links AUSCResidentInfoPacket,
/// completing the family 0x37 map -> 0x38 info -> 0x39 member info -> 0x3A info list.
/// </summary>
public class SCResidentInfoOptionPacket(short zoneGroup, byte option) : GamePacket(SCOffsets.SCResidentInfoOptionPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(zoneGroup);
        stream.Write(option);
        return stream;
    }
}
