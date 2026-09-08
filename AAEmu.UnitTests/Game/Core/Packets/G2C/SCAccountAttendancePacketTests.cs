using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.AccountAttendance;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCAccountAttendancePacketTests
{
    [Test]
    public async Task EmptyMonth_IsThirtyOneZeroSlots()
    {
        var body = Body();

        await Assert.That(body.Length).IsEqualTo(AccountAttendanceRules.BodyBytes);
        await Assert.That(body.All(x => x == 0)).IsTrue();
    }

    [Test]
    public async Task ClaimedDay_WritesUnixTimeAndArchelifeInASixteenByteSlot()
    {
        var times = new long[AccountAttendanceRules.DaysInPacket];
        var life = new bool[AccountAttendanceRules.DaysInPacket];
        times[5] = 1_725_000_000;
        life[5] = true;

        var body = Body(times, life);
        var offset = 5 * AccountAttendanceRules.SlotBytes;

        await Assert.That(body.Length).IsEqualTo(AccountAttendanceRules.BodyBytes);
        await Assert.That(BitConverter.ToInt64(body, offset)).IsEqualTo(1_725_000_000);
        await Assert.That(body[offset + 8]).IsEqualTo((byte)1);
        await Assert.That(body.Skip(offset + 9).Take(7).All(x => x == 0)).IsTrue();
    }

    private static byte[] Body(long[] times = null, bool[] archelife = null) =>
        new SCAccountAttendancePacket(times, archelife).Write(new PacketStream()).GetBytes();
}
