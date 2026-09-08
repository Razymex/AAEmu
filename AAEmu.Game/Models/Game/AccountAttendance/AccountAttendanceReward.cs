namespace AAEmu.Game.Models.Game.AccountAttendance;

/// <summary>
/// One row from <c>account_attendance_rewards</c>. Daily gifts have
/// <see cref="AdditionalReward"/> false; milestone gifts sit on the same day count
/// with that flag set.
/// </summary>
public sealed class AccountAttendanceReward
{
    public uint Id { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public int DayCount { get; init; }
    public uint ItemId { get; init; }
    public int ItemGradeId { get; init; }
    public int ItemCount { get; init; }
    public bool AdditionalReward { get; init; }
}
