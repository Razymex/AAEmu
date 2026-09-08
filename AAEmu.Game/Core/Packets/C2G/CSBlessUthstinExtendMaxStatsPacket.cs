using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Spend the extend item to raise this character's Bless Uthstin cap. Packet has no body.</summary>
public class CSBlessUthstinExtendMaxStatsPacket() : GamePacket(CSOffsets.CSBlessUthstinExtendMaxStatsPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        Connection.ActiveChar?.BlessUthstin?.TryExtend();
    }
}
