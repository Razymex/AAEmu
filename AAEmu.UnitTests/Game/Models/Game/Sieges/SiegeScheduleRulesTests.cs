using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Sieges;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.UnitTests.Game.Models.Game.Sieges;

public class SiegeScheduleRulesTests
{
    /// <summary>
    /// All four shipped <c>siege_zones</c> rows (33/34/43/44) share this template. Weekday
    /// offsets are added to a Tuesday <c>siege_plans.week_start</c>.
    /// </summary>
    private static SiegeZoneSchedule ShippedSiegeZone() => new()
    {
        ZoneGroupId = 33,
        StartHeroVolunteerWeekdayOffset = 3,
        StartHeroVolunteerHour = 12,
        StartReadyToSiegeWeekdayOffset = 3,
        StartReadyToSiegeHour = 20,
        StartDeclareDominionWeekdayOffset = 3,
        StartDeclareDominionHour = 20,
        DeclareDominionDuration = TimeSpan.FromHours(1),
        StartSiegeWeekdayOffset = 3,
        StartSiegeHour = 21,
        SiegeDuration = TimeSpan.FromHours(1)
    };

    // Tuesday 2026-09-01, matching a real siege_plans.week_start weekday.
    private static readonly DateTime WeekStart = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CurrentCycleWeekStart_PicksTheLatestWeekNotAfterNow()
    {
        DateTime[] weeks =
        [
            new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc)
        ];

        await Assert.That(SiegeScheduleRules.CurrentCycleWeekStart(weeks, weeks[0].AddDays(3)))
            .IsEqualTo(weeks[0]);
        await Assert.That(SiegeScheduleRules.CurrentCycleWeekStart(weeks, weeks[1]))
            .IsEqualTo(weeks[1]);
        await Assert.That(SiegeScheduleRules.CurrentCycleWeekStart(weeks, weeks[0].AddDays(-1)))
            .IsNull();
        await Assert.That(SiegeScheduleRules.CurrentCycleWeekStart([], WeekStart)).IsNull();
    }

    [Test]
    public async Task IsDeclareWindowOpen_UsesTheShippedFridayHour()
    {
        var schedule = ShippedSiegeZone();
        // Friday 2026-09-04 20:00–21:00.
        var open = new DateTime(2026, 9, 4, 20, 0, 0, DateTimeKind.Utc);
        var lastTick = new DateTime(2026, 9, 4, 20, 59, 59, DateTimeKind.Utc);
        var closed = new DateTime(2026, 9, 4, 21, 0, 0, DateTimeKind.Utc);

        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(schedule, WeekStart, open)).IsTrue();
        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(schedule, WeekStart, lastTick)).IsTrue();
        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(schedule, WeekStart, closed)).IsFalse();
        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(schedule, WeekStart, open.AddHours(-1))).IsFalse();
        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(null, WeekStart, open)).IsFalse();
        await Assert.That(SiegeScheduleRules.IsDeclareWindowOpen(schedule, null, open)).IsFalse();
    }

    [Test]
    public async Task GetScheduledPeriod_FollowsVolunteerReadySiegeThenPeace()
    {
        var schedule = ShippedSiegeZone();

        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(
            schedule, WeekStart, new DateTime(2026, 9, 4, 11, 59, 0, DateTimeKind.Utc)))
            .IsEqualTo(SiegePeriod.Peace);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(
            schedule, WeekStart, new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc)))
            .IsEqualTo(SiegePeriod.HeroVolunteer);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(
            schedule, WeekStart, new DateTime(2026, 9, 4, 20, 0, 0, DateTimeKind.Utc)))
            .IsEqualTo(SiegePeriod.ReadyToSiege);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(
            schedule, WeekStart, new DateTime(2026, 9, 4, 21, 0, 0, DateTimeKind.Utc)))
            .IsEqualTo(SiegePeriod.Siege);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(
            schedule, WeekStart, new DateTime(2026, 9, 4, 22, 0, 0, DateTimeKind.Utc)))
            .IsEqualTo(SiegePeriod.Peace);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(null, WeekStart, WeekStart))
            .IsEqualTo(SiegePeriod.Peace);
        await Assert.That(SiegeScheduleRules.GetScheduledPeriod(schedule, null, WeekStart))
            .IsEqualTo(SiegePeriod.Peace);
    }
}
