using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.Game.Models.Game.Items;

/// <summary>
/// Turns bag Lulu stamps into account loyalty and publishes <c>SCBmPoint</c>.
/// </summary>
public static class ItemWallet
{
    public static bool CreditLoyalty(Character character, int amount)
    {
        var add = ItemWalletRules.LoyaltyFromCount(amount);
        if (character == null || add == 0)
            return add == 0;

        if (!AccountManager.Instance.AddLoyalty(character.AccountId, add))
            return false;

        character.BmPoint = AccountManager.Instance.GetAccountDetails(character.AccountId).Loyalty;
        character.SendPacket(new SCBmPointPacket(character.BmPoint));
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

        var consumed = container.ConsumeItem(ItemTaskType.ConsumeSkillSource, Item.BmMileage, amount, null);
        if (consumed <= 0)
            return 0;

        CreditLoyalty(character, consumed);
        return consumed;
    }

    public static bool CreditCredits(Character character, int amount)
    {
        if (character == null || amount <= 0)
            return amount == 0;

        if (!AccountManager.Instance.AddCredits(character.AccountId, amount))
            return false;

        var points = AccountManager.Instance.GetAccountDetails(character.AccountId);
        character.SendPacket(new SCICSCashPointPacket(points.Credits));
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
        return total > 0 && CreditCredits(character, total);
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
            var consumed = container.ConsumeItem(ItemTaskType.ConsumeSkillSource, group.Key, total, null);
            if (consumed <= 0)
                continue;
            if (CreditCredits(character, ItemWalletRules.CreditsFromEffect(per, consumed)))
                credited += consumed;
        }

        return credited;
    }
}
