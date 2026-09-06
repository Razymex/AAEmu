using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Notifies a Hero's Dominion Point distribution: giver name and points handed to the territory.</summary>
public class SCHeroGiveDominionPointPacket(int @type, string name, short @type2, uint point, bool myParty) : GamePacket(SCOffsets.SCHeroGiveDominionPointPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(@type);
        stream.Write(name);
        stream.Write(@type2);
        stream.Write(point);
        stream.Write(myParty);
        return stream;
    }
}
