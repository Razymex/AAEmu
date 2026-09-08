using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.AccountAttendance;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

[GameData]
public class AccountAttendanceGameData : Singleton<AccountAttendanceGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly List<AccountAttendanceReward> _rewards = [];
    private (int Year, int Month) _latestMonth;

    public void Load(SqliteConnection connection)
    {
        _rewards.Clear();
        _latestMonth = default;

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, year, month, day_count, item_id, item_grade_id, item_count, additional_reward
            FROM account_attendance_rewards
            """;
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var reward = new AccountAttendanceReward
            {
                Id = reader.GetUInt32("id"),
                Year = reader.GetInt32("year"),
                Month = reader.GetInt32("month"),
                DayCount = reader.GetInt32("day_count"),
                ItemId = reader.GetUInt32("item_id"),
                ItemGradeId = reader.GetInt32("item_grade_id"),
                ItemCount = reader.GetInt32("item_count"),
                AdditionalReward = reader.GetBoolean("additional_reward")
            };
            _rewards.Add(reward);
            if (reward.Year > _latestMonth.Year ||
                (reward.Year == _latestMonth.Year && reward.Month > _latestMonth.Month))
                _latestMonth = (reward.Year, reward.Month);
        }

        Logger.Info(
            "Loaded {0} account attendance rewards (latest {1}-{2:00})",
            _rewards.Count,
            _latestMonth.Year,
            _latestMonth.Month);
    }

    public void PostLoad()
    {
    }

    /// <summary>
    /// Rewards for a calendar month. When that month has no rows, the latest shipped
    /// month is reused so a claim can still pay out after the published calendar ends.
    /// The Event Center tab still needs matching year/month rows in the client DB.
    /// </summary>
    public IReadOnlyList<AccountAttendanceReward> GetRewards(int year, int month)
    {
        var exact = _rewards.Where(x => x.Year == year && x.Month == month).ToList();
        if (exact.Count > 0)
            return exact;
        if (_latestMonth.Year == 0)
            return [];
        return _rewards
            .Where(x => x.Year == _latestMonth.Year && x.Month == _latestMonth.Month)
            .Select(x => new AccountAttendanceReward
            {
                Id = x.Id,
                Year = year,
                Month = month,
                DayCount = x.DayCount,
                ItemId = x.ItemId,
                ItemGradeId = x.ItemGradeId,
                ItemCount = x.ItemCount,
                AdditionalReward = x.AdditionalReward
            })
            .ToList();
    }

    public bool IsRewardItem(uint itemId) =>
        itemId != 0 && _rewards.Exists(x => x.ItemId == itemId);

    public AccountAttendanceReward DailyReward(int year, int month, int day) =>
        GetRewards(year, month).FirstOrDefault(x => x.DayCount == day && !x.AdditionalReward);

    public AccountAttendanceReward AdditionalReward(int year, int month, int dayCount) =>
        GetRewards(year, month).FirstOrDefault(x => x.DayCount == dayCount && x.AdditionalReward);
}
