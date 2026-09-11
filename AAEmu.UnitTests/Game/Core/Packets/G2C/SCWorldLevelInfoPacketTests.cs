using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCWorldLevelInfoPacketTests
{
    [Test]
    public async Task Write_IsSixNamedU32s()
    {
        var info = new WorldLevelInfoWire(55, 9, 10000, 55, 55, 0);
        var body = new SCWorldLevelInfoPacket(info).Write(new PacketStream()).GetBytes();

        await Assert.That(body.Length).IsEqualTo(24);
        await Assert.That(BitConverter.ToUInt32(body, 0)).IsEqualTo(55u);
        await Assert.That(BitConverter.ToUInt32(body, 4)).IsEqualTo(9u);
        await Assert.That(BitConverter.ToUInt32(body, 8)).IsEqualTo(10000u);
        await Assert.That(BitConverter.ToUInt32(body, 12)).IsEqualTo(55u);
        await Assert.That(BitConverter.ToUInt32(body, 16)).IsEqualTo(55u);
        await Assert.That(BitConverter.ToInt32(body, 20)).IsEqualTo(0);
    }

    [Test]
    public async Task Write_DoesNotReuseTheMispackedCapture()
    {
        var info = WorldLevelInfoRules.ForCharacter(
            55,
            55,
            [new WorldLevelHardCapRow(9, 9999, 55, true)],
            [new WorldLevelExpModifierRow(-1, 1, 10000)]);
        var body = new SCWorldLevelInfoPacket(info).Write(new PacketStream()).GetBytes();

        await Assert.That(BitConverter.ToUInt32(body, 0)).IsNotEqualTo(42u);
        await Assert.That(BitConverter.ToUInt32(body, 4)).IsNotEqualTo(6u);
    }
}

public class SCServerInfoPacketTests
{
    [Test]
    public async Task Write_IsUnixSecondsU64()
    {
        const long open = 1_777_136_000;
        var body = new SCServerInfoPacket(open).Write(new PacketStream()).GetBytes();

        await Assert.That(body.Length).IsEqualTo(8);
        await Assert.That(BitConverter.ToInt64(body, 0)).IsEqualTo(open);
    }
}
