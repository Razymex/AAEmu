using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Roll a Bless Uthstin apply. The item is spent only on a later confirm.</summary>
public class CSBlessUthstinConsumeApplyStatsPacket() : GamePacket(CSOffsets.CSBlessUthstinConsumeApplyStatsPacket, 1)
{
    public long Item { get; private set; }
    public int PageIndex { get; private set; }

    public override void Read(PacketStream stream)
    {
        Item = stream.ReadInt64();
        PageIndex = stream.ReadInt32();
        Connection.ActiveChar?.BlessUthstin?.TryConsumeApply((ulong)Item, PageIndex);
    }
}
