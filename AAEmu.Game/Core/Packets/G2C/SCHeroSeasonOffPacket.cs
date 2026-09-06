using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Previous-period leadership for the voter gate. The client also takes this figure from the game-points
/// table, so both are sent together whenever it changes.
/// </summary>
public class SCHeroSeasonOffPacket(int @type, int score) : GamePacket(SCOffsets.SCHeroSeasonOffPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(@type);
        stream.Write(score);
        return stream;
    }
}
