using System.Collections.Concurrent;
using AAEmu.Game.Core.Managers;
using MySql.Data.MySqlClient;

namespace AAEmu.Game.Models.Game;

/// <summary>
/// Wallet credits and loyalty queued for the same World/character transaction as
/// the bag removal that produced them.
/// </summary>
public static class AccountLiveWallet
{
    private static readonly ConcurrentDictionary<uint, int> PendingCredits = [];
    private static readonly ConcurrentDictionary<uint, int> PendingLoyalty = [];
    private static readonly ConcurrentDictionary<uint, int> Dirty = [];
    private static int _stamp;

    [ThreadStatic] private static List<(uint AccountId, int Credits, int Loyalty, int Stamp)> t_written;

    public static void QueueCredits(uint accountId, int amount)
    {
        if (amount == 0)
            return;
        PendingCredits.AddOrUpdate(accountId, amount, (_, current) => current + amount);
        MarkDirty(accountId);
    }

    public static void QueueLoyalty(uint accountId, int amount)
    {
        if (amount == 0)
            return;
        PendingLoyalty.AddOrUpdate(accountId, amount, (_, current) => current + amount);
        MarkDirty(accountId);
    }

    public static void UnqueueCredits(uint accountId, int amount) =>
        SubtractPending(PendingCredits, accountId, amount);

    public static void UnqueueLoyalty(uint accountId, int amount) =>
        SubtractPending(PendingLoyalty, accountId, amount);

    public static int PeekCredits(uint accountId) =>
        PendingCredits.TryGetValue(accountId, out var credits) ? credits : 0;

    public static int PeekLoyalty(uint accountId) =>
        PendingLoyalty.TryGetValue(accountId, out var loyalty) ? loyalty : 0;

    public static void SaveForAccount(uint accountId, MySqlConnection connection, MySqlTransaction transaction)
    {
        PendingCredits.TryGetValue(accountId, out var credits);
        PendingLoyalty.TryGetValue(accountId, out var loyalty);
        if (credits == 0 && loyalty == 0)
            return;

        if (!Dirty.TryGetValue(accountId, out var stamp))
            return;

        if (credits != 0 && !AccountManager.Instance.AddCreditsOn(accountId, credits, connection, transaction))
            throw new InvalidOperationException("Account wallet credits were not written with the item removal");
        if (loyalty != 0 && !AccountManager.Instance.AddLoyaltyOn(accountId, loyalty, connection, transaction))
            throw new InvalidOperationException("Account wallet loyalty was not written with the item removal");

        t_written ??= [];
        t_written.Add((accountId, credits, loyalty, stamp));
    }

    public static void ConfirmSaved()
    {
        if (t_written == null)
            return;

        foreach (var (accountId, credits, loyalty, stamp) in t_written)
        {
            AccountLiveDirty.ClearIfUnchanged(Dirty, accountId, stamp);
            SubtractPending(PendingCredits, accountId, credits);
            SubtractPending(PendingLoyalty, accountId, loyalty);
        }

        t_written = null;
    }

    public static void DiscardPendingClears() => t_written = null;

    private static void MarkDirty(uint accountId) =>
        Dirty[accountId] = Interlocked.Increment(ref _stamp);

    private static void SubtractPending(ConcurrentDictionary<uint, int> pending, uint accountId, int amount)
    {
        if (amount == 0)
            return;
        pending.AddOrUpdate(accountId, 0, (_, current) => Math.Max(0, current - amount));
        if (pending.TryGetValue(accountId, out var left) && left <= 0)
            pending.TryRemove(accountId, out _);
    }
}
