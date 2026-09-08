namespace AAEmu.Game.Models.Game.ScheduleItems;

public sealed class ScheduleItemDef
{
    public int Id { get; init; }
    public int Kind { get; init; }
    public int KindValue { get; init; }
    public uint ItemId { get; init; }
    public int ItemCount { get; init; }
    public int GiveTerm { get; init; }
    public int GiveMax { get; init; }
    public bool ActiveTake { get; init; }
    public bool OnAir { get; init; }
    public int StYear { get; init; }
    public int StMonth { get; init; }
    public int StDay { get; init; }
    public int StHour { get; init; }
    public int StMin { get; init; }
    public int EdYear { get; init; }
    public int EdMonth { get; init; }
    public int EdDay { get; init; }
    public int EdHour { get; init; }
    public int EdMin { get; init; }
    public string MailTitle { get; init; } = "";
    public string MailBody { get; init; } = "";

    public DateTime? Start =>
        ScheduleItemRules.MakeLocalStamp(StYear, StMonth, StDay, StHour, StMin);

    public DateTime? End =>
        ScheduleItemRules.MakeLocalStamp(EdYear, EdMonth, EdDay, EdHour, EdMin);

    public bool IsOnAir(DateTime utcNow) => ScheduleItemRules.IsOnAir(utcNow, Start, End);
}
