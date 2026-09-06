using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Nation-wide broadcast that a Hero issued a Mobilization Order: the rally flag's zone group, the Hero's
/// character id, and the Hero's name. Opens the accept popup on members' clients, which echo the zone
/// group and hero id back in their answer.
/// </summary>
public class SCFactionMobilizationOrderPacket(ulong zoneGroupType, ulong heroId, string heroName) : GamePacket(SCOffsets.SCFactionMobilizationOrderPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(zoneGroupType);
        stream.Write(heroId);
        stream.Write(heroName);
        return stream;
    }
}
