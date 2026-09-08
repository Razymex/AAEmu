using System.Text;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCPremiumServiceListPacketTests
{
    [Test]
    public async Task ClosedCatalog_WritesEndAndZeroSize()
    {
        var body = new SCPremiumServiceListPacket(true, 0, PremiumDetail.Unlisted, 0)
            .Write(new PacketStream())
            .GetBytes();

        await Assert.That(body[0]).IsEqualTo((byte)1);
        await Assert.That(body[1]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task ListedRow_WritesProductThenEmptyUrlAndBuyLimit()
    {
        var detail = PremiumServiceRules.CreateDetail(new PremiumServiceRules.Pass(49190, 30), 1, "thirty");
        var body = new SCPremiumServiceListPacket(true, 8, detail, 0)
            .Write(new PacketStream())
            .GetBytes();

        await Assert.That(body[0]).IsEqualTo((byte)1);
        await Assert.That(body[1]).IsEqualTo((byte)8);
        await Assert.That(BitConverter.ToInt32(body, 2)).IsEqualTo(49190);

        var nameLen = BitConverter.ToUInt16(body, 6);
        var name = Encoding.UTF8.GetString(body, 8, nameLen);
        await Assert.That(name).IsEqualTo("thirty");

        var afterName = 8 + nameLen;
        await Assert.That(BitConverter.ToUInt16(body, afterName)).IsEqualTo((ushort)1);
        await Assert.That(body[afterName + 2]).IsEqualTo((byte)1);
        await Assert.That(body[afterName + 3]).IsEqualTo((byte)0);
        await Assert.That(BitConverter.ToInt32(body, afterName + 4)).IsEqualTo(720);
        await Assert.That(body[afterName + 8]).IsEqualTo(PremiumServiceRules.PriceTypeAaCash);
        await Assert.That(BitConverter.ToInt32(body, afterName + 9)).IsEqualTo(0);
        await Assert.That(BitConverter.ToUInt32(body, afterName + 13)).IsEqualTo(0u);
        await Assert.That(BitConverter.ToInt32(body, afterName + 17)).IsEqualTo(0);

        var urlLen = BitConverter.ToUInt16(body, afterName + 21);
        await Assert.That(urlLen).IsEqualTo((ushort)0);
        var afterUrl = afterName + 23;
        await Assert.That(BitConverter.ToInt32(body, afterUrl)).IsEqualTo(0);
        await Assert.That(BitConverter.ToInt32(body, afterUrl + 4)).IsEqualTo(PremiumServiceRules.ListedBuyLimit);
        await Assert.That(BitConverter.ToInt32(body, afterUrl + 8)).IsEqualTo(0);
    }
}
