using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Premium;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Which paid memberships an account currently holds, and what they add to its labor.
/// </summary>
/// <remarks>
/// One place on purpose: the same list has to reach the client (as account attributes, which is the
/// only thing it accepts as proof of membership - see <see cref="AccountMembership"/>) and the labor
/// arithmetic. When the two drifted apart the client showed benefits the server never paid out.
/// </remarks>
public static class AccountMemberships
{
    /// <summary>
    /// Membership ids active for an account: stored account_buff rows, plus the stacked
    /// Patron pair when <see cref="AccountPatron.GrantStacked"/> is on.
    /// </summary>
    public static List<uint> ActiveIds(uint accountId, uint worldId)
    {
        var ids = AccountAttributeManager.Instance
            .Get(accountId, worldId)
            .Where(a => a.KindId == (uint)AccountAttributeKind.AccountBuff)
            .Select(a => a.KindValue)
            .ToList();

        return AccountPatronRules.WithStacked(ids, AccountPatron.GrantStacked).ToList();
    }

    /// <summary>Stacked Patron column (1001 + 1002).</summary>
    public static IReadOnlyList<uint> ForcedIds => AccountPatronRules.StackedMemberships;

    /// <summary>
    /// The grade's labor numbers with the account's memberships applied - the same sum the client
    /// computes for its own display.
    /// </summary>
    public static LaborAllowance LaborFor(uint premiumGradeId, uint accountId, uint worldId)
    {
        var grade = PremiumGameData.Instance.GetGrade(premiumGradeId);
        return AccountBuffsGameData.Instance.Apply(grade, ActiveIds(accountId, worldId));
    }
}
