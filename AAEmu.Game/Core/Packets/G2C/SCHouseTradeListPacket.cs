using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Townhall Sales tab rows. Wire shape from the 10.0.2.13 client:
/// count u32, first bool, final bool, then per row: decoLimit u32,
/// expandedDecoLimit u32, seller name str, tl u16, type u32, price u64,
/// pos xyz floats, zoneId u32, category u32. Opcode 0x2F7 from the packet
/// ctor (same ctor pattern that yields F7/F8/FA for the tax/sale packets).
/// The client resolves kind/division text from its own house objects by tl, and
/// echoes filter/searchword/refresh from its own request state, so the server
/// sends only the header plus rows.
/// </summary>
public class SCHouseTradeListPacket(IEnumerable<House> houses) : GamePacket(SCOffsets.SCHouseTradeListPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        var rows = houses.ToArray();
        stream.Write((uint)rows.Length);
        stream.Write(true); // first page
        stream.Write(true); // final page
        foreach (var house in rows)
        {
            var pos = house.Transform.World.Position;
            stream.Write(house.Template?.DecoLimit ?? 0);
            stream.Write(0u); // expandedDecoLimit
            stream.Write(NameManager.Instance.GetCharacterName(house.OwnerId) ?? string.Empty);
            stream.Write(house.TlId);
            stream.Write(house.TemplateId); // client reads u32 (slot 0x80)
            stream.Write((ulong)house.SellPrice);
            stream.Write(pos.X);
            stream.Write(pos.Y);
            stream.Write(pos.Z);
            stream.Write(house.Transform.ZoneId);
            stream.Write(house.Template?.CategoryId ?? 0);
        }
        return stream;
    }
}
