using AAEmu.Game.Models.Game.ScheduleItems;

namespace AAEmu.UnitTests.Game.Models.Game.ScheduleItems;

public class ScheduleItemRulesTests
{
    [Test]
    public async Task RowAndPacketSizes_MatchTheFixedLayout()
    {
        await Assert.That(ScheduleItemRules.RowBytes).IsEqualTo(21);
        await Assert.That(ScheduleItemRules.BodyBytes(0)).IsEqualTo(1);
        await Assert.That(ScheduleItemRules.BodyBytes(1)).IsEqualTo(22);
        await Assert.That(ScheduleItemRules.BodyBytes(32)).IsEqualTo(1 + 32 * 21);
        await Assert.That(ScheduleItemRules.BodyBytes(40)).IsEqualTo(1 + 32 * 21);
    }

    [Test]
    public async Task ZeroDates_AreAlwaysOnAir()
    {
        var now = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc);
        await Assert.That(ScheduleItemRules.IsOnAir(now, null, null)).IsTrue();
    }

    [Test]
    public async Task DatedWindow_IncludesTheCurrentInstant()
    {
        var start = new DateTime(2021, 2, 11, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc);

        await Assert.That(ScheduleItemRules.IsOnAir(now, start, end)).IsTrue();
        await Assert.That(ScheduleItemRules.IsOnAir(start, start, end)).IsFalse();
        await Assert.That(ScheduleItemRules.IsOnAir(end, start, end)).IsFalse();
    }

    [Test]
    public async Task CanTake_WaitsForTheTermThenStopsAtGiveMax()
    {
        await Assert.That(ScheduleItemRules.CanTake(0, 2, 0, 10)).IsFalse();
        await Assert.That(ScheduleItemRules.CanTake(0, 2, ScheduleItemRules.SecondsForTerm(10), 10)).IsTrue();
        await Assert.That(ScheduleItemRules.CanTake(2, 2, ScheduleItemRules.SecondsForTerm(10), 10)).IsFalse();
        await Assert.That(ScheduleItemRules.CanTake(0, 1, 0, 0)).IsTrue();
    }

    [Test]
    public async Task DailyReset_TripsOnANewUtcDate()
    {
        var monday = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
        var tuesday = new DateTime(2026, 9, 7, 0, 1, 0, DateTimeKind.Utc);

        await Assert.That(ScheduleItemRules.NeedsDailyReset(monday, monday.AddHours(1))).IsFalse();
        await Assert.That(ScheduleItemRules.NeedsDailyReset(monday, tuesday)).IsTrue();
    }

    [Test]
    public async Task ShouldAutoGrant_OnlySilentAccountBuffRows()
    {
        await Assert.That(ScheduleItemRules.ShouldAutoGrant(
            (int)ScheduleItemKind.AccountBuff, activeTake: false, 0, 0, 1)).IsTrue();
        await Assert.That(ScheduleItemRules.ShouldAutoGrant(
            (int)ScheduleItemKind.AccountBuff, activeTake: false, 0, 1, 1)).IsFalse();
        await Assert.That(ScheduleItemRules.ShouldAutoGrant(
            (int)ScheduleItemKind.AccountBuff, activeTake: true, 0, 0, 1)).IsFalse();
        await Assert.That(ScheduleItemRules.ShouldAutoGrant(
            (int)ScheduleItemKind.Premium, activeTake: false, 0, 0, 1)).IsFalse();
        await Assert.That(ScheduleItemRules.ShouldAutoGrant(
            (int)ScheduleItemKind.Every, activeTake: false, 0, 0, 1)).IsFalse();
    }

    [Test]
    public async Task TickCumulated_CapsAtTheGiveTerm()
    {
        var term = ScheduleItemRules.SecondsForTerm(10);
        await Assert.That(ScheduleItemRules.TickCumulated(0, 10, 60)).IsEqualTo(60);
        await Assert.That(ScheduleItemRules.TickCumulated(term - 10, 10, 60)).IsEqualTo(term);
        await Assert.That(ScheduleItemRules.TickCumulated(10, 0, 60)).IsEqualTo(0);
    }

    [Test]
    public async Task SessionAddSeconds_IgnoresLogoutGaps()
    {
        var start = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        var later = start.AddMinutes(31);

        await Assert.That(ScheduleItemRules.HasSessionTick(default)).IsFalse();
        await Assert.That(ScheduleItemRules.SessionAddSeconds(default, later)).IsEqualTo(0);
        await Assert.That(ScheduleItemRules.TickCumulated(0, 30, 0)).IsEqualTo(0);

        await Assert.That(ScheduleItemRules.HasSessionTick(start)).IsTrue();
        await Assert.That(ScheduleItemRules.SessionAddSeconds(start, later)).IsEqualTo(31 * 60);
        await Assert.That(ScheduleItemRules.TickCumulated(0, 30, 31 * 60))
            .IsEqualTo(ScheduleItemRules.SecondsForTerm(30));
    }
}
