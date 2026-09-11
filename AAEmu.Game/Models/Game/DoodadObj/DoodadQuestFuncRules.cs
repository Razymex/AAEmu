using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// Compact <c>doodad_func_quests.quest_kind_id</c>: 1 starts a quest, 2 turns one in.
/// One doodad phase can list several of each; only the matching row may run.
/// </summary>
public static class DoodadQuestFuncRules
{
    public const uint AcceptKind = 1;
    public const uint ReportKind = 2;

    public static bool IsAcceptKind(uint questKindId) => questKindId == AcceptKind;

    public static bool IsReportKind(uint questKindId) => questKindId == ReportKind;

    public static bool ShouldOfferAccept(uint questKindId, bool hasQuest, bool completed, bool repeatable)
    {
        if (!IsAcceptKind(questKindId) || hasQuest)
            return false;
        return repeatable || !completed;
    }

    public static bool ShouldReport(uint questKindId, bool hasQuest)
    {
        return IsReportKind(questKindId) && hasQuest;
    }

    /// <summary>
    /// Ready (or let-it-done early) turn-in. World offers the complete window; the client
    /// sends <c>CSCompleteQuestContext</c> after the last line.
    /// </summary>
    public static bool ShouldOfferComplete(QuestObjectiveStatus status, bool letItDone)
    {
        var minimum = letItDone
            ? QuestObjectiveStatus.CanEarlyComplete
            : QuestObjectiveStatus.QuestComplete;
        return status >= minimum;
    }

    /// <summary>
    /// Ready step/status is already past Progress counters (those acts are finalized).
    /// Count the journal Ready flag, not only leftover objective cells.
    /// </summary>
    public static bool ShouldOfferComplete(
        QuestObjectiveStatus objectiveStatus,
        bool letItDone,
        QuestStatus questStatus,
        QuestComponentKind step)
    {
        if (questStatus == QuestStatus.Ready || step == QuestComponentKind.Ready)
            return true;
        return ShouldOfferComplete(objectiveStatus, letItDone);
    }

    /// <summary>
    /// A Use that found no matching func must not credit <c>QuestActObjInteraction</c>.
    /// Credit is tied to that caster's use, not a shared doodad flag.
    /// </summary>
    public static bool ShouldCountInteraction(bool useAppliedFunc) => useAppliedFunc;

    public static bool ShouldCountInteractionForCaster(uint casterObjId, uint recordedCasterObjId, bool applied) =>
        casterObjId != 0 && casterObjId == recordedCasterObjId && ShouldCountInteraction(applied);

    /// <summary>
    /// Prefer an in-progress report, else the first startable accept.
    /// </summary>
    public static T Select<T>(
        IEnumerable<T> funcs,
        Func<T, uint> questKindId,
        Func<T, uint> questId,
        Func<uint, bool> hasQuest,
        Func<uint, bool> completed,
        Func<uint, bool> repeatable,
        Func<uint, bool> canStart = null)
    {
        if (funcs == null || questKindId == null || questId == null || hasQuest == null || completed == null)
            return default;

        repeatable ??= static _ => false;
        canStart ??= static _ => true;

        T accept = default;
        var haveAccept = false;
        foreach (var func in funcs)
        {
            var id = questId(func);
            if (id == 0)
                continue;

            var kind = questKindId(func);
            if (ShouldReport(kind, hasQuest(id)))
                return func;

            if (haveAccept)
                continue;

            if (ShouldOfferAccept(kind, hasQuest(id), completed(id), repeatable(id)) && canStart(id))
            {
                accept = func;
                haveAccept = true;
            }
        }

        return haveAccept ? accept : default;
    }
}
