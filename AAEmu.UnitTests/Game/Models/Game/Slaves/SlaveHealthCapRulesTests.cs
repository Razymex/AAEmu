using AAEmu.Game.Models.Game.Slaves;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

public class SlaveHealthCapRulesTests
{
    [Test]
    public async Task FullBar_FollowsARaisedCap()
    {
        await Assert.That(SlaveHealthCapRules.AfterMaxHpChanged(95000, 95000, 104500))
            .IsEqualTo(104500);
    }

    [Test]
    public async Task DamagedBar_StaysDamaged()
    {
        await Assert.That(SlaveHealthCapRules.AfterMaxHpChanged(40000, 95000, 104500))
            .IsEqualTo(40000);
    }

    [Test]
    public async Task OverCapSaved_ClampsWhenNotFullAgainstOldMax()
    {
        // Saved Ezi-full HP vs the bare cap still counts as full, so kit/Ezi raise stays full.
        await Assert.That(SlaveHealthCapRules.AfterMaxHpChanged(104500, 85000, 95000))
            .IsEqualTo(95000);
    }

    [Test]
    public async Task NewMaxZero_DoesNotGoNegative()
    {
        await Assert.That(SlaveHealthCapRules.AfterMaxHpChanged(10, 10, 0)).IsEqualTo(10);
    }
}
