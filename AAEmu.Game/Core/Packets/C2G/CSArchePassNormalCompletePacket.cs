using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Finish a non-premium pass after the last free reward. Body is the <c>arche_passes.id</c>.</summary>
public class CSArchePassNormalCompletePacket() : GamePacket(CSOffsets.CSArchePassNormalCompletePacket, 1)
{
    public int TypeValue { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadInt32();
        Connection.ActiveChar?.ArchePass?.TryComplete((uint)TypeValue);
    }
}
