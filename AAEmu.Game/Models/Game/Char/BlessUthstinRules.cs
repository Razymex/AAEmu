using AAEmu.Commons.Network;
using AAEmu.Game.GameData;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// Bless Uthstin page list, costs, and apply checks.
/// A new character always has one empty page. The window hides unused tabs from
/// <see cref="MaxPageCount"/> and anchors the expand button to
/// <c>selectedButton[pageCount]</c>, which is invalid when the list is empty.
/// </summary>
public static class BlessUthstinRules
{
    /// <summary>Retail new-character seed. Client tabs are 1-based.</summary>
    public const int DefaultPageCount = 1;

    /// <summary>Client <c>GetMaxPageCount</c> is a fixed 3; the window creates tabs 1–3.</summary>
    public const int MaxPageCount = 3;

    public const int StatCount = 5;

    /// <summary>Five applied stats plus the two apply counters.</summary>
    public const int PageWireBytes = StatCount * sizeof(int) + sizeof(int) + sizeof(int);

    /// <summary>Skill the activate button casts. The extra byte is the 0-based page.</summary>
    public const uint SelectSkillId = 37244;

    /// <summary>Dedicated bonus slot so page stats do not collide with gear or buffs.</summary>
    public const uint BonusIndex = 0x42555354;

    public const string ConfigBaseStats = "bless_uthstin_base_stats";
    public const string ConfigMaxStatsLimit = "bless_uthstin_max_stats_limit";
    public const string ConfigExtendPerPoint = "bless_uthstin_max_stats_extend_per_point";
    public const string ConfigApplyLimit = "bless_uthstin_apply_limit_count";
    public const string ConfigInitItem = "bless_uthstin_Init_ItemType";
    public const string ConfigExtendItem = "bless_uthstin_max_stats_extend_ItemType";
    public const string ConfigInitItemCount = "bless_uthstin_Init_Item_Num";
    public const string ConfigSelectCost = "bless_uthstin_select_cost";
    public const string ConfigCopyCost = "bless_uthstin_copy_cost";
    public const string ConfigExpandItem = "bless_uthstin_expand_page_item_type";
    public const string ConfigExpandPage2 = "bless_uthstin_expand_item_count_for_page_2";
    public const string ConfigExpandPage3 = "bless_uthstin_expand_item_count_for_page_3";

    public const string FormulaApplyCountKey = "bless_uthstin_apply_count";

    public static int BaseStats => ContentConfigGameData.Instance.RequireInt(ConfigBaseStats);
    public static int MaxStatsLimit => ContentConfigGameData.Instance.RequireInt(ConfigMaxStatsLimit);
    public static int ExtendPerPoint => ContentConfigGameData.Instance.RequireInt(ConfigExtendPerPoint);
    public static int ApplyLimit => ContentConfigGameData.Instance.RequireInt(ConfigApplyLimit);
    public static uint InitItemId => ContentConfigGameData.Instance.RequireUInt(ConfigInitItem);
    public static uint ExtendItemId => ContentConfigGameData.Instance.RequireUInt(ConfigExtendItem);
    public static int InitItemCount => ContentConfigGameData.Instance.RequireInt(ConfigInitItemCount);
    public static int SelectCostBase => ContentConfigGameData.Instance.RequireInt(ConfigSelectCost);
    public static int CopyCostBase => ContentConfigGameData.Instance.RequireInt(ConfigCopyCost);
    public static uint ExpandItemId => ContentConfigGameData.Instance.RequireUInt(ConfigExpandItem);

    public static int ActivatedPageNumber(int selectPageIndex) =>
        selectPageIndex >= 0 ? selectPageIndex + 1 : DefaultPageCount;

    public static int MaxStats(int extendMaxStats) =>
        Math.Min(MaxStatsLimit, Math.Max(0, extendMaxStats) + BaseStats);

    public static bool CanExtend(int extendMaxStats) =>
        MaxStats(extendMaxStats + ExtendPerPoint) > MaxStats(extendMaxStats);

    public static int SelectCost(int level) =>
        Math.Max(0, level) * Math.Max(0, SelectCostBase);

    public static int CopyCost(int positiveApplied) =>
        Math.Max(0, positiveApplied) * Math.Max(0, CopyCostBase);

    /// <summary>First apply of a function costs 1; later counts use formula 44.</summary>
    public static int ConsumeItemCount(int applyCountForFunction, Func<int, int> evaluateFormula)
    {
        if (applyCountForFunction <= 0)
            return 1;
        if (evaluateFormula == null)
            return 1;
        return Math.Max(1, evaluateFormula(applyCountForFunction));
    }

    public static int ExtendItemCount(int nextApplyExtendCount, Func<int, int> evaluateFormula)
    {
        if (evaluateFormula == null)
            return 1;
        return Math.Max(1, evaluateFormula(Math.Max(1, nextApplyExtendCount)));
    }

    /// <summary>Need count to grow from <paramref name="currentPageCount"/> to +1.</summary>
    public static int ExpandNeedCount(int currentPageCount)
    {
        return currentPageCount switch
        {
            1 => ContentConfigGameData.Instance.RequireInt(ConfigExpandPage2),
            2 => ContentConfigGameData.Instance.RequireInt(ConfigExpandPage3),
            _ => 0
        };
    }

