using System.Collections.Concurrent;

using AAEmu.Game;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Models.Game.NPChar;

using NLog;

namespace AAEmu.World.Core.Zone;

/// <summary>
/// Per-zone unit body store. BcIds come from the process-wide
/// <see cref="ObjectIdManager"/> (same pool as characters) so multi-zone
/// mirrors stay unique and stay under dedicate <c>max_unit</c>.
/// </summary>
public class UnitRegistry
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const int MaxLiveIdSkips = 32;
    private readonly ConcurrentDictionary<uint, byte[]> _units = new();

    public uint Register(byte[] rawBody)
    {
        uint bcId = 0;
        for (var i = 0; i < MaxLiveIdSkips; i++)
        {
            bcId = ObjectIdManager.Instance.GetNextId();
            if (!ZoneMirrorIdRules.ShouldSkipAllocatedId(WorldIntegration.FindUnitAcrossWorlds(bcId) != null))
            {
                _units[bcId] = rawBody;
                return bcId;
            }

            Logger.Warn("UnitRegistry skipped live bc={0} (still owned in Game)", bcId);
        }

        Logger.Error("UnitRegistry exhausted live-id skips, using bc={0}", bcId);
        _units[bcId] = rawBody;
        return bcId;
    }

    public void RegisterWithId(uint bcId, byte[] rawBody)
    {
        _units[bcId] = rawBody;
    }

    public bool TryRemove(uint bcId) => _units.TryRemove(bcId, out _);

    public bool TryGet(uint bcId, out byte[]? rawBody) => _units.TryGetValue(bcId, out rawBody);

    public bool Contains(uint bcId) => _units.ContainsKey(bcId);

    public int Count => _units.Count;

    public KeyValuePair<uint, byte[]>[] Snapshot() => _units.ToArray();

    /// <summary>Drop all tracked bodies (caller removes Game mirrors first).</summary>
    public int Clear()
    {
        var n = _units.Count;
        _units.Clear();
        return n;
    }
}
