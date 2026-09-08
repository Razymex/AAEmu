using AAEmu.Game.Models;

namespace AAEmu.Game.Models.Game.AccountAttendance;

/// <summary>
/// Monthly login streak. The Event Center tab lists <c>day_count</c> 1, 2, 3… from
/// <c>account_attendance_rewards</c>. One claim per calendar day; the client paints a
/// prefix of filled slots and gates the button on today's calendar slot.
/// </summary>
public static class AccountAttendanceRules
{
    public const int DaysInPacket = 31;
    public const int SlotBytes = 16;

    public static int BodyBytes => DaysInPacket * SlotBytes;

    public static DateTime CalendarDay(DateTime utc)
    {
        var day = ServerCalendar.AsUtc(utc);
        return day.Date;
    }

    public static int DayOfMonth(DateTime utc) => CalendarDay(utc).Day;

    public static bool IsValidDay(int day) => day is >= 1 and <= DaysInPacket;

    public static bool CanClaim(bool alreadyClaimedToday) => !alreadyClaimedToday;

    /// <summary>
    /// The Event Center list is a login streak this month, not the calendar date.
    /// The first claim pays <c>day_count</c> 1, the second pays 2, and so on.
    /// </summary>
    public static int NextDayCount(int claimedThisMonth) =>
        claimedThisMonth < 0 ? 1 : claimedThisMonth + 1;

    public static bool ShouldGrantAdditional(int archelifeDaysAfterClaim, int additionalDayCount) =>
        additionalDayCount > 0 && archelifeDaysAfterClaim == additionalDayCount;

    public static long UnixNoonUtc(int year, int month, int day) =>
        new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
}
