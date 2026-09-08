namespace AAEmu.Game.Models.Game;

/// <summary>
/// Thread-safe dirty flag plus a stamp. Capture the stamp before reading persist
/// values. Clear only after commit, and only if the stamp is still that value.
/// Mark and clear share one lock so a change cannot land between the check and
/// the clear.
/// </summary>
public sealed class LiveDirtyGate
{
    private readonly object _sync = new();
    private bool _dirty;
    private int _stamp;

    public int Stamp
    {
        get
        {
            lock (_sync)
                return _stamp;
        }
    }

    public bool IsDirty
    {
        get
        {
            lock (_sync)
                return _dirty;
        }
        set
        {
            if (value)
                Mark();
            else
            {
                lock (_sync)
                    _dirty = false;
            }
        }
    }

    public void Mark()
    {
        lock (_sync)
        {
            _dirty = true;
            unchecked
            {
                _stamp++;
            }
        }
    }

    public bool TryCapture(out int stamp)
    {
        lock (_sync)
        {
            if (!_dirty)
            {
                stamp = 0;
                return false;
            }

            stamp = _stamp;
            return true;
        }
    }

    public bool TryClear(int writtenStamp)
    {
        lock (_sync)
        {
            if (!AccountLiveDirty.ShouldClear(writtenStamp, _stamp))
                return false;
            _dirty = false;
            return true;
        }
    }
}
