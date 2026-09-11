using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// First skill on <c>SCNpcInteractionSkillList</c>.
/// Quest talk is <see cref="SkillsEnum.NpcTalk"/>; zero never opens start/complete.
/// </summary>
public static class NpcInteractionRules
{
    public static uint PrimarySkill(NpcTemplate template, bool questTalk = false)
    {
        if (template == null)
            return 0;
        if (questTalk)
            return SkillsEnum.NpcTalk;
        if (template.Banker)
            return SkillsEnum.UseWarehouse;
        if (template.AbilityChanger)
            return SkillsEnum.ChangeSkillsets;
        if (template.Auctioneer)
            return SkillsEnum.UseAuctioneer;
        if (template.Priest)
            return SkillsEnum.Blessing;
        if (template.Repairman)
            return SkillsEnum.Repair;
        if (template.Merchant)
            return SkillsEnum.UseStore;
        if (template.Stabler)
            return SkillsEnum.HealPetSWounds;
        if (template.Expedition)
            return SkillsEnum.FormGuild;
        if (template.RecrutingBattlefieldId > 0)
            return SkillsEnum.WarSupport;
        if (template.Blacksmith)
            return SkillsEnum.ItemFusion;
        return 0;
    }
}
