using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.ScheduleItems;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCScheduleItemUpdatePacketTests
{
    [Test]
    public async Task EmptyList_WritesAZeroCountByte()
    {
        var body = Body([]);

        await Assert.That(body.Length).IsEqualTo(1);
        await Assert.That(body[0]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task OneRow_IsCountPlusTwentyOneBytes()
    {
        var updated = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
        var body = Body([
            new ScheduleItem { Type = 1, Gave = 1, Cumulated = 120, Updated = updated }
        ]);

        await Assert.That(body.Length).IsEqualTo(ScheduleItemRules.BodyBytes(1));
        await Assert.That(body[0]).IsEqualTo((byte)1);
        await Assert.That(BitConverter.ToInt32(body, 1)).IsEqualTo(1);
        await Assert.That(body[5]).IsEqualTo((byte)1);
        await Assert.That(BitConverter.ToInt64(body, 6)).IsEqualTo(120);
        await Assert.That(BitConverter.ToInt64(body, 14)).IsEqualTo(
            new DateTimeOffset(updated).ToUnixTimeSeconds());
    }

    private static byte[] Body(List<ScheduleItem> items) =>
        new SCScheduleItemUpdatePacket(items).Write(new PacketStream()).GetBytes();
}
