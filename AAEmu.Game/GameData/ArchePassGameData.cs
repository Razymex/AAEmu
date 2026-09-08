using System.Collections.Concurrent;
using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

/// <summary><c>arche_passes</c> and <c>arche_pass_tiers</c>. Unknown ids are refused.</summary>
[GameData]
public class ArchePassGameData : Singleton<ArchePassGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<uint, ArchePassDesc> _byId = [];
    private readonly ConcurrentDictionary<uint, List<ArchePassTierDesc>> _tiersByPass = [];

    public void Load(SqliteConnection connection)
    {
        _byId.Clear();
        _tiersByPass.Clear();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT id, arche_pass_category_id, name, currency_id, currency_value, upgrade_item_id,
                       max_tier, ed_year, ed_month, ed_day, ed_hour, ed_min
                FROM arche_passes
                """;
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var desc = new ArchePassDesc
                {
                    Id = reader.GetUInt32("id"),
                    CategoryId = reader.GetUInt32("arche_pass_category_id"),
                    Name = reader.GetString("name"),
                    CurrencyId = reader.GetUInt32("currency_id", 0),
                    CurrencyValue = reader.GetInt32("currency_value", 0),
                    UpgradeItemId = reader.GetUInt32("upgrade_item_id", 0),
                    MaxTier = reader.GetInt32("max_tier", 0),
                    EndYear = reader.GetInt32("ed_year"),
                    EndMonth = reader.GetInt32("ed_month"),
                    EndDay = reader.GetInt32("ed_day"),
                    EndHour = reader.GetInt32("ed_hour"),
                    EndMinute = reader.GetInt32("ed_min")
                };
                _byId[desc.Id] = desc;
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT id, arche_pass_id, tier, point, reward_item_id, reward_item_count,
                       premium_reward_item_id, premium_reward_item_count
                FROM arche_pass_tiers
                ORDER BY arche_pass_id, tier
                """;
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var tier = new ArchePassTierDesc
                {
                    Id = reader.GetUInt32("id"),
                    PassId = reader.GetUInt32("arche_pass_id"),
                    Tier = reader.GetUInt32("tier"),
                    Point = reader.GetInt32("point", 0),
                    RewardItemId = reader.GetUInt32("reward_item_id", 0),
                    RewardItemCount = reader.GetInt32("reward_item_count", 0),
                    PremiumRewardItemId = reader.GetUInt32("premium_reward_item_id", 0),
                    PremiumRewardItemCount = reader.GetInt32("premium_reward_item_count", 0)
                };
                var list = _tiersByPass.GetOrAdd(tier.PassId, _ => []);
                list.Add(tier);
            }
        }

        Logger.Info("Loaded {0} arche passes, {1} tiers", _byId.Count, _tiersByPass.Values.Sum(list => list.Count));
    }

    public void PostLoad()
    {
    }

    public bool TryGet(uint passId, out ArchePassDesc desc) => _byId.TryGetValue(passId, out desc);

    public uint MaxTier(uint passId)
    {
        if (_byId.TryGetValue(passId, out var desc) && desc.MaxTier > 0)
            return (uint)desc.MaxTier;
        if (!_tiersByPass.TryGetValue(passId, out var tiers) || tiers.Count == 0)
            return 0;
        return tiers[^1].Tier;
    }

    public IReadOnlyList<ArchePassTierDesc> Tiers(uint passId) =>
        _tiersByPass.TryGetValue(passId, out var tiers) ? tiers : [];

    public bool TryGetTier(uint passId, uint tier, out ArchePassTierDesc desc)
    {
        desc = null;
        if (!_tiersByPass.TryGetValue(passId, out var tiers))
            return false;
        foreach (var row in tiers)
        {
            if (row.Tier != tier)
                continue;
            desc = row;
            return true;
        }

        return false;
    }

    /// <summary>Highest tier whose point threshold is at or below <paramref name="points"/>.</summary>
    public uint CurrentTier(uint passId, long points)
    {
        if (!_tiersByPass.TryGetValue(passId, out var tiers))
            return 0;
        uint current = 0;
        foreach (var tier in tiers)
        {
            if (points < tier.Point)
                break;
            current = tier.Tier;
        }

        return current;
    }

    /// <summary>
    /// First unclaimed tier at or below the current point tier that still has a reward of that track.
    /// </summary>
    public bool TryNextReward(uint passId, long points, uint lastClaimed, bool premium, out ArchePassTierDesc desc)
    {
        desc = null;
        var current = CurrentTier(passId, points);
        if (current == 0)
            return false;
        if (!_tiersByPass.TryGetValue(passId, out var tiers))
            return false;

        foreach (var tier in tiers)
        {
            if (tier.Tier <= lastClaimed || tier.Tier > current)
                continue;
            if (premium)
            {
                if (!tier.HasPremiumReward)
                    continue;
            }
            else if (!tier.HasFreeReward)
            {
                continue;
            }

            desc = tier;
            return true;
        }

        return false;
    }

    /// <summary>For tests: seeds a pass without a database.</summary>
    public void SetForTest(ArchePassDesc desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        _byId[desc.Id] = desc;
    }

    /// <summary>For tests: replaces the tier list of one pass.</summary>
    public void SetTiersForTest(uint passId, IEnumerable<ArchePassTierDesc> tiers)
    {
        _tiersByPass[passId] = tiers?.ToList() ?? [];
    }

    public void ClearForTest()
    {
        _byId.Clear();
        _tiersByPass.Clear();
    }
}
