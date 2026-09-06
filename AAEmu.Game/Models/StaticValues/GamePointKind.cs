namespace AAEmu.Game.Models.StaticValues;

public enum GamePointKind : byte
{
    Honor = 0,
    Vocation = 1,

    /// <summary>
    /// Moves Character.LeadershipPoint (current period) and, on a gain, the lifetime and daily figures.
    /// The previous period's frozen figure is only written by the election roll.
    /// </summary>
    Leadership = 2
}
