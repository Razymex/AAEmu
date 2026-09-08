using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Quests;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestProgressActRulesTests
{
    [Test]
    public async Task ObjectiveMet_NeedsTheCount_AndDoesNotTreatZeroAsDone()
    {
        await Assert.That(QuestProgressActRules.ObjectiveMet(0, 1000)).IsFalse();
        await Assert.That(QuestProgressActRules.ObjectiveMet(999, 1000)).IsFalse();
        await Assert.That(QuestProgressActRules.ObjectiveMet(1000, 1000)).IsTrue();
        await Assert.That(QuestProgressActRules.ObjectiveMet(0, 0)).IsTrue();
    }

    [Test]
    public async Task HoldCount_KeepsAMissingProgressActOpen()
    {
        await Assert.That(QuestProgressActRules.HoldCount(0)).IsEqualTo(1);
        await Assert.That(QuestProgressActRules.HoldCount(3)).IsEqualTo(3);
    }

    [Test]
    public async Task CompleteQuestGroup_IgnoresLifetimeAndSelf()
    {
        await Assert.That(QuestProgressActRules.CountsTowardCompleteQuestGroup(0, 10164)).IsFalse();
        await Assert.That(QuestProgressActRules.CountsTowardCompleteQuestGroup(10164, 10164)).IsFalse();
        await Assert.That(QuestProgressActRules.CountsTowardCompleteQuestGroup(9044, 10164)).IsTrue();
    }

    [Test]
    public async Task LevelInRange_ZeroMaxIsOpen()
    {
        await Assert.That(QuestProgressActRules.LevelInRange(29, 30, 0)).IsFalse();
        await Assert.That(QuestProgressActRules.LevelInRange(30, 30, 0)).IsTrue();
        await Assert.That(QuestProgressActRules.LevelInRange(55, 30, 0)).IsTrue();
        await Assert.That(QuestProgressActRules.LevelInRange(56, 30, 55)).IsFalse();
    }

    [Test]
    public async Task NpcGradeAllowed_UsesTheListedFlags()
    {
        await Assert.That(QuestProgressActRules.NpcGradeAllowed(
            NpcGradeType.Normal, true, true, true, false, false, false)).IsTrue();
        await Assert.That(QuestProgressActRules.NpcGradeAllowed(
            NpcGradeType.BossA, true, true, true, false, false, false)).IsFalse();
        await Assert.That(QuestProgressActRules.NpcGradeAllowed(
            NpcGradeType.Weak, true, true, true, false, false, false)).IsFalse();
    }

    [Test]
    public async Task PcLevelGap_RejectsFarLevels()
    {
        await Assert.That(QuestProgressActRules.PcLevelGapOk(50, 50, 4)).IsTrue();
        await Assert.That(QuestProgressActRules.PcLevelGapOk(50, 54, 4)).IsTrue();
        await Assert.That(QuestProgressActRules.PcLevelGapOk(50, 55, 4)).IsFalse();
        await Assert.That(QuestProgressActRules.PcLevelGapOk(50, 1, 0)).IsTrue();
    }
}
