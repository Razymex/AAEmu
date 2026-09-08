using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// One-pass update. Reason 6 inserts a row the client does not already have.
/// </summary>
public class SCUpdateArchePassPacket(
    ArchePassProgress row,
    byte reason,
    int diffPoint,
    bool allDone) : GamePacket(SCOffsets.SCUpdateArchePassPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        ArchePassRules.WriteRow(stream, row);
        stream.Write(reason);
        stream.Write(diffPoint);
        stream.Write(allDone);
        return stream;
    }
}
