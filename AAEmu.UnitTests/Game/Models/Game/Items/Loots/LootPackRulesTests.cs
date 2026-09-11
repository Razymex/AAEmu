using AAEmu.Game.Models.Game.Items.Loots;

namespace AAEmu.UnitTests.Game.Models.Game.Items.Loots;

public class LootPackRulesTests
{
    [Test]
    public async Task MergeQuestItems_WhenGroupWasNotRolled_AddsQuestItems()
    {
        var selected = new Dictionary<uint, List<Loot>>();
        var quest = new Loot { Id = 10, Group = 1, ItemId = 100 };
        var questByGroup = new Dictionary<uint, List<Loot>> { [1] = [quest] };

        LootPackRules.MergeQuestItems(selected, questByGroup, 1);

        await Assert.That(selected.ContainsKey(1)).IsTrue();
        await Assert.That(selected[1]).Contains(quest);
    }

    [Test]
    public async Task MergeQuestItems_WhenGroupAlreadyHasItem_DoesNotDuplicate()
    {
        var quest = new Loot { Id = 10, Group = 1, ItemId = 100 };
        var selected = new Dictionary<uint, List<Loot>> { [1] = [quest] };
        var questByGroup = new Dictionary<uint, List<Loot>> { [1] = [quest] };

        LootPackRules.MergeQuestItems(selected, questByGroup, 1);

        await Assert.That(selected[1].Count).IsEqualTo(1);
    }

    [Test]
    public async Task MergeQuestItems_MissingQuestGroup_LeavesSelectedAlone()
    {
        var selected = new Dictionary<uint, List<Loot>>();
        var questByGroup = new Dictionary<uint, List<Loot>>
        {
            [2] = [new Loot { Id = 11, Group = 2 }]
        };

        LootPackRules.MergeQuestItems(selected, questByGroup, 1);

        await Assert.That(selected.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MergeQuestItems_NullMaps_DoNotThrow()
    {
        var empty = new Dictionary<uint, List<Loot>>();
        LootPackRules.MergeQuestItems(null, empty, 1);
        LootPackRules.MergeQuestItems(empty, null, 1);
        await Assert.That(empty.Count).IsEqualTo(0);
    }
}
