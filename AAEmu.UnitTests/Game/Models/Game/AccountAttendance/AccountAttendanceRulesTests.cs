using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.AccountAttendance;

namespace AAEmu.UnitTests.Game.Models.Game.AccountAttendance;

public class AccountAttendanceRulesTests
{
    [Test]
    public async Task PacketBody_IsThirtyOneSixteenByteSlots()
    {
        await Assert.That(AccountAttendanceRules.DaysInPacket).IsEqualTo(31);
        await Assert.That(AccountAttendanceRules.SlotBytes).IsEqualTo(16);
        await Assert.That(AccountAttendanceRules.BodyBytes).IsEqualTo(496);
    }

    [Test]
    public async Task CanClaim_OnlyWhenThatDayIsEmpty()
    {
        await Assert.That(AccountAttendanceRules.CanClaim(alreadyClaimedToday: false)).IsTrue();
        await Assert.That(AccountAttendanceRules.CanClaim(alreadyClaimedToday: true)).IsFalse();
    }

    [Test]
    public async Task NextDayCount_IsTheStreakPlusOne()
    {
        await Assert.That(AccountAttendanceRules.NextDayCount(0)).IsEqualTo(1);
        await Assert.That(AccountAttendanceRules.NextDayCount(6)).IsEqualTo(7);
        await Assert.That(AccountAttendanceRules.NextDayCount(-1)).IsEqualTo(1);
    }

    [Test]
    public async Task ArcheLife_IsTheAncientMembership()
    {
        await Assert.That(AccountPatronRules.IsArcheLife([(uint)AccountMembership.Ancient])).IsTrue();
        await Assert.That(AccountPatronRules.IsArcheLife([(uint)AccountMembership.Advanced])).IsFalse();
    }

    [Test]
    public async Task ShouldGrantAdditional_OnlyOnTheMatchingArchelifeCount()
    {
        await Assert.That(AccountAttendanceRules.ShouldGrantAdditional(7, 7)).IsTrue();
        await Assert.That(AccountAttendanceRules.ShouldGrantAdditional(6, 7)).IsFalse();
        await Assert.That(AccountAttendanceRules.ShouldGrantAdditional(8, 7)).IsFalse();
        await Assert.That(AccountAttendanceRules.ShouldGrantAdditional(7, 0)).IsFalse();
    }

    [Test]
    public async Task DayOfMonth_UsesTheUtcCalendarDay()
    {
        var utc = new DateTime(2026, 9, 6, 3, 15, 0, DateTimeKind.Utc);

        await Assert.That(AccountAttendanceRules.DayOfMonth(utc)).IsEqualTo(6);
        await Assert.That(AccountAttendanceRules.IsValidDay(6)).IsTrue();
        await Assert.That(AccountAttendanceRules.IsValidDay(0)).IsFalse();
        await Assert.That(AccountAttendanceRules.IsValidDay(32)).IsFalse();
    }

    [Test]
    public async Task UnixNoonUtc_FallsOnThatCalendarDay()
    {
        var unix = AccountAttendanceRules.UnixNoonUtc(2026, 9, 6);
        var dt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

        await Assert.That(dt.Year).IsEqualTo(2026);
        await Assert.That(dt.Month).IsEqualTo(9);
        await Assert.That(dt.Day).IsEqualTo(6);
    }
}
