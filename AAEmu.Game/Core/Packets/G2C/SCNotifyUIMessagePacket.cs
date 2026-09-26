using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Opens a client dialog by task id, with up to two string arguments.
/// </summary>
/// <remarks>
/// Wire: u32 <c>msgType</c>, u32 <c>argc</c>, then <c>argv0</c> and <c>argv1</c> as length-prefixed
/// strings. All four fields are confirmed by the 10.0.2.13 client's serializer, which caps each
/// argument at 255 bytes on read, so both arguments are always written — an absent one is empty,
/// not omitted. <c>argc</c> counts how many the client should read.
/// </remarks>
public class SCNotifyUIMessagePacket(uint msgType, uint argc, string argv0, string argv1 = "")
    : GamePacket(SCOffsets.SCNotifyUIMessagePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(msgType);
        stream.Write(argc);
        stream.Write(argv0 ?? string.Empty);
        stream.Write(argv1 ?? string.Empty);
        return stream;
    }
}
