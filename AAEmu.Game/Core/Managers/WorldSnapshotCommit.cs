namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Holds <see cref="PersistenceGate"/> across an in-memory mutation, then writes one World
/// snapshot. Mail sent inside the scope is deferred until that snapshot. A
/// <see cref="WorldSaveStatus.Busy"/> reply is a safe skip — the in-flight save already
/// captured the mutation.
/// </summary>
public static class WorldSnapshotCommit
{
    public static bool Accepted(WorldSaveStatus status) =>
        status is WorldSaveStatus.Saved or WorldSaveStatus.Busy;

    /// <summary>
    /// Tests that skip charges also skip the World snapshot (no <see cref="ISaveManager"/>).
    /// </summary>
    public static IDisposable Begin(bool bypassCharges) =>
        bypassCharges ? NopScope.Instance : MailManager.Instance.DeferPersist();

    /// <summary>Marks the enclosing <see cref="Begin"/> scope to flush on dispose.</summary>
    public static void RequestFlush(bool bypassCharges)
    {
        if (bypassCharges)
            return;
        MailManager.Instance.PersistNow();
    }

    /// <summary>
    /// Call after disposing <see cref="Begin"/>. The snapshot runs on that dispose, so this
    /// reads the flush that just finished.
    /// </summary>
    public static bool AcceptedLast(bool bypassCharges, bool failNextPersist)
    {
        if (failNextPersist)
            return false;
        if (bypassCharges)
            return true;
        return Accepted(MailManager.Instance.TakeLastFlushStatus());
    }

    private sealed class NopScope : IDisposable
    {
        public static readonly NopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
