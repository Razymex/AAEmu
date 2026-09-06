using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Empty acknowledgement to the Hero whose Mobilization Order was issued.</summary>
public class SCFactionMobilizationOrderSuccessPacket() : GamePacket(SCOffsets.SCFactionMobilizationOrderSuccessPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        return stream;
    }
}
