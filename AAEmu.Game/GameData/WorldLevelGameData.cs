using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

[GameData]
public class WorldLevelGameData : Singleton<WorldLevelGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly List<WorldLevelHardCapRow> _hardCaps = [];
    private readonly List<WorldLevelExpModifierRow> _modifiers = [];

    public void Load(SqliteConnection connection)
    {
        _hardCaps.Clear();
        _modifiers.Clear();

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT min_server_days, max_server_days, hard_cap_level, get_quest
                FROM world_level_hard_caps
                ORDER BY min_server_days
                """;
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                _hardCaps.Add(new WorldLevelHardCapRow(
                    reader.GetInt32("min_server_days"),
                    reader.GetInt32("max_server_days"),
                    reader.GetInt32("hard_cap_level"),
                    reader.GetBoolean("get_quest")));
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT level_diff_min, level_diff_max, exp_modifier
                FROM world_level_exp_modifiers
                ORDER BY level_diff_min
                """;
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                _modifiers.Add(new WorldLevelExpModifierRow(
                    reader.GetInt32("level_diff_min"),
                    reader.GetInt32("level_diff_max"),
                    (uint)reader.GetDouble("exp_modifier")));
            }
        }

        if (_hardCaps.Count == 0)
            throw new InvalidOperationException("world_level_hard_caps is empty.");
        if (_modifiers.Count == 0)
            throw new InvalidOperationException("world_level_exp_modifiers is empty.");

        Logger.Info("Loaded {0} world-level hard caps and {1} exp modifiers", _hardCaps.Count, _modifiers.Count);
    }

    public void PostLoad()
    {
    }

    public WorldLevelInfoWire CreateFor(int characterLevel, int playerLevelCap)
    {
        return WorldLevelInfoRules.ForCharacter(characterLevel, playerLevelCap, _hardCaps, _modifiers);
    }

    public long ServerOpenUnixTime(int playerLevelCap, long nowUnixSeconds)
    {
        var unlock = WorldLevelInfoRules.SelectUnlockRow(_hardCaps, playerLevelCap);
        return WorldLevelInfoRules.ServerOpenUnixTime(nowUnixSeconds, unlock.MinServerDays);
    }

    public long ServerOpenUnixTime()
    {
        return ServerOpenUnixTime(AppConfiguration.Instance.World.PlayerLevelCap, Helpers.UnixTimeNow());
    }

    public void SetForTest(
        IReadOnlyList<WorldLevelHardCapRow> hardCaps,
        IReadOnlyList<WorldLevelExpModifierRow> modifiers)
    {
        _hardCaps.Clear();
        _hardCaps.AddRange(hardCaps);
        _modifiers.Clear();
        _modifiers.AddRange(modifiers);
    }
}
