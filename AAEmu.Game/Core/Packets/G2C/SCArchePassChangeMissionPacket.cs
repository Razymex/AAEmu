using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Weekly mission-change used count. Max is <c>arche_pass_mission_change_count</c>.</summary>
public class SCArchePassChangeMissionPacket(uint count) : GamePacket(SCOffsets.SCArchePassChangeMissionPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(count);
        return stream;
    }
}
