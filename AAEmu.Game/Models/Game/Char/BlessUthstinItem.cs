namespace AAEmu.Game.Models.Game.Char;

/// <summary>One <c>item_bless_uthstins</c> row. Function 1 is a normal apply, 2 is special.</summary>
public sealed class BlessUthstinItem
{
    public const int FunctionNormal = 1;
    public const int FunctionSpecial = 2;

    public uint ItemId { get; init; }
    public int FunctionId { get; init; }
    public int RiseCount { get; init; }
    public int DropCount { get; init; }
    public int[] RiseWeights { get; init; } = new int[BlessUthstinRules.StatCount];
    public int[] DropWeights { get; init; } = new int[BlessUthstinRules.StatCount];

    public bool IsSpecial => FunctionId == FunctionSpecial;
}
