using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Move an owned pass to in-progress. Body is the <c>arche_passes.id</c>.</summary>
/// <remarks>
/// Field order, widths and names come from the 10.0.2.13 client's serializer, which passes each
/// value's name alongside the value:
/// </remarks>
public class CSArchePassStartPacket() : GamePacket(CSOffsets.CSArchePassStartPacket, 1)
{
    public int TypeValue { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadInt32();
        Connection.ActiveChar?.ArchePass?.TryStart((uint)TypeValue);
    }
}
