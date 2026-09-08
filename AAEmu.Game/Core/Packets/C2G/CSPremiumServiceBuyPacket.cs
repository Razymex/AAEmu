using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSPremiumServiceBuyPacket() : GamePacket(CSOffsets.CSPremiumServiceBuyPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var cid = stream.ReadInt32();

        Logger.Info("PremiumServiceBuy refused, CId: {0} — Patron is granted, not sold", cid);
        Connection.ActiveChar?.SendErrorMessage(ErrorMessageType.PremiumServiceBuyFail);
        // Refreshing the list drops the wait overlay the Buy confirm leaves open.
        PremiumServiceCatalog.Send(Connection);
    }
}
