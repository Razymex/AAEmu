using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCScheduleItemSentPacket(int @type, bool byMail) : GamePacket(SCOffsets.SCScheduleItemSentPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(@type);
        stream.Write(byMail);
        return stream;
    }
}
