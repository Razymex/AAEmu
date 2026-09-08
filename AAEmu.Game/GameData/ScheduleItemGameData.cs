using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.ScheduleItems;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

[GameData]
public class ScheduleItemGameData : Singleton<ScheduleItemGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly List<ScheduleItemDef> _items = [];

    public void Load(SqliteConnection connection)
    {
        _items.Clear();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, kind_id, kind_value, item_id, item_count, give_term, give_max,
                   active_take, on_air,
                   st_year, st_month, st_day, st_hour, st_min,
                   ed_year, ed_month, ed_day, ed_hour, ed_min,
                   mail_title, mail_body
            FROM schedule_items
            """;
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            _items.Add(new ScheduleItemDef
            {
                Id = reader.GetInt32("id"),
                Kind = reader.GetInt32("kind_id", 0),
                KindValue = reader.GetInt32("kind_value", 0),
                ItemId = reader.GetUInt32("item_id", 0),
                ItemCount = reader.GetInt32("item_count", 0),
                GiveTerm = reader.GetInt32("give_term", 0),
                GiveMax = reader.GetInt32("give_max", 0),
                ActiveTake = reader.GetBoolean("active_take"),
                OnAir = reader.GetBoolean("on_air"),
                StYear = reader.GetInt32("st_year", 0),
                StMonth = reader.GetInt32("st_month", 0),
                StDay = reader.GetInt32("st_day", 0),
                StHour = reader.GetInt32("st_hour", 0),
                StMin = reader.GetInt32("st_min", 0),
                EdYear = reader.GetInt32("ed_year", 0),
                EdMonth = reader.GetInt32("ed_month", 0),
                EdDay = reader.GetInt32("ed_day", 0),
                EdHour = reader.GetInt32("ed_hour", 0),
                EdMin = reader.GetInt32("ed_min", 0),
                MailTitle = reader.GetString("mail_title", ""),
                MailBody = reader.GetString("mail_body", "")
            });
        }

        Logger.Info(
            "Loaded {0} schedule items ({1} on-air active-take)",
            _items.Count,
            _items.Count(x => x.OnAir && x.ActiveTake));
    }

    public void PostLoad()
    {
    }

    public ScheduleItemDef Get(int id) => _items.FirstOrDefault(x => x.Id == id);

    public IReadOnlyList<ScheduleItemDef> ActiveOnAir(DateTime utcNow) =>
        _items.Where(x => x.OnAir && x.ActiveTake && x.ItemId > 0 && x.ItemCount > 0 && x.IsOnAir(utcNow)).ToList();

    /// <summary>On-air rows with no take button. The HUD list stays <see cref="ActiveOnAir"/>.</summary>
    public IReadOnlyList<ScheduleItemDef> SilentOnAir(DateTime utcNow) =>
        _items.Where(x => x.OnAir && !x.ActiveTake && x.ItemId > 0 && x.ItemCount > 0 && x.IsOnAir(utcNow)).ToList();
}
