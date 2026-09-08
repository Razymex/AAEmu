using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Models.Game.CashShop;

/// <summary>
/// Pages one product per list packet. An empty page is a closed catalog (maintenance overlay).
/// </summary>
public static class PremiumServiceCatalog
{
    public static void Send(GameConnection connection)
    {
        if (connection == null)
            return;

        var rows = PremiumServiceRules.BuildListed(id =>
        {
            var template = ItemManager.Instance.GetTemplate(id);
            if (template == null)
                return null;
            return LocalizationManager.Instance.Get("items", "name", id, template.Name);
        });

        if (rows.Count == 0)
        {
            connection.SendPacket(new SCPremiumServiceListPacket(true, 0, PremiumDetail.Unlisted, 0));
            return;
        }

        var size = (byte)rows.Count;
        for (var i = 0; i < rows.Count; i++)
        {
            var last = i == rows.Count - 1;
            connection.SendPacket(new SCPremiumServiceListPacket(last, size, rows[i], 0));
        }
    }
}
