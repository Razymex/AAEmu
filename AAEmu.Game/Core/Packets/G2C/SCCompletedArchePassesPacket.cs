using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Completed-pass bitset, one 64-bit word per index (<c>passId &gt;&gt; 6</c>).</summary>
public class SCCompletedArchePassesPacket(IReadOnlyList<(uint Idx, ulong Body)> words)
    : GamePacket(SCOffsets.SCCompletedArchePassesPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        var rows = words ?? [];
        stream.Write(rows.Count);
        foreach (var (idx, body) in rows)
        {
            stream.Write(idx);
            stream.Write(body);
        }

        return stream;
    }
}
