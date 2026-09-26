using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public sealed class SCTeamJointPacket(
    uint targetTeamId,
    uint leaderTeamId,
    long type,
    byte packetMode,
    int jointOrder) : GamePacket(SCOffsets.SCTeamJointPacket, 1)
{
    /// <summary>
    /// The <em>position and width</em> of <c>packetMode</c> are established: the client's
    /// serializer carries it as a confirmed one-byte value at the fifth of five body fields
    /// (object offset 36, fixed 21-byte body). The derived <c>_packet_structs_*.json</c> summaries
    /// report an object offset of 0 for it because they flatten guarded reads; the raw serializer
    /// description is the authoritative shape and is what this packet matches.
    ///
    /// The <em>value</em> is not established. The client switches on this byte, but no mode table
    /// for it was recovered, and it is NOT the same numbering as
    /// <c>SCTeamJointInfoPacket.mode</c> (see <see cref="Models.Game.Team.TeamJointModes"/>). Zero is
    /// written as the neutral value; it is the only byte in this slice that is not evidence-backed,
    /// and it stays pinned rather than guessed.
    /// </summary>
    public const byte PacketModeUnresolved = 0;

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(targetTeamId);
        stream.Write(leaderTeamId);
        stream.Write(type);
        stream.Write(packetMode);
        stream.Write(jointOrder);
        return stream;
    }
}
