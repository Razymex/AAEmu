using System.Collections.Concurrent;
using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

/// <summary><c>item_bless_uthstins</c> — weighted rise/drop tables for Bless Uthstin apply items.</summary>
[GameData]
public class BlessUthstinGameData : Singleton<BlessUthstinGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<uint, BlessUthstinItem> _byItemId = [];

    public void Load(SqliteConnection connection)
    {
        _byItemId.Clear();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT item_id, function_id, rise_count, drop_count,
                   rise_weight_str, rise_weight_dex, rise_weight_sta, rise_weight_int, rise_weight_spi,
                   drop_weight_str, drop_weight_dex, drop_weight_sta, drop_weight_int, drop_weight_spi
            FROM item_bless_uthstins
            """;
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var item = new BlessUthstinItem
            {
                ItemId = reader.GetUInt32("item_id"),
                FunctionId = reader.GetInt32("function_id"),
                RiseCount = reader.GetInt32("rise_count"),
                DropCount = reader.GetInt32("drop_count"),
                RiseWeights =
                [
                    reader.GetInt32("rise_weight_str"),
                    reader.GetInt32("rise_weight_dex"),
                    reader.GetInt32("rise_weight_sta"),
                    reader.GetInt32("rise_weight_int"),
                    reader.GetInt32("rise_weight_spi")
                ],
                DropWeights =
                [
                    reader.GetInt32("drop_weight_str"),
                    reader.GetInt32("drop_weight_dex"),
                    reader.GetInt32("drop_weight_sta"),
                    reader.GetInt32("drop_weight_int"),
                    reader.GetInt32("drop_weight_spi")
                ]
            };
            _byItemId[item.ItemId] = item;
        }

        Logger.Info("Loaded {0} bless uthstin items", _byItemId.Count);
    }

    public void PostLoad()
    {
    }

    public bool TryGet(uint itemId, out BlessUthstinItem item) => _byItemId.TryGetValue(itemId, out item);

    /// <summary>For tests: seeds a row without a database.</summary>
    public void SetForTest(BlessUthstinItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _byItemId[item.ItemId] = item;
    }
}
