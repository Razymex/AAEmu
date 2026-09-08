using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Live Patron policy: stacked 1001+1002 by default, plus the paid labor grade those
/// memberships sit on. Purchase is not part of this path.
/// </summary>
public static class AccountPatron
{
    public static bool GrantStacked =>
        AppConfiguration.Instance.Account?.GrantStackedPatron != false
        || AppConfiguration.Instance.Account?.ForceMaxPremiumGrade == true;

    public static bool ForceMaxGrade =>
        AppConfiguration.Instance.Account?.ForceMaxPremiumGrade == true;

    public static bool IsEnabled => AccountPatronRules.IsEnabled(GrantStacked, ForceMaxGrade);

    public static int ResolvePoint(int point) =>
        AccountPatronRules.ResolvePoint(
            point,
            PremiumGameData.Instance.FirstPaidGradePoint,
            PremiumGameData.Instance.GetGrade(PremiumGameData.Instance.MaxGradeId)?.Point ?? 0,
            GrantStacked,
            ForceMaxGrade);

    public static uint ResolveGrade(uint gradeFromPoints) =>
        AccountPatronRules.ResolveGrade(
            gradeFromPoints,
            PremiumGameData.Instance.FirstPaidGradeId,
            PremiumGameData.Instance.MaxGradeId,
            GrantStacked,
            ForceMaxGrade);

    public static uint PaidFloorGradeId => PremiumGameData.Instance.FirstPaidGradeId;

    public static bool IsPaid(Character character) =>
        character != null &&
        AccountPatronRules.IsPaidPatron(
            character.PremiumGrade,
            AccountMemberships.ActiveIds(character.AccountId, AppConfiguration.Instance.Id),
            PaidFloorGradeId);

    public static IReadOnlyList<uint> WantedBuffIds(uint accountId, uint premiumGrade)
    {
        var gradeBuff = PremiumGameData.Instance.GetGrade(premiumGrade)?.BuffId ?? 0;
        var membershipBuffs = AccountBuffsGameData.Instance.BuffIdsFor(
            AccountMemberships.ActiveIds(accountId, AppConfiguration.Instance.Id));
        return AccountPatronRules.WantedBuffIds(
            gradeBuff,
            membershipBuffs,
            PremiumGameData.Instance.GradeBuffIds);
    }

    public static IReadOnlyList<uint> KnownBuffIds() =>
        AccountPatronRules.KnownPatronBuffIds(
            PremiumGameData.Instance.GradeBuffIds,
            AccountBuffsGameData.Instance.AllCharacterBuffIds);

    public static bool HasAuctionFeeDiscount(uint accountId) =>
        AccountPatronRules.HasAuctionFeeDiscount(
            AccountMemberships.ActiveIds(accountId, AppConfiguration.Instance.Id),
            AccountBuffsGameData.Instance.UsesAuctionConfig);
}
