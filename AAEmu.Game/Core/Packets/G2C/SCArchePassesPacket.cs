using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Owned-pass list. <paramref name="last"/> tells the client the burst is complete.
/// </summary>
public class SCArchePassesPacket(IReadOnlyList<ArchePassProgress> passes, bool last)
    : GamePacket(SCOffsets.SCArchePassesPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        var rows = ArchePassRules.RowsForWire(passes);
        stream.Write(rows.Count);
        stream.Write(last);
        foreach (var row in rows)
            ArchePassRules.WriteRow(stream, row);
        return stream;
    }
}
