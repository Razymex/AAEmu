namespace AAEmu.Game.Models.Game;

/// <summary>
/// A World snapshot is all-or-nothing. One character persist failure rolls the
/// transaction back so money cannot commit without the purchase it paid for.
/// </summary>
public static class WorldSaveCommitRules
{
    public static bool MustRollback(bool characterSaveFailed) => characterSaveFailed;

    public static bool CanCommit(int totalCommits, bool characterSaveFailed) =>
        !characterSaveFailed && totalCommits > 0;
}
