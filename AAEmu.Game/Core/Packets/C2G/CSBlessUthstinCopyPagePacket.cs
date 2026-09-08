using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Copy one Bless Uthstin page onto another. Both indexes are 0-based.</summary>
public class CSBlessUthstinCopyPagePacket() : GamePacket(CSOffsets.CSBlessUthstinCopyPagePacket, 1)
{
    public int SrcPageIndex { get; private set; }
    public int DstPageIndex { get; private set; }

    public override void Read(PacketStream stream)
    {
        SrcPageIndex = stream.ReadInt32();
        DstPageIndex = stream.ReadInt32();
        Connection.ActiveChar?.BlessUthstin?.TryCopy(SrcPageIndex, DstPageIndex);
    }
}
