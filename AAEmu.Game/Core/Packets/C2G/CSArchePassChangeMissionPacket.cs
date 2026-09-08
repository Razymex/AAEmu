using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Re-roll an in-progress Arche Pass today-assignment. Body is <c>today_quest_steps.real_step</c>.</summary>
public class CSArchePassChangeMissionPacket() : GamePacket(CSOffsets.CSArchePassChangeMissionPacket, 1)
{
    public uint RealStep { get; private set; }

    public override void Read(PacketStream stream)
    {
        RealStep = stream.ReadUInt32();
        Connection.ActiveChar?.ArchePass?.TryChangeMission(RealStep);
    }
}
