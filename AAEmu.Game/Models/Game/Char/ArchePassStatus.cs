namespace AAEmu.Game.Models.Game.Char;

/// <summary>Values from compact <c>enum_arche_pass_statuses</c>. Absent from the list is <see cref="Invalid"/>.</summary>
public enum ArchePassStatus : byte
{
    Invalid = 0,
    Owned = 1,
    Progress = 2,
    Expired = 3,
    Dropped = 4,
    Completed = 5
}
