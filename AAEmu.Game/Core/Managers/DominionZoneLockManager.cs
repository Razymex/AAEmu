using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;

using NLog;

namespace AAEmu.Game.Core.Managers;

/// <summary>See IDominionZoneLockManager's doc comment.</summary>
public class DominionZoneLockManager : Singleton<DominionZoneLockManager>, IDominionZoneLockManager
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private HashSet<ushort> _lockedZones = [];

    public IEnumerable<ushort> LockedZones => _lockedZones;

    public bool IsLocked(ushort zoneId) => _lockedZones.Contains(zoneId);

    public void Load()
    {
        _lockedZones = [];

        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT zone_id FROM dominion_locked_zones";
        command.Prepare();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            _lockedZones.Add((ushort)reader.GetInt32(0));

        Logger.Info("Loaded {0} locked castle-system zones", _lockedZones.Count);
    }

    public void Lock(ushort zoneId)
    {
        if (!_lockedZones.Add(zoneId))
            return;

        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT IGNORE INTO dominion_locked_zones (zone_id, locked_at) VALUES (@zoneId, @now)";
        command.Parameters.AddWithValue("@zoneId", zoneId);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        command.Prepare();
        command.ExecuteNonQuery();

        Logger.Info("Castle system locked for zone group {0} - no new claims will be accepted", zoneId);
    }

    public void Unlock(ushort zoneId)
    {
        if (!_lockedZones.Remove(zoneId))
            return;

        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dominion_locked_zones WHERE zone_id = @zoneId";
        command.Parameters.AddWithValue("@zoneId", zoneId);
        command.Prepare();
        command.ExecuteNonQuery();

        Logger.Info("Castle system unlocked for zone group {0}", zoneId);
    }
}
