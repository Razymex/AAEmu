using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Confirm or cancel a pending Bless Uthstin roll. Kinds are 0-based.</summary>
public class CSBlessUthstinApplyStatsPacket() : GamePacket(CSOffsets.CSBlessUthstinApplyStatsPacket, 1)
{
    public bool BApply { get; private set; }
    public int TypeValue { get; private set; }
    public uint IncStatsKind { get; private set; }
    public uint DecStatsKind { get; private set; }
    public uint IncStatsPoint { get; private set; }
    public uint DecStatsPoint { get; private set; }
    public int PageIndex { get; private set; }

    public override void Read(PacketStream stream)
    {
        BApply = stream.ReadBoolean();
        TypeValue = stream.ReadInt32();
        IncStatsKind = stream.ReadUInt32();
        DecStatsKind = stream.ReadUInt32();
        IncStatsPoint = stream.ReadUInt32();
        DecStatsPoint = stream.ReadUInt32();
        PageIndex = stream.ReadInt32();

        Connection.ActiveChar?.BlessUthstin?.TryApply(
            BApply,
            TypeValue,
            (int)IncStatsKind,
            (int)DecStatsKind,
            (int)IncStatsPoint,
            (int)DecStatsPoint,
            PageIndex);
    }
}
