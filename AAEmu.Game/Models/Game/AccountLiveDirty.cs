using System.Collections.Concurrent;

namespace AAEmu.Game.Models.Game;

/// <summary>
/// Dirty stamps for account live rows written inside a World/character transaction.
/// Clear a key only after commit, and only if nothing marked it again.
/// </summary>
public static class AccountLiveDirty
{
    public static bool ShouldClear(int writtenStamp, int currentStamp) =>
        writtenStamp == currentStamp;

    public static void ClearIfUnchanged<TKey>(
        ConcurrentDictionary<TKey, int> dirty,
        TKey key,
        int writtenStamp)
    {
        if (!dirty.TryGetValue(key, out var current) || !ShouldClear(writtenStamp, current))
            return;

        ((ICollection<KeyValuePair<TKey, int>>)dirty).Remove(new KeyValuePair<TKey, int>(key, writtenStamp));
    }
}
