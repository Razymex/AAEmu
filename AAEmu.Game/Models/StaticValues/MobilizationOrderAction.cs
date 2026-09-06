namespace AAEmu.Game.Models.StaticValues;

/// <summary>Leading byte of the Mobilization Order counter update: a plain counter refresh or a new order.</summary>
public enum MobilizationOrderAction : byte
{
    None = 0,
    Issued = 1
}
