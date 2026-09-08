using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>One completed-pass bitset word (<c>idx = passId &gt;&gt; 6</c>).</summary>
public class SCCompletedArchePassPacket(int idx, long body) : GamePacket(SCOffsets.SCCompletedArchePassPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(idx);
        stream.Write(body);
        return stream;
    }
}
