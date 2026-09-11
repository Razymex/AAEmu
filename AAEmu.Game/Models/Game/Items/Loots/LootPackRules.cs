namespace AAEmu.Game.Models.Game.Items.Loots;

/// <summary>
/// Quest rows in a rolled loot group always drop. The non-quest roll can miss that
/// group, so the merge must create the list instead of indexing it.
/// </summary>
public static class LootPackRules
{
    public static void MergeQuestItems(
        IDictionary<uint, List<Loot>> selectedItemsByGroup,
        IReadOnlyDictionary<uint, List<Loot>> questItemsByGroup,
        uint groupNo)
    {
        if (selectedItemsByGroup == null || questItemsByGroup == null)
            return;
        if (!questItemsByGroup.TryGetValue(groupNo, out var questLoots) || questLoots == null)
            return;

        if (!selectedItemsByGroup.TryGetValue(groupNo, out var selected) || selected == null)
        {
            selected = [];
            selectedItemsByGroup[groupNo] = selected;
        }

        foreach (var loot in questLoots)
        {
            if (loot == null || selected.Contains(loot))
                continue;
            selected.Add(loot);
        }
    }
}
