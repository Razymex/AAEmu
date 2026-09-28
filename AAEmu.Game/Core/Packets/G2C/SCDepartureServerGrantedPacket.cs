using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// The server confirms a cross-server departure request.
/// </summary>
/// <remarks>
/// The grant packet has no body; <see cref="Write"/> intentionally emits no fields.
/// </remarks>
public class SCDepartureServerGrantedPacket() : GamePacket(SCOffsets.SCDepartureServerGrantedPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        return stream;
    }
}
