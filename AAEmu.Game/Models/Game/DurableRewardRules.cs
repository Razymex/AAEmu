namespace AAEmu.Game.Models.Game;

/// <summary>
/// Persist-first claims stay claimed when anything was delivered, or when the
/// compensating delete/write did not land. Only a clean rollback of a zero-delivery
/// grant makes the row retryable.
/// </summary>
public static class DurableRewardRules
{
    public static bool KeepClaim(bool grantSucceeded, bool deliveredAny, bool rollbackSucceeded)
    {
        if (grantSucceeded || deliveredAny)
            return true;
        return !rollbackSucceeded;
    }
}
