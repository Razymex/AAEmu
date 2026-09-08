using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.CashShop;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Utils.DB;
using Microsoft.Data.Sqlite;
using NLog;

namespace AAEmu.Game.GameData;

/// <summary>
/// ArcheLife duration tickets: <c>items.use_skill_id</c> → <c>special_effects</c> of type
/// <see cref="SpecialType.BuyPremium"/>. <c>value1</c> is the duration in days.
/// </summary>
[GameData]
public class PremiumServiceGameData : Singleton<PremiumServiceGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public void Load(SqliteConnection connection)
    {
        var effects = new List<PremiumServiceRules.BuyPremiumEffect>();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT i.id AS item_id, sx.special_effect_type_id, sx.value1
            FROM items i
            JOIN skill_effects se ON se.skill_id = i.use_skill_id
            JOIN effects e ON e.id = se.effect_id AND e.actual_type = 'SpecialEffect'
            JOIN special_effects sx ON sx.id = e.actual_id
            WHERE sx.special_effect_type_id = @buyPremium
            """;
        command.Parameters.AddWithValue("@buyPremium", (int)SpecialType.BuyPremium);
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            effects.Add(new PremiumServiceRules.BuyPremiumEffect(
                reader.GetUInt32("item_id"),
                reader.GetInt32("special_effect_type_id"),
                reader.GetInt32("value1")));
        }

        var passes = PremiumServiceRules.FromBuyPremiumEffects(effects);
        PremiumServiceRules.ReplacePasses(passes);
        Logger.Info("Loaded {0} premium duration tickets", passes.Count);
    }

    public void PostLoad()
    {
    }
}