    public static int PositiveApplied(BlessUthstinPage page)
    {
        page ??= new BlessUthstinPage();
        var sum = 0;
        for (var i = 0; i < StatCount; i++)
        {
            var value = page.GetStat(i);
            if (value > 0)
                sum += value;
        }

        return sum;
    }

    public static IReadOnlyList<BlessUthstinPage> PagesForWire(IReadOnlyList<BlessUthstinPage> pages)
    {
        if (pages is { Count: > 0 })
        {
            if (pages.Count <= MaxPageCount)
                return pages;

            return pages.Take(MaxPageCount).ToList();
        }

        return [new BlessUthstinPage()];
    }

    public static void WritePageInfos(
        PacketStream stream,
        IReadOnlyList<BlessUthstinPage> pages,
        int selectPageIndex,
        int extendMaxStats,
        int applyExtendCount)
    {
        var wire = PagesForWire(pages);
        stream.Write(wire.Count);
        foreach (var page in wire)
            WritePage(stream, page);

        stream.Write(selectPageIndex < 0 ? 0 : selectPageIndex);
        stream.Write(extendMaxStats);
        stream.Write(applyExtendCount);
    }

    public static void WriteAppliedStats(PacketStream stream, BlessUthstinPage page)
    {
        page ??= new BlessUthstinPage();
        stream.Write(page.Strength);
        stream.Write(page.Dexterity);
        stream.Write(page.Stamina);
        stream.Write(page.Intelligence);
        stream.Write(page.Spirit);
    }

    public static void WritePage(PacketStream stream, BlessUthstinPage page)
    {
        page ??= new BlessUthstinPage();
        WriteAppliedStats(stream, page);
        stream.Write(page.ApplyNormalCount);
        stream.Write(page.ApplySpecialCount);
    }

    public static bool IsStatKind(int kind) => (uint)kind < StatCount;

    /// <summary>
    /// Same overflow / floor checks the window runs before it asks for a roll.
    /// <paramref name="liveWithoutPage"/> is the unit's current attr without this page's applied value.
    /// </summary>
    public static int ApplyRefuseReason(
        BlessUthstinPage page,
        BlessUthstinItem item,
        IReadOnlyList<int> liveWithoutPage,
        int extendMaxStats)
    {
        page ??= new BlessUthstinPage();
        if (item == null || liveWithoutPage == null || liveWithoutPage.Count < StatCount)
            return 1;

        var defaultUnderCount = 0;
        var dropStatKindCount = 0;
        var dropOnlyOneKind = -1;
        for (var i = 0; i < StatCount; i++)
        {
            if (liveWithoutPage[i] + page.GetStat(i) - item.DropCount < 0)
                defaultUnderCount++;
            else if (item.DropWeights[i] > 0)
            {
                dropStatKindCount++;
                dropOnlyOneKind = i;
            }
        }

        if (dropStatKindCount <= 0)
            return 1;
        if (dropStatKindCount > 1)
            dropOnlyOneKind = -1;
        if (defaultUnderCount > 3)
            return 1;
        if (dropOnlyOneKind >= 0)
        {
            if (liveWithoutPage[dropOnlyOneKind] + page.GetStat(dropOnlyOneKind) - item.DropCount < 0)
                return 1;
            if (item.RiseWeights[dropOnlyOneKind] > 0)
                return 2;
        }

        if (PositiveApplied(page) + item.RiseCount > MaxStats(extendMaxStats))
        {
            if (dropOnlyOneKind < 0 || page.GetStat(dropOnlyOneKind) - item.DropCount < 0)
                return 3;
        }

        return 0;
    }

    public static bool TryPickWeighted(IReadOnlyList<int> weights, int excludeKind, Func<int, int> next, out int kind)
    {
        kind = -1;
        if (weights == null || weights.Count < StatCount || next == null)
            return false;

        var total = 0;
        for (var i = 0; i < StatCount; i++)
        {
            if (i == excludeKind)
                continue;
            if (weights[i] > 0)
                total += weights[i];
        }

        if (total <= 0)
            return false;

        var pick = next(total);
        if (pick < 0)
            pick = 0;
        pick %= total;
        var cursor = 0;
        for (var i = 0; i < StatCount; i++)
        {
            if (i == excludeKind || weights[i] <= 0)
                continue;
            cursor += weights[i];
            if (pick < cursor)
            {
                kind = i;
                return true;
            }
        }

        return false;
    }

    public static bool TryRoll(BlessUthstinItem item, Func<int, int> next, out int incKind, out int decKind)
    {
        incKind = -1;
        decKind = -1;
        if (item == null)
            return false;
        if (!TryPickWeighted(item.RiseWeights, -1, next, out incKind))
            return false;
        return TryPickWeighted(item.DropWeights, incKind, next, out decKind);
    }

    public static void ApplyRoll(BlessUthstinPage page, int incKind, int decKind, int incPoints, int decPoints, bool special)
    {
        page ??= new BlessUthstinPage();
        if (IsStatKind(incKind))
            page.SetStat(incKind, page.GetStat(incKind) + incPoints);
        if (IsStatKind(decKind))
            page.SetStat(decKind, page.GetStat(decKind) - decPoints);
        if (special)
            page.ApplySpecialCount++;
        else
            page.ApplyNormalCount++;
    }
}
