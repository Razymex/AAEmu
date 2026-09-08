using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Shared Progress-act gates. After-accept counters never use a lifetime total.
/// </summary>
public static class QuestProgressActRules
{
    public static bool ObjectiveMet(int current, int required, int score = 0)
        => score > 0
            ? current * required > score
            : current >= required;

    /// <summary>
    /// An unimplemented Progress act must still occupy an objective slot so Accept
    /// cannot walk Start → Reward.
    /// </summary>
    public static int HoldCount(int currentCount)
        => currentCount > 0 ? currentCount : 1;

    public static void HoldUntilImplemented(QuestActTemplate template)
    {
        if (template == null)
            return;
        template.CountsAsAnObjective = true;
        template.Count = HoldCount(template.Count);
    }

    public static bool LevelInRange(int value, int min, int max)
    {
        if (min > 0 && value < min)
            return false;
        return max <= 0 || value <= max;
    }

    public static bool NpcGradeAllowed(
        NpcGradeType grade,
        bool normal,
        bool strong,
        bool elite,
        bool bossA,
        bool bossB,
        bool bossC)
        => grade switch
        {
            NpcGradeType.Normal => normal,
            NpcGradeType.Strong => strong,
            NpcGradeType.Elite => elite,
            NpcGradeType.BossA => bossA,
            NpcGradeType.BossB => bossB,
            NpcGradeType.BossC => bossC,
            _ => false
        };

    public static bool PcLevelGapOk(int killerLevel, int victimLevel, int levelGap)
    {
        if (levelGap <= 0)
            return true;
        return Math.Abs(killerLevel - victimLevel) <= levelGap;
    }

    /// <summary>
    /// Complete-quest-group is after-accept only. Lifetime completion of the group
    /// must not finish the daily / Arche Pass contract.
    /// </summary>
    public static bool CountsTowardCompleteQuestGroup(uint completedQuestId, uint selfQuestId)
        => completedQuestId != 0 && completedQuestId != selfQuestId;
}
