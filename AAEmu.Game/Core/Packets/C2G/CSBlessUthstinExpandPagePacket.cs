using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Unlock the next Bless Uthstin page. Packet has no body.</summary>
public class CSBlessUthstinExpandPagePacket() : GamePacket(CSOffsets.CSBlessUthstinExpandPagePacket, 1)
{
    public override void Read(PacketStream stream)
    {
        Connection.ActiveChar?.BlessUthstin?.TryExpand();
    }
}
