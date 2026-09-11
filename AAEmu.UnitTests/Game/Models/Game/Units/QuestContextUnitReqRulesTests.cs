using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class QuestContextUnitReqRulesTests
{
    [Test]
    public async Task JournalProgress_CountsRegardlessOfStep()
    {
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Progress)).IsTrue();
    }

    [Test]
    public async Task ReadyOrCompleted_IsNotInProgress()
    {
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Ready)).IsFalse();
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Completed)).IsFalse();
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Dropped)).IsFalse();
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Failed)).IsFalse();
    }

    [Test]
    public async Task MissingQuest_IsNotInProgress()
    {
        await Assert.That(QuestContextUnitReqRules.IsInProgress(null)).IsFalse();
        await Assert.That(QuestContextUnitReqRules.IsInProgress(QuestStatus.Invalid)).IsFalse();
    }
}
