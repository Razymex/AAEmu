namespace AAEmu.Game.Models.StaticValues;

/// <summary>enum_siege_periods from game_decrypted.sqlite3 — a zone group's siege-cycle phase.</summary>
public enum SiegePeriod : byte
{
    /// <summary>No live Dominion claim exists for the zone group at all (DominionManager holds no record for it).</summary>
    NoDominion = 0,
    HeroVolunteer = 1,
    ReadyToSiege = 2,
    Siege = 3,
    Peace = 4
}
