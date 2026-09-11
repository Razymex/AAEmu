using System.Collections.Concurrent;

using AAEmu.Game;
using AAEmu.Game.Core.Managers.Id;

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

    public uint Register(byte[] rawBody) => Register(rawBody, NextPoolId, IsOwnedByGame);

    /// <summary>
    /// Allocate the next bcId that Game does not already own. Exhaustion drops the unit
    /// instead of reusing an occupied id: <c>MirrorZoneNpcSpawn</c> rejects a bcId another
    /// unit still owns, while the zone keeps reusing that id, so the unit would never reach
    /// Game or its clients.
    /// </summary>
    internal uint Register(byte[] rawBody, Func<uint> allocateId, Func<uint, bool> isOwnedByGame)
    {
        for (var i = 0; i < MaxLiveIdSkips; i++)
        {
            var bcId = allocateId();
            if (!isOwnedByGame(bcId))
            {
                _units[bcId] = rawBody;
                return bcId;
            }

            Logger.Warn("UnitRegistry skipped live bc={0} (still owned in Game)", bcId);
        }

        Logger.Error(
            "UnitRegistry exhausted {0} live-id skips, unit not registered (every candidate is still owned in Game, bodyLen={1})",
            MaxLiveIdSkips,
            rawBody?.Length ?? 0);
        return 0;
    }

    private static uint NextPoolId() => ObjectIdManager.Instance.GetNextId();

    private static bool IsOwnedByGame(uint bcId) => WorldIntegration.FindUnitAcrossWorlds(bcId) != null;

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
