using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCDoodadCreatedPacket(Doodad doodad, uint funcGroupId) : GamePacket(SCOffsets.SCDoodadCreatedPacket, 1)
{
    public SCDoodadCreatedPacket(Doodad doodad)
        : this(doodad, doodad.FuncGroupId)
    {
    }

    public override PacketStream Write(PacketStream stream)
    {
        return doodad.Write(stream, funcGroupId);
    }
}
