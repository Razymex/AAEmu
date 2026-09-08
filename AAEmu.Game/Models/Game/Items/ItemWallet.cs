using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills.Effects;

using NLog;

namespace AAEmu.Game.Models.Game.Items;

/// <summary>
/// Turns bag Lulu stamps into account loyalty and publishes <c>SCBmPoint</c>.
/// </summary>
public static class ItemWallet
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static bool CreditLoyalty(Character character, int amount)
    {
        var add = ItemWalletRules.LoyaltyFromCount(amount);
        if (character == null || add == 0)
            return add == 0;

        if (!AccountManager.Instance.AddLoyalty(character.AccountId, add))
            return false;

        PublishLoyalty(character);
        return true;
    }

    /// <summary>
    /// Stamps already sitting in bag or warehouse (claims from before convert-on-acquire).
    /// </summary>
    public static int ConvertOwnedMileage(Character character)
    {
        if (character?.Inventory == null)
            return 0;

        return ConvertContainer(character, character.Inventory.Bag)
               + ConvertContainer(character, character.Inventory.Warehouse);
    }

    private static int ConvertContainer(Character character, ItemContainer container)
    {
        if (container == null)
            return 0;

        container.GetAllItemsByTemplate(Item.BmMileage, -1, out _, out var total);
        var amount = ItemWalletRules.LoyaltyFromCount(total);
        if (amount <= 0)
            return 0;

        return ConsumeThenCreditLoyalty(character, container, Item.BmMileage, amount);
    }

    public static int ConsumeThenCreditLoyalty(
        Character character,
        ItemContainer container,
        uint templateId,
        int count,
        Item item = null)
    {
        if (character == null || container == null || count <= 0)
            return 0;

        var consumed = 0;
        using (WorldSnapshotCommit.Begin(bypassCharges: false))
        {
            consumed = container.ConsumeItem(ItemTaskType.ConsumeSkillSource, templateId, count, item);
            if (consumed <= 0)
                return 0;

            var loyalty = ItemWalletRules.LoyaltyFromCount(consumed);
            AccountLiveWallet.QueueLoyalty(character.AccountId, loyalty);
            WorldSnapshotCommit.RequestFlush(bypassCharges: false);
            if (WorldSnapshotCommit.FlushNow(bypassCharges: false, () =>
                {
                    AccountLiveWallet.UnqueueLoyalty(character.AccountId, loyalty);
                    TryRestore(character, container, templateId, consumed, "loyalty");
                }))
            {
                PublishLoyalty(character);
                return consumed;
            }

            return 0;
        }
    }

    public static bool CreditCredits(Character character, int amount)
    {
        if (character == null || amount <= 0)
            return amount == 0;

        if (!AccountManager.Instance.AddCredits(character.AccountId, amount))
            return false;

        PublishCredits(character);
        return true;
    }

    public static bool TryRefundCredits(Character character, int amount)
    {
        if (character == null || amount <= 0)
            return amount == 0;

        if (!AccountManager.Instance.RemoveCredits(character.AccountId, amount))
            return false;

        PublishCredits(character);
        return true;
    }

    /// <summary>
    /// Credits on one coupon, from its use skill's <c>GiveCashPoint</c> value1. Zero if the
    /// item is not a cash pack.
    /// </summary>
    public static int CreditsOnTemplate(ItemTemplate template)
    {
        if (template?.UseSkillId == 0)
            return 0;
        var skill = SkillManager.Instance.GetSkillTemplate(template.UseSkillId);
        if (skill?.Effects == null)
            return 0;
        foreach (var effect in skill.Effects)
        {
            if (effect.Template is SpecialEffect special &&
                special.SpecialEffectTypeId == SpecialType.GiveCashPoint &&
                special.Value1 > 0)
                return special.Value1;
        }
        return 0;
    }

    public static bool TryCreditCashPack(Character character, uint templateId, int count)
    {
        var template = ItemManager.Instance.GetTemplate(templateId);
        var per = CreditsOnTemplate(template);
        var total = ItemWalletRules.CreditsFromEffect(per, count);
        if (total <= 0 || character?.Inventory?.Bag == null)
            return false;
        return ConsumeThenCreditCredits(character, character.Inventory.Bag, templateId, count, total);
    }

    /// <summary>
    /// Attendance coupons left in the bag from before cash-on-grant.
    /// </summary>
    public static int ConvertOwnedCashPacks(Character character)
    {
        if (character?.Inventory == null)
            return 0;
        return ConvertCashContainer(character, character.Inventory.Bag)
               + ConvertCashContainer(character, character.Inventory.Warehouse);
    }

    public static bool ConsumeThenCreditCredits(
        Character character,
        ItemContainer container,
        uint templateId,
        int count,
        int credits)
    {
        if (character == null || container == null || count <= 0 || credits <= 0)
            return credits == 0 && count == 0;

        var consumed = 0;
        var pay = 0;
        using (WorldSnapshotCommit.Begin(bypassCharges: false))
        {
            consumed = container.ConsumeItem(ItemTaskType.ConsumeSkillSource, templateId, count, null);
            if (consumed <= 0)
                return false;

            pay = consumed == count
                ? credits
                : ItemWalletRules.CreditsFromEffect(credits / count, consumed);
            if (pay <= 0)
            {
                TryRestore(character, container, templateId, consumed, "credits");
                return false;
            }

            AccountLiveWallet.QueueCredits(character.AccountId, pay);
            WorldSnapshotCommit.RequestFlush(bypassCharges: false);
            if (WorldSnapshotCommit.FlushNow(bypassCharges: false, () =>
                {
                    AccountLiveWallet.UnqueueCredits(character.AccountId, pay);
                    TryRestore(character, container, templateId, consumed, "credits");
                }))
            {
                PublishCredits(character);
                return true;
            }

            return false;
        }
    }

    private static int ConvertCashContainer(Character character, ItemContainer container)
    {
        if (container?.Items == null)
            return 0;

        var credited = 0;
        foreach (var group in container.Items
                     .Where(x => x != null)
                     .GroupBy(x => x.TemplateId)
                     .ToList())
        {
            if (!AccountAttendanceGameData.Instance.IsRewardItem(group.Key))
                continue;
            var per = CreditsOnTemplate(group.First().Template);
            if (per <= 0)
                continue;
            var total = group.Sum(x => x.Count);
            var credits = ItemWalletRules.CreditsFromEffect(per, total);
            if (ConsumeThenCreditCredits(character, container, group.Key, total, credits))
                credited += total;
        }

        return credited;
    }

    private static void PublishLoyalty(Character character)
    {
        character.BmPoint = AccountManager.Instance.GetAccountDetails(character.AccountId).Loyalty;
        character.SendPacket(new SCBmPointPacket(character.BmPoint));
    }

    private static void PublishCredits(Character character)
    {
        var points = AccountManager.Instance.GetAccountDetails(character.AccountId);
        character.SendPacket(new SCICSCashPointPacket(points.Credits));
    }

    private static void TryRestore(
        Character character,
        ItemContainer container,
        uint templateId,
        int consumed,
        string wallet)
    {
        var restore = ItemWalletRules.RestoreAfterFailedCredit(consumed, creditOk: false);
        if (restore <= 0)
            return;
        using (ItemWalletRules.SuppressAcquireConvert())
        {
            if (container.AcquireDefaultItem(ItemTaskType.ConsumeSkillSource, templateId, restore, convertWallet: false))
                return;
        }

        Logger.Error(
            "Wallet convert consumed {0} without a {1} credit and could not restore them for {2}",
            restore,
            wallet,
            character.Name);
    }
}
