using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSBuyHousePacket() : GamePacket(CSOffsets.CSBuyHousePacket, 1)
{
    public override void Read(PacketStream stream)
    {

        // Fix: wire is u16 tl + u64 moneyAmount (live capture 0900+A086..., was u32)
        var tl = stream.ReadUInt16();
        var moneyAmount = stream.ReadUInt64();

        if (moneyAmount > uint.MaxValue)
        {
            Connection.ActiveChar.SendErrorMessage(ErrorMessageType.HouseCannotBuyAsSaleInfoChanged);
            return;
        }
        HousingManager.Instance.BuyHouse(tl, (uint)moneyAmount, Connection.ActiveChar);
    }
}
