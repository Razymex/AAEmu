using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Weekly mission-complete used count. Max is <c>arche_pass_mission_complete_count</c>.</summary>
public class SCArchePassMissionCountPacket(uint count) : GamePacket(SCOffsets.SCArchePassMissionCountPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(count);
        return stream;
    }
}
