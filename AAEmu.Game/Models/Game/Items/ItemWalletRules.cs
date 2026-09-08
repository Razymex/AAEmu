namespace AAEmu.Game.Models.Game.Items;

/// <summary>
/// Wallet currencies that must not sit in the bag as usable items.
/// </summary>
public static class ItemWalletRules
{
    /// <summary>
    /// Lulu's stamp (28586) is the BM mileage item. Right-click is refused because
    /// <c>use_skill_id</c> is 0 — the shop and HUD read <c>GetBmPoint</c>, not the stack.
    /// </summary>
    public static bool IsBmMileage(uint templateId) => templateId == Item.BmMileage;

    /// <summary>
    /// Convert on the way into a player bag or warehouse. Mail and auction keep the
    /// stack until it is taken.
    /// </summary>
    public static bool CreditOnAcquire(uint templateId, SlotType container) =>
        IsBmMileage(templateId) && container is SlotType.Inventory or SlotType.Bank;

    public static int LoyaltyFromCount(int count) => count > 0 ? count : 0;

    /// <summary>
    /// <c>GiveCashPoint</c> value1 is the credit amount on one coupon. Packs must not sit
    /// in the bag after an attendance grant — the client list is the coupon icon.
    /// </summary>
    public static int CreditsFromEffect(int value1, int count) =>
        value1 > 0 && count > 0 ? value1 * count : 0;
}
