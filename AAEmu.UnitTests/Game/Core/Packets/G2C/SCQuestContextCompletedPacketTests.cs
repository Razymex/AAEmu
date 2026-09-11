using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCQuestContextCompletedPacketTests
{
    [Test]
    public async Task Body_IsQuestThenComponent()
    {
        var body = new SCQuestContextCompletedPacket(2385, 10255).Write(new PacketStream()).GetBytes();

        await Assert.That(body.Length).IsEqualTo(8);
        await Assert.That(BitConverter.ToInt32(body, 0)).IsEqualTo(2385);
        await Assert.That(BitConverter.ToInt32(body, 4)).IsEqualTo(10255);
    }
}
