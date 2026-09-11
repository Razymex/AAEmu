using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.World;

public class VisibleObjectResendRulesTests
{
    [Test]
    public async Task CinemaDrop_RepaintsEvenWhenAlreadyStreamed()
    {
        await Assert.That(VisibleObjectResendRules.ShouldRepaintMirror(true, true)).IsTrue();
        await Assert.That(VisibleObjectResendRules.ShouldRepaintMirror(false, true)).IsTrue();
        await Assert.That(VisibleObjectResendRules.ShouldResendDoodadCreates(true)).IsTrue();
    }

    [Test]
    public async Task TeleportOrLogin_SkipsAlreadyStreamed()
    {
        await Assert.That(VisibleObjectResendRules.ShouldRepaintMirror(true, false)).IsFalse();
        await Assert.That(VisibleObjectResendRules.ShouldRepaintMirror(false, false)).IsTrue();
        await Assert.That(VisibleObjectResendRules.ShouldResendDoodadCreates(false)).IsFalse();
    }
}
