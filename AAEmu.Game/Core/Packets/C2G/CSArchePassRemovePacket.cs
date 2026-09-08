using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Drop an owned or in-progress pass. Body is the <c>arche_passes.id</c>.</summary>
public class CSArchePassRemovePacket() : GamePacket(CSOffsets.CSArchePassRemovePacket, 1)
{
    public int TypeValue { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadInt32();
        Connection.ActiveChar?.ArchePass?.TryRemove((uint)TypeValue);
    }
}
