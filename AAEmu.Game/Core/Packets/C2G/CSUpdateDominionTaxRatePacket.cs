using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSUpdateDominionTaxRatePacket() : GamePacket(CSOffsets.CSUpdateDominionTaxRatePacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var id = stream.ReadUInt16();
        var taxRate = stream.ReadInt32();

        Logger.Debug("UpdateDominionTaxRate, Id: {0}, TaxRate: {1}", id, taxRate);

        // Guild-claimed zone groups live in GuildDominionManager, Hero/faction claims in DominionManager.
        // Same guild-first disjoint check as HousingManager.Build.
        if (GuildDominionManager.Instance.GetByZoneId(id) != null)
            GuildDominionManager.Instance.UpdateTaxRate(Connection, id, taxRate);
        else
            DominionManager.Instance.UpdateTaxRate(Connection, id, taxRate);
    }
}
