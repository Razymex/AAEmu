using AAEmu.Game.Models.Game.NPChar;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class ZoneMirrorIdRulesTests
{
    [Test]
    public async Task SameZoneMirror_IsIdempotent()
    {
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(288, 0, true, 288, 0)).IsTrue();
    }

    [Test]
    public async Task OtherZoneOrWorldUnit_IsNotTheSameMirror()
    {
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(282, 0, true, 288, 0)).IsFalse();
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(288, 0, false, 288, 0)).IsFalse();
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(0, 0, true, 288, 0)).IsFalse();
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(288, 0, true, 0, 0)).IsFalse();
        await Assert.That(ZoneMirrorIdRules.IsIdempotentRemirror(288, 1, true, 288, 2)).IsFalse();
    }
}
