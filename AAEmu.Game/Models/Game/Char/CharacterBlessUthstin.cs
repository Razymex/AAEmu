using AAEmu.Commons.Network;
using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Features;
using AAEmu.Game.Models.Game.Formulas;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

using MySql.Data.MySqlClient;

using NLog;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// Bless Uthstin pages for one character. Seeded with one empty page when nothing is persisted.
/// </summary>
public sealed class CharacterBlessUthstin
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly UnitAttribute[] StatAttributes =
    [
        UnitAttribute.Str, UnitAttribute.Dex, UnitAttribute.Sta, UnitAttribute.Int, UnitAttribute.Spi
    ];

    private readonly object _sync = new();
    private BlessUthstinPendingRoll _pending;

    public CharacterBlessUthstin()
    {
    }

    public CharacterBlessUthstin(Character owner)
    {
        Owner = owner;
    }

    public Character Owner { get; private set; }

    public List<BlessUthstinPage> Pages { get; } = [new BlessUthstinPage()];

    /// <summary>0-based. The client displays <c>index + 1</c>.</summary>
    public int SelectPageIndex { get; set; }

    public int ExtendMaxStats { get; set; }

    public int ApplyExtendCount { get; set; }

    /// <summary>Tests skip item/gold charges.</summary>
    public bool BypassChargesForTests { get; set; }

    /// <summary>Tests force the next persist to fail and then clear this flag.</summary>
    public bool FailNextPersist { get; set; }

    /// <summary>When set, floor/overflow checks use these five live attrs instead of the unit.</summary>
    public int[] TestLiveStats { get; set; }

    /// <summary>When set, consume rolls use this instead of <see cref="Random.Shared"/>.</summary>
    public Func<int, int> TestNext { get; set; }

    public void Bind(Character owner) => Owner = owner;

    public bool TryPeekPendingForTests(
        out int pageIndex,
        out uint itemType,
        out int incKind,
        out int decKind,
        out int incPoints,
        out int decPoints)
    {
        lock (_sync)
        {
            if (_pending == null)
            {
                pageIndex = 0;
                itemType = 0;
                incKind = 0;
                decKind = 0;
                incPoints = 0;
                decPoints = 0;
                return false;
            }

            pageIndex = _pending.PageIndex;
            itemType = _pending.ItemType;
            incKind = _pending.IncKind;
            decKind = _pending.DecKind;
            incPoints = _pending.IncPoints;
            decPoints = _pending.DecPoints;
            return true;
        }
    }

    public void WritePageInfos(PacketStream stream) =>
        BlessUthstinRules.WritePageInfos(stream, Pages, SelectPageIndex, ExtendMaxStats, ApplyExtendCount);

    public bool TryConsumeApply(ulong itemId, int pageIndex)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        Item item;
        BlessUthstinItem desc;
        BlessUthstinPage page;
        BlessUthstinPendingRoll pending;
        int need;
        lock (_sync)
        {
            if (_pending != null)
            {
                Logger.Warn("BlessUthstin consume {0}: already waiting for confirm", Owner.Name);
                return false;
            }

            if (!TryGetPage(pageIndex, out page))
                return false;

            if (BypassChargesForTests)
            {
                if (!BlessUthstinGameData.Instance.TryGet((uint)itemId, out desc))
                    return false;
                item = null;
            }
            else
            {
                item = Owner.Inventory.GetItemById(itemId);
                if (item == null || item.SlotType != SlotType.Inventory)
                    return FailRequiredItem();

                if (!BlessUthstinGameData.Instance.TryGet(item.TemplateId, out desc))
                    return false;
            }

            if (!desc.IsSpecial && page.ApplyNormalCount >= BlessUthstinRules.ApplyLimit)
            {
                Logger.Info("BlessUthstin consume {0}: normal apply limit on page {1}", Owner.Name, pageIndex);
                return false;
            }

            var applyCount = desc.IsSpecial ? page.ApplySpecialCount : page.ApplyNormalCount;
            need = BlessUthstinRules.ConsumeItemCount(applyCount, EvaluateConsumeFormula);
            if (!BypassChargesForTests && Owner.Inventory.GetItemsCount(SlotType.Inventory, item.TemplateId) < need)
                return FailRequiredItem();

            var live = LiveWithoutPage(pageIndex, page);
            if (BlessUthstinRules.ApplyRefuseReason(page, desc, live, ExtendMaxStats) != 0)
                return false;

            if (!BlessUthstinRules.TryRoll(desc, TestNext ?? Random.Shared.Next, out var incKind, out var decKind))
                return false;

            _pending = new BlessUthstinPendingRoll
            {
                PageIndex = pageIndex,
                ItemId = item?.Id ?? 0,
                ItemType = desc.ItemId,
                Special = desc.IsSpecial,
                IncKind = incKind,
                DecKind = decKind,
                IncPoints = desc.RiseCount,
                DecPoints = desc.DropCount,
                NeedCount = need
            };
            pending = _pending;
        }

        Owner.SendPacket(new SCBlessUthstinConsumeApplyStatsPacket(
            Owner.ObjId,
            true,
            (int)pending.ItemType,
            (uint)pending.IncKind,
            (uint)pending.DecKind,
            (uint)pending.IncPoints,
            (uint)pending.DecPoints));
        return true;
    }

    public bool TryApply(
        bool apply,
        int itemType,
        int incKind,
        int decKind,
        int incPoints,
        int decPoints,
        int pageIndex)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        BlessUthstinPendingRoll pending;
        BlessUthstinPage page;
        BlessSnapshot snapshot;
        lock (_sync)
        {
            pending = _pending;
            if (pending == null)
                return false;

            if (!apply)
            {
                _pending = null;
                return true;
            }

            if (pending.PageIndex != pageIndex ||
                pending.ItemType != (uint)itemType ||
                pending.IncKind != incKind ||
                pending.DecKind != decKind ||
                pending.IncPoints != incPoints ||
                pending.DecPoints != decPoints)
            {
                Logger.Warn("BlessUthstin apply {0}: roll does not match pending", Owner.Name);
                return false;
            }

            if (!TryGetPage(pageIndex, out page))
                return false;

            snapshot = Capture();
        }

        if (!BypassChargesForTests)
        {
            var consumeItem = Owner.Inventory.GetItemById(pending.ItemId);
            if (consumeItem == null ||
                Owner.Inventory.Bag.ConsumeItem(
                    ItemTaskType.BlessUthstinChangeStats,
                    pending.ItemType,
                    pending.NeedCount,
                    consumeItem) != pending.NeedCount)
                return FailRequiredItem();
        }

        lock (_sync)
        {
            BlessUthstinRules.ApplyRoll(
                page,
                pending.IncKind,
                pending.DecKind,
                pending.IncPoints,
                pending.DecPoints,
                pending.Special);
            _pending = null;
        }

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundTemplate(pending.ItemType, pending.NeedCount, ItemTaskType.BlessUthstinChangeStats))
            {
                Logger.Error(
                    "BlessUthstin apply persist failed and the item refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }

        RefreshModifiersIfSelected(pageIndex);
        SendApply(pageIndex, page, login: false);
        return true;
    }

    public bool TryInit(int pageIndex)
    {
        if (!FeatureOn() || Owner == null)
            return false;

        BlessUthstinPage page;
        BlessSnapshot snapshot;
        lock (_sync)
        {
            if (!TryGetPage(pageIndex, out page))
                return false;
            if (BlessUthstinRules.PositiveApplied(page) <= 0 &&
                page.ApplyNormalCount == 0 &&
                page.ApplySpecialCount == 0)
                return false;

            snapshot = Capture();
        }

        if (!BypassChargesForTests &&
            !TryConsumeTemplate(
                BlessUthstinRules.InitItemId,
                BlessUthstinRules.InitItemCount,
                ItemTaskType.BlessUthstinInitStats))
            return false;

        lock (_sync)
        {
            page.Clear();
            if (_pending?.PageIndex == pageIndex)
                _pending = null;
        }

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundTemplate(
                    BlessUthstinRules.InitItemId,
                    BlessUthstinRules.InitItemCount,
                    ItemTaskType.BlessUthstinInitStats))
            {
                Logger.Error(
                    "BlessUthstin init persist failed and the item refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }
        RefreshModifiersIfSelected(pageIndex);
        Owner.SendPacket(new SCBlessUthstinInitStatsPacket(Owner.ObjId, true, pageIndex));
        SendApply(pageIndex, page, login: false);
        return true;
    }

    public bool TryExtend()
    {
        if (!FeatureOn() || Owner == null)
            return false;

        BlessSnapshot snapshot;
        int need;
        lock (_sync)
        {
            if (!BlessUthstinRules.CanExtend(ExtendMaxStats))
                return false;

            need = BlessUthstinRules.ExtendItemCount(ApplyExtendCount + 1, EvaluateExtendFormula);
            snapshot = Capture();
        }

        if (!BypassChargesForTests &&
            !TryConsumeTemplate(BlessUthstinRules.ExtendItemId, need, ItemTaskType.BlessUthstinExpandMaxStats))
            return false;

        lock (_sync)
        {
            ExtendMaxStats += BlessUthstinRules.ExtendPerPoint;
            ApplyExtendCount++;
        }

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundTemplate(BlessUthstinRules.ExtendItemId, need, ItemTaskType.BlessUthstinExpandMaxStats))
            {
                Logger.Error(
                    "BlessUthstin extend persist failed and the item refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }
        Owner.SendPacket(new SCBlessUthstinExtendMaxStatsPacket(
            Owner.ObjId,
            true,
            (uint)ExtendMaxStats,
            (uint)ApplyExtendCount));
        return true;
    }

    public bool TryExpand()
    {
        if (!FeatureOn(requirePageOps: true) || Owner == null)
            return false;

        int newIndex;
        int need;
        BlessSnapshot snapshot;
        lock (_sync)
        {
            if (Pages.Count >= BlessUthstinRules.MaxPageCount)
                return false;

            need = BlessUthstinRules.ExpandNeedCount(Pages.Count);
            if (need <= 0)
                return false;

            snapshot = Capture();
        }

        if (!BypassChargesForTests &&
            !TryConsumeTemplate(BlessUthstinRules.ExpandItemId, need, ItemTaskType.BlessUthstinExpandPage))
            return false;

        lock (_sync)
        {
            Pages.Add(new BlessUthstinPage());
            newIndex = Pages.Count - 1;
        }

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundTemplate(BlessUthstinRules.ExpandItemId, need, ItemTaskType.BlessUthstinExpandPage))
            {
                Logger.Error(
                    "BlessUthstin expand persist failed and the item refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }
        Owner.SendPacket(new SCBlessUthstinExpandPagePacket(Owner.ObjId, true, newIndex));
        return true;
    }

    public bool TryCopy(int srcPageIndex, int dstPageIndex)
    {
        if (!FeatureOn(requirePageOps: true) || Owner == null)
            return false;

        BlessUthstinPage src;
        BlessUthstinPage dest;
        long cost;
        BlessSnapshot snapshot;
        lock (_sync)
        {
            if (srcPageIndex == dstPageIndex)
                return false;
            if (!TryGetPage(srcPageIndex, out src) || !TryGetPage(dstPageIndex, out dest))
                return false;

            cost = BlessUthstinRules.CopyCost(BlessUthstinRules.PositiveApplied(src));
            snapshot = Capture();
        }

        if (!TryCharge(cost, ItemTaskType.BlessUthstinCopyPage))
            return false;

        lock (_sync)
        {
            dest.CopyFrom(src);
            if (_pending?.PageIndex == dstPageIndex)
                _pending = null;
        }

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundMoney(cost, ItemTaskType.BlessUthstinCopyPage))
            {
                Logger.Error(
                    "BlessUthstin copy persist failed and the gold refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }
        RefreshModifiersIfSelected(dstPageIndex);
        Owner.SendPacket(new SCBlessUthstinCopyPagePacket(Owner.ObjId, true, dstPageIndex, dest));
        return true;
    }

    public bool TrySelect(int pageIndex)
    {
        if (!FeatureOn(requirePageOps: true) || Owner == null)
            return false;

        BlessUthstinPage page;
        long cost;
        BlessSnapshot snapshot;
        lock (_sync)
        {
            if (!TryGetPage(pageIndex, out page))
                return false;
            if (SelectPageIndex == pageIndex)
                return true;

            cost = BlessUthstinRules.SelectCost(Owner.Level);
            snapshot = Capture();
        }

        if (!TryCharge(cost, ItemTaskType.BlessUthstinSelectPage))
            return false;

        lock (_sync)
            SelectPageIndex = pageIndex;

        if (!TryPersist())
        {
            Restore(snapshot);
            if (!TryRefundMoney(cost, ItemTaskType.BlessUthstinSelectPage))
            {
                Logger.Error(
                    "BlessUthstin select persist failed and the gold refund did not land for {0}",
                    Owner.Name);
            }

            return false;
        }
        ApplyModifiers();
        Owner.SendPacket(new SCBlessUthstinSelectPagePacket(Owner.ObjId, true, pageIndex));
        SendApply(pageIndex, page, login: false);
        return true;
    }

    public void SetPendingSelectPage(int pageIndex)
    {
        lock (_sync)
            _pendingSelectPage = pageIndex;
    }

    public bool TrySelectPending()
    {
        int page;
        lock (_sync)
        {
            page = _pendingSelectPage;
            _pendingSelectPage = -1;
        }

        return page >= 0 && TrySelect(page);
    }

    public void SendLoginApply()
    {
        if (Owner == null)
            return;

        if (!TryGetPage(SelectPageIndex, out var page))
            page = Pages[0];
        ApplyModifiers();
        SendApply(SelectPageIndex, page, login: true);
    }

    public void ApplyModifiers()
    {
        if (Owner == null)
            return;

        if (!TryGetPage(SelectPageIndex, out var page))
            page = Pages.Count > 0 ? Pages[0] : new BlessUthstinPage();

        Owner.Bonuses[BlessUthstinRules.BonusIndex] = [];
        for (var i = 0; i < BlessUthstinRules.StatCount; i++)
        {
            var value = page.GetStat(i);
            if (value == 0)
                continue;
            Owner.AddBonus(BlessUthstinRules.BonusIndex, new Skills.Bonus
            {
                Template = new BonusTemplate
                {
                    Attribute = StatAttributes[i],
                    ModifierType = UnitModifierType.Value,
                    Value = value
                },
                Value = value
            });
        }

        if (!BypassChargesForTests)
        {
            Owner.Hp = Math.Min(Owner.Hp, Owner.MaxHp);
            Owner.Mp = Math.Min(Owner.Mp, Owner.MaxMp);
            if (Owner.ObjId != 0)
                Owner.BroadcastPacket(new SCUnitPointsPacket(Owner.ObjId, Owner.Hp, Owner.Mp), true);
        }
    }

    public void Load(MySqlConnection connection)
    {
        if (Owner == null)
            return;

        try
        {
            var header = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT `select_page_index`, `extend_max_stats`, `apply_extend_count` " +
                    "FROM character_bless_uthstin WHERE `owner` = @owner";
                command.Parameters.AddWithValue("@owner", Owner.Id);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    SelectPageIndex = reader.GetInt32("select_page_index");
                    ExtendMaxStats = reader.GetInt32("extend_max_stats");
                    ApplyExtendCount = reader.GetInt32("apply_extend_count");
                    header = true;
                }
            }

            var loaded = new List<BlessUthstinPage>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT `page_index`, `str`, `dex`, `sta`, `int`, `spi`, `apply_normal`, `apply_special` " +
                    "FROM character_bless_uthstin_pages WHERE `owner` = @owner ORDER BY `page_index`";
                command.Parameters.AddWithValue("@owner", Owner.Id);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    loaded.Add(new BlessUthstinPage
                    {
                        Strength = reader.GetInt32("str"),
                        Dexterity = reader.GetInt32("dex"),
                        Stamina = reader.GetInt32("sta"),
                        Intelligence = reader.GetInt32("int"),
                        Spirit = reader.GetInt32("spi"),
                        ApplyNormalCount = reader.GetInt32("apply_normal"),
                        ApplySpecialCount = reader.GetInt32("apply_special")
                    });
                }
            }

            lock (_sync)
            {
                Pages.Clear();
                if (loaded.Count == 0)
                    Pages.Add(new BlessUthstinPage());
                else
                    Pages.AddRange(loaded.Take(BlessUthstinRules.MaxPageCount));

                if (!header)
                {
                    SelectPageIndex = 0;
                    ExtendMaxStats = 0;
                    ApplyExtendCount = 0;
                }

                if (SelectPageIndex < 0 || SelectPageIndex >= Pages.Count)
                    SelectPageIndex = 0;
            }
        }
        catch (MySqlException ex)
        {
            Logger.Warn(ex, "BlessUthstin load skipped for {0}", Owner.Name);
            lock (_sync)
            {
                if (Pages.Count == 0)
                    Pages.Add(new BlessUthstinPage());
            }
        }
    }

    public void Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        if (Owner == null)
            return;

        try
        {
            Persist(connection, transaction);
        }
        catch (MySqlException ex)
        {
            Logger.Warn(ex, "BlessUthstin save skipped for {0}", Owner.Name);
        }
    }

    private int _pendingSelectPage = -1;

    private bool FeatureOn(bool requirePageOps = false)
    {
        var features = FeaturesManager.Fsets;
        if (features == null)
            return true;
        if (!features.Check(Feature.bless_uthstin))
            return false;
        return !requirePageOps || features.Check(Feature.fset_25_1_unknown);
    }

    private bool TryGetPage(int pageIndex, out BlessUthstinPage page)
    {
        page = null;
        if (pageIndex < 0 || pageIndex >= Pages.Count)
            return false;
        page = Pages[pageIndex];
        return page != null;
    }

    private int[] LiveWithoutPage(int pageIndex, BlessUthstinPage page)
    {
        var live = TestLiveStats is { Length: BlessUthstinRules.StatCount }
            ? (int[])TestLiveStats.Clone()
            : [Owner.Str, Owner.Dex, Owner.Sta, Owner.Int, Owner.Spi];
        if (pageIndex != SelectPageIndex)
            return live;
        for (var i = 0; i < BlessUthstinRules.StatCount; i++)
            live[i] -= page.GetStat(i);
        return live;
    }

    private bool TryCharge(long cost, ItemTaskType task)
    {
        if (BypassChargesForTests || cost <= 0)
            return true;
        return Owner.ChangeMoney(SlotType.Inventory, -cost, task);
    }

    private bool TryRefundMoney(long cost, ItemTaskType task)
    {
        if (BypassChargesForTests || cost <= 0)
            return true;
        return Owner.ChangeMoney(SlotType.Inventory, cost, task);
    }

    private bool TryRefundTemplate(uint templateId, int count, ItemTaskType task)
    {
        if (BypassChargesForTests || count <= 0)
            return true;
        return Owner.Inventory.Bag.AcquireDefaultItem(task, templateId, count);
    }

    private bool TryConsumeTemplate(uint templateId, int count, ItemTaskType task)
    {
        if (count <= 0)
            return false;
        if (!Owner.Inventory.CheckItems(SlotType.Inventory, templateId, count))
            return FailRequiredItem();
        return Owner.Inventory.Bag.ConsumeItem(task, templateId, count, null) == count;
    }

    private bool FailRequiredItem()
    {
        Owner?.SendErrorMessage(ErrorMessageType.NotEnoughRequiredItem);
        return false;
    }

    private void RefreshModifiersIfSelected(int pageIndex)
    {
        if (pageIndex == SelectPageIndex)
            ApplyModifiers();
    }

    private void SendApply(int pageIndex, BlessUthstinPage page, bool login)
    {
        if (Owner == null)
            return;
        Owner.SendPacket(new SCBlessUthstinApplyStatsPacket(Owner.ObjId, true, page, pageIndex, login));
    }

    private static int EvaluateConsumeFormula(int applyCount) =>
        EvaluateFormula(FormulaKind.BlessUthstinConsumeItemNum, applyCount);

    private static int EvaluateExtendFormula(int nextCount) =>
        EvaluateFormula(FormulaKind.BlessUthstinExtendMaxStat, nextCount);

    private static int EvaluateFormula(FormulaKind kind, int applyCount)
    {
        Formula formula = null;
        try
        {
            formula = FormulaManager.Instance.GetFormula((uint)kind);
        }
        catch (NullReferenceException)
        {
            // Formula tables are created in Load(); unit tests never call it.
        }

        if (formula == null)
            return 1;
        var value = formula.Evaluate(new Dictionary<string, double>
        {
            [BlessUthstinRules.FormulaApplyCountKey] = applyCount
        });
        return (int)Math.Max(1, Math.Round(value));
    }

    private sealed class BlessSnapshot
    {
        public List<BlessUthstinPage> Pages { get; init; }
        public int SelectPageIndex { get; init; }
        public int ExtendMaxStats { get; init; }
        public int ApplyExtendCount { get; init; }
        public BlessUthstinPendingRoll Pending { get; init; }
    }

    private BlessSnapshot Capture() =>
        new()
        {
            Pages = Pages.Select(page => page.Clone()).ToList(),
            SelectPageIndex = SelectPageIndex,
            ExtendMaxStats = ExtendMaxStats,
            ApplyExtendCount = ApplyExtendCount,
            Pending = _pending
        };

    private void Restore(BlessSnapshot snapshot)
    {
        lock (_sync)
        {
            Pages.Clear();
            Pages.AddRange(snapshot.Pages.Select(page => page.Clone()));
            SelectPageIndex = snapshot.SelectPageIndex;
            ExtendMaxStats = snapshot.ExtendMaxStats;
            ApplyExtendCount = snapshot.ApplyExtendCount;
            _pending = snapshot.Pending;
        }
    }

    private bool TryPersist()
    {
        if (FailNextPersist)
        {
            FailNextPersist = false;
            return false;
        }

        if (Owner == null || BypassChargesForTests || Owner.Id == 0)
            return true;

        using var connection = MySQL.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            Persist(connection, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "BlessUthstin persist failed for {0}", Owner.Name);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollback)
            {
                Logger.Fatal(rollback, "BlessUthstin persist rollback failed for {0}", Owner.Name);
            }

            return false;
        }
    }

    private void Persist(MySqlConnection connection, MySqlTransaction transaction)
    {
        List<BlessUthstinPage> snapshot;
        int select;
        int extend;
        int applyExtend;
        lock (_sync)
        {
            snapshot = Pages.Select(page => page.Clone()).ToList();
            select = SelectPageIndex;
            extend = ExtendMaxStats;
            applyExtend = ApplyExtendCount;
        }

        using (var command = connection.CreateCommand())
        {
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText =
                "REPLACE INTO character_bless_uthstin " +
                "(`owner`,`select_page_index`,`extend_max_stats`,`apply_extend_count`) " +
                "VALUES (@owner,@select,@extend,@applyExtend)";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.Parameters.AddWithValue("@select", select);
            command.Parameters.AddWithValue("@extend", extend);
            command.Parameters.AddWithValue("@applyExtend", applyExtend);
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM character_bless_uthstin_pages WHERE `owner` = @owner";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.ExecuteNonQuery();
        }

        for (var i = 0; i < snapshot.Count && i < BlessUthstinRules.MaxPageCount; i++)
        {
            var page = snapshot[i];
            using var command = connection.CreateCommand();
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO character_bless_uthstin_pages " +
                "(`owner`,`page_index`,`str`,`dex`,`sta`,`int`,`spi`,`apply_normal`,`apply_special`) " +
                "VALUES (@owner,@page,@str,@dex,@sta,@int,@spi,@normal,@special)";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.Parameters.AddWithValue("@page", i);
            command.Parameters.AddWithValue("@str", page.Strength);
            command.Parameters.AddWithValue("@dex", page.Dexterity);
            command.Parameters.AddWithValue("@sta", page.Stamina);
            command.Parameters.AddWithValue("@int", page.Intelligence);
            command.Parameters.AddWithValue("@spi", page.Spirit);
            command.Parameters.AddWithValue("@normal", page.ApplyNormalCount);
            command.Parameters.AddWithValue("@special", page.ApplySpecialCount);
            command.ExecuteNonQuery();
        }
    }
}

internal sealed class BlessUthstinPendingRoll
{
    public int PageIndex { get; init; }
    public ulong ItemId { get; init; }
    public uint ItemType { get; init; }
    public bool Special { get; init; }
    public int IncKind { get; init; }
    public int DecKind { get; init; }
    public int IncPoints { get; init; }
    public int DecPoints { get; init; }
    public int NeedCount { get; init; }
}
