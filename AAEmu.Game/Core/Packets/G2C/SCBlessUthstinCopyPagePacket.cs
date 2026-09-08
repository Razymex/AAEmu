using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Bless Uthstin page copy result. Destination index is 0-based.</summary>
public class SCBlessUthstinCopyPagePacket(
    uint bc,
    bool bResult,
    int copyPageIndex,
    BlessUthstinPage page) : GamePacket(SCOffsets.SCBlessUthstinCopyPagePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(bc);
        stream.Write(bResult);
        stream.Write(copyPageIndex);
        BlessUthstinRules.WriteAppliedStats(stream, page);
        stream.Write(page?.ApplyNormalCount ?? 0);
        stream.Write(page?.ApplySpecialCount ?? 0);
        return stream;
    }
}
