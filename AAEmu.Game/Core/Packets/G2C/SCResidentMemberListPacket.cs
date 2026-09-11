using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Townhall Residents tab rows. Wire shape from the 10.0.2.13 client
///, opcode 0x3C from the packet factory):
/// type u16, total u32, count u32, final bool, then per row: servicePoint u32,
/// updated datetime, type u64, name str, level u8, heirLevel u8, family u32,
/// isOnLine bool, isInParty bool. Matches the memberlist lua columns
/// (NAME/PARTY/ONLINE/SERVICE/LEVEL/HEIR_LEVEL/FAMILY).
/// </summary>
public class SCResidentMemberListPacket(short zoneGroup, uint total, IReadOnlyList<ResidentMemberRow> rows) : GamePacket(SCOffsets.SCResidentMemberListPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(zoneGroup);
        stream.Write(total);
        stream.Write((uint)rows.Count);
        stream.Write(true); // final
        foreach (var row in rows)
        {
            stream.Write(row.ServicePoint);
            stream.Write(row.Updated);
            stream.Write(row.CharId);
            stream.Write(row.Name);
            stream.Write(row.Level);
            stream.Write(row.HeirLevel);
            stream.Write(row.Family);
            stream.Write(row.IsOnline);
            stream.Write(row.IsInParty);
        }
        return stream;
    }
}

public sealed record ResidentMemberRow(
    uint ServicePoint,
    DateTime Updated,
    ulong CharId,
    string Name,
    byte Level,
    byte HeirLevel,
    uint Family,
    bool IsOnline,
    bool IsInParty);
