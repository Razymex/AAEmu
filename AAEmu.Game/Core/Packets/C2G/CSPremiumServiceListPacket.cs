using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSPremiumServiceListPacket() : GamePacket(CSOffsets.CSPremiumServiceListPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        PremiumServiceCatalog.Send(Connection);
    }
}
