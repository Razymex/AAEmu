using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <remarks>
/// 10.0.2.13 body, named by its own serializer: i16 tl, u64 moneyAmount, u64 (the buyer id, which the
/// serializer calls "type"), string sellToName, string houseName. 1.2 sent the money and buyer id as
/// u32 and omitted the house name, so the client parsed the name out of the wrong bytes.
/// </remarks>
public class SCHouseSetForSalePacket(
    ushort tl,
    uint moneyAmount,
    uint sellToPlayerId,
    string sellToName,
    string houseName)
    : GamePacket(SCOffsets.SCHouseSetForSalePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((short)tl);
        stream.Write((ulong)moneyAmount);
        stream.Write((ulong)sellToPlayerId);
        stream.Write(sellToName);
        stream.Write(houseName ?? string.Empty);
        return stream;
    }
}
