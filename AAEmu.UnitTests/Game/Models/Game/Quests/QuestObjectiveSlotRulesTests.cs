using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestObjectiveSlotRulesTests
{
    [Test]
    public async Task ProgressJournal_StaysOnSlotZero_NoneActsTakeLeftovers()
    {
        var cinema = QuestObjectiveSlotRules.SlotForProgressComponent(0, 5);
        byte other = 1;
        var interaction = QuestObjectiveSlotRules.NextIndex(ref other, true, 5);
        var gather = QuestObjectiveSlotRules.NextIndex(ref other, true, 5);
        var report = QuestObjectiveSlotRules.NextIndex(ref other, false, 5);

        await Assert.That(cinema).IsEqualTo((byte)0);
        await Assert.That(interaction).IsEqualTo((byte)1);
        await Assert.That(gather).IsEqualTo((byte)2);
        await Assert.That(report).IsEqualTo(QuestObjectiveSlotRules.NoSlot);
    }

    [Test]
    public async Task ActsInTheSameProgressComponent_ShareOneSlot()
    {
        var first = QuestObjectiveSlotRules.SlotForProgressComponent(0, 5);
        var second = QuestObjectiveSlotRules.SlotForProgressComponent(0, 5);
        var nextComponent = QuestObjectiveSlotRules.SlotForProgressComponent(1, 5);

        await Assert.That(first).IsEqualTo((byte)0);
        await Assert.That(second).IsEqualTo((byte)0);
        await Assert.That(nextComponent).IsEqualTo((byte)1);
    }

    [Test]
    public async Task ProgressOnly_KeepsComponentLocalIndexes()
    {
        await Assert.That(QuestObjectiveSlotRules.SlotForProgressComponent(0, 5)).IsEqualTo((byte)0);
        await Assert.That(QuestObjectiveSlotRules.SlotForProgressComponent(1, 5)).IsEqualTo((byte)1);
        await Assert.That(QuestObjectiveSlotRules.SlotForProgressComponent(2, 5)).IsEqualTo((byte)2);
        await Assert.That(QuestObjectiveSlotRules.SlotForProgressComponent(5, 5)).IsEqualTo(QuestObjectiveSlotRules.NoSlot);
        await Assert.That(QuestObjectiveSlotRules.SlotForProgressComponent(-1, 5)).IsEqualTo(QuestObjectiveSlotRules.NoSlot);
    }

    [Test]
    public async Task NoneOnly_StartsAtZero()
    {
        byte actIndex = 0;
        var first = QuestObjectiveSlotRules.NextIndex(ref actIndex, true, 5);
        var second = QuestObjectiveSlotRules.NextIndex(ref actIndex, true, 5);

        await Assert.That(first).IsEqualTo((byte)0);
        await Assert.That(second).IsEqualTo((byte)1);
    }

    [Test]
    public async Task NoneAndProgress_ReadStoredCount()
    {
        await Assert.That(QuestObjectiveSlotRules.RunActUsesStoredCount(QuestComponentKind.None, 0, 5)).IsTrue();
        await Assert.That(QuestObjectiveSlotRules.RunActUsesStoredCount(QuestComponentKind.Progress, 1, 5)).IsTrue();
        await Assert.That(QuestObjectiveSlotRules.RunActUsesStoredCount(QuestComponentKind.Start, 0, 5)).IsFalse();
        await Assert.That(QuestObjectiveSlotRules.RunActUsesStoredCount(QuestComponentKind.None, QuestObjectiveSlotRules.NoSlot, 5)).IsFalse();
    }

    [Test]
    public async Task CinemaWatch_OnlyOnProgress()
    {
        await Assert.That(QuestObjectiveSlotRules.CinemaWatchCounts(QuestComponentKind.None)).IsFalse();
        await Assert.That(QuestObjectiveSlotRules.CinemaWatchCounts(QuestComponentKind.Start)).IsFalse();
        await Assert.That(QuestObjectiveSlotRules.CinemaWatchCounts(QuestComponentKind.Progress)).IsTrue();
    }

    [Test]
    public async Task ObjectiveActMet_UsesOverrideOrStoredCount()
    {
        var objectives = new[] { 0, 1, 0, 0, 0 };
        await Assert.That(QuestObjectiveSlotRules.ObjectiveActMet(1, 1, false, objectives)).IsTrue();
        await Assert.That(QuestObjectiveSlotRules.ObjectiveActMet(0, 1, false, objectives)).IsFalse();
        await Assert.That(QuestObjectiveSlotRules.ObjectiveActMet(0, 1, true, objectives)).IsTrue();
        await Assert.That(QuestObjectiveSlotRules.ObjectiveActMet(QuestObjectiveSlotRules.NoSlot, 1, false, objectives)).IsFalse();
    }
}
