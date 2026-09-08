namespace AAEmu.Game.Models.Game.Items;

/// <summary>
/// Wallet currencies that must not sit in the bag as usable items.
/// </summary>
public static class ItemWalletRules
{
    [ThreadStatic]
    private static int t_suppressAcquireConvert;

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

    /// <summary>
    /// Failed-save restore must put the stamp back in the bag. Conversion here would
    /// credit loyalty again while the original row is still in the database.
    /// </summary>
    public static bool ShouldCreditOnAcquire(uint templateId, SlotType container, bool convertWallet) =>
        convertWallet && t_suppressAcquireConvert == 0 && CreditOnAcquire(templateId, container);

    /// <summary>
    /// Blocks convert-on-acquire, including <c>OnAcquiredItem</c>, for the restore path.
    /// </summary>
    public static IDisposable SuppressAcquireConvert()
    {
        t_suppressAcquireConvert++;
        return new SuppressScope();
    }

    private sealed class SuppressScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (t_suppressAcquireConvert > 0)
                t_suppressAcquireConvert--;
        }
    }

    public static int LoyaltyFromCount(int count) => count > 0 ? count : 0;

    /// <summary>
    /// <c>GiveCashPoint</c> value1 is the credit amount on one coupon. Packs must not sit
    /// in the bag after an attendance grant — the client list is the coupon icon.
    /// </summary>
    public static int CreditsFromEffect(int value1, int count) =>
        value1 > 0 && count > 0 ? value1 * count : 0;

    /// <summary>
    /// Loyalty or credits already written when only some of the bag stacks could be removed.
    /// </summary>
    public static int RefundAfterPartialConsume(int credited, int consumed)
    {
        if (credited <= 0)
            return 0;
        var kept = Math.Max(0, consumed);
        return kept >= credited ? 0 : credited - kept;
    }

    /// <summary>
    /// Consume-first convert: put the removed count back when the wallet write misses.
    /// </summary>
    public static int RestoreAfterFailedCredit(int consumed, bool creditOk) =>
        !creditOk && consumed > 0 ? consumed : 0;
}
