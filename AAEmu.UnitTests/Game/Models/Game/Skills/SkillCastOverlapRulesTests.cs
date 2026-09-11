using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillCastOverlapRulesTests
{
    [Test]
    public async Task FlameboltComboHits_AreInstantCombo()
    {
        await Assert.That(SkillCastOverlapRules.IsInstantComboHit(0, 10)).IsTrue();
        await Assert.That(SkillCastOverlapRules.BypassesSharedCastGate(0, 10)).IsTrue();
        await Assert.That(SkillCastOverlapRules.IsInstantComboHit(1000, 1000)).IsFalse();
        await Assert.That(SkillCastOverlapRules.IsInstantComboHit(0, 0)).IsFalse();
        await Assert.That(SkillCastOverlapRules.IsInstantComboHit(0, 1000)).IsFalse();
        await Assert.That(SkillCastOverlapRules.ArmsSharedGlobalCooldown(0, 10)).IsTrue();
        await Assert.That(SkillCastOverlapRules.ArmsSharedGlobalCooldown(1000, 1000)).IsTrue();
        await Assert.That(SkillCastOverlapRules.ArmsSharedGlobalCooldown(0, 0)).IsFalse();
    }

    [Test]
    public async Task ComboHit_DoesNotCancelParentPlot()
    {
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: true,
            incomingIsInstantCombo: true,
            incomingSameSkill: false,
            sportFishWouldCancel: true)).IsFalse();
    }

    [Test]
    public async Task SameSkillRepeat_DoesNotCancelInFlightPlot()
    {
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: true,
            incomingIsInstantCombo: false,
            incomingSameSkill: true,
            sportFishWouldCancel: true)).IsFalse();
    }

    [Test]
    public async Task ComboFollowUpPlot_IsNotCancelledByParent()
    {
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: true,
            incomingIsInstantCombo: false,
            incomingSameSkill: false,
            sportFishWouldCancel: true,
            previousIsComboFollowUpOfIncoming: true)).IsFalse();
    }

    [Test]
    public async Task OtherSkill_StillCancelsABusyPlot()
    {
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: true,
            incomingIsInstantCombo: false,
            incomingSameSkill: false,
            sportFishWouldCancel: true)).IsTrue();
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: false,
            incomingIsInstantCombo: false,
            incomingSameSkill: false,
            sportFishWouldCancel: false)).IsFalse();
    }

    [Test]
    public async Task FishingHoldSwap_StillCancelsThroughSportFishRule()
    {
        var holdSwap = SportFishCombat.ShouldCancelPreviousPlot(true, previousWasHold: true, incomingIsHold: true);
        await Assert.That(holdSwap).IsTrue();
        await Assert.That(SkillCastOverlapRules.ShouldCancelPreviousPlot(
            previousWasBusy: true,
            incomingIsInstantCombo: false,
            incomingSameSkill: false,
            sportFishWouldCancel: holdSwap)).IsTrue();
    }
}
