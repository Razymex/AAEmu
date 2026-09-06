using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Marks the character as having voted this cycle; the ballot window greys its vote button on it.</summary>
public class SCHeroVotingPacket(int @type, sbyte voteInfo) : GamePacket(SCOffsets.SCHeroVotingPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(@type);
        stream.Write(voteInfo);
        return stream;
    }
}
