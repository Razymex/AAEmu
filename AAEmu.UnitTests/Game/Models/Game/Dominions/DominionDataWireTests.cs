using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game;
using AAEmu.World.Core.Packets.Wz;

namespace AAEmu.UnitTests.Game.Models.Game.Dominions;

public class DominionDataWireTests
{
    [Test]
    public async Task RequiredPaddingBytes_MatchTheWorldPacket()
    {
        await Assert.That(DominionData.RequiredPaddingBytes).IsEqualTo(36);
        await Assert.That(WZDominionDataPacket.RequiredPaddingBytes)
            .IsEqualTo(DominionData.RequiredPaddingBytes);
    }

    [Test]
    public async Task Write_EndsWithTheRequiredZeroPad()
    {
        var body = new DominionData
        {
            ZoneId = 33,
            TerritoryData = new DominionTerritoryData(),
            SiegeTimers = new DominionSiegeTimers()
        }.Write(new PacketStream()).GetBytes();

        await Assert.That(body.Length).IsGreaterThanOrEqualTo(DominionData.RequiredPaddingBytes);
        var pad = body.AsSpan(body.Length - DominionData.RequiredPaddingBytes).ToArray();
        await Assert.That(pad.All(b => b == 0)).IsTrue();

        var withoutPad = body.AsSpan(0, body.Length - DominionData.RequiredPaddingBytes).ToArray();
        await Assert.That(withoutPad.Length + DominionData.RequiredPaddingBytes).IsEqualTo(body.Length);
    }
}
