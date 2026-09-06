using AAEmu.Game.GameData;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Sieges;

/// <summary>
/// Calendar math for <c>siege_zones</c> + <c>siege_plans</c>. A missing schedule or week is Peace
/// and a closed declare window — do not invent a fallback zone or a 7-day timer.
/// </summary>
public static class SiegeScheduleRules
{
    /// <summary>
    /// Latest <c>siege_plans.week_start</c> that is not after <paramref name="atUtc"/>.
    /// </summary>
    public static DateTime? CurrentCycleWeekStart(IEnumerable<DateTime> weekStarts, DateTime atUtc)
    {
        DateTime? best = null;
        foreach (var weekStart in weekStarts)
        {
            if (weekStart > atUtc)
                continue;
            if (best == null || weekStart > best)
                best = weekStart;
        }

        return best;
    }

    public static bool IsDeclareWindowOpen(SiegeZoneSchedule schedule, DateTime? weekStart, DateTime atUtc)
    {
        if (schedule == null || weekStart is not { } ws)
            return false;
        return atUtc >= schedule.DeclareDominionStart(ws) && atUtc < schedule.DeclareDominionEnd(ws);
    }

    public static SiegePeriod GetScheduledPeriod(SiegeZoneSchedule schedule, DateTime? weekStart, DateTime atUtc)
    {
        if (schedule == null || weekStart is not { } ws)
            return SiegePeriod.Peace;

        if (atUtc >= schedule.SiegeStart(ws) && atUtc < schedule.SiegeEnd(ws))
            return SiegePeriod.Siege;
        if (atUtc >= schedule.ReadyToSiegeStart(ws) && atUtc < schedule.SiegeStart(ws))
            return SiegePeriod.ReadyToSiege;
        if (atUtc >= schedule.HeroVolunteerStart(ws) && atUtc < schedule.ReadyToSiegeStart(ws))
            return SiegePeriod.HeroVolunteer;

        return SiegePeriod.Peace;
    }
}
