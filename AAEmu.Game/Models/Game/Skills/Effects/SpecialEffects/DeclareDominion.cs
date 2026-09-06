using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Dominions;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class DeclareDominion : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.DeclareDominion;

    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int value1,
        int value2,
        int value3,
        int value4)
    {
        if (caster is Character)
            Logger.Debug("Special effects: DeclareDominion value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

        if (caster is not Character declarer)
            return;

        var existingHouse = target as House;
        var marker = target as Doodad;
        if (existingHouse == null && marker == null)
            return;

        var zoneKey = existingHouse != null ? existingHouse.Transform.ZoneId : marker.Transform.ZoneId;
        var zone = ZoneManager.Instance.GetZoneByKey(zoneKey);
        if (zone == null)
            return;

        var zoneId = (ushort)zone.GroupId;
        var isFactionTerritory = SiegeGameData.Instance.GetSiegeZoneSchedule(zoneId) != null;
        uint expeditionId = 0;
        uint owningFactionId = 0;
        if (isFactionTerritory)
            owningFactionId = (uint)DominionManager.ResolveOwningFaction(declarer);
        else if (declarer.Expedition != null)
            expeditionId = (uint)declarer.Expedition.Id;

        var lodestone = existingHouse ?? DominionManager.FindLodestoneInZoneGroup(zoneId);
        var refuse = DominionClaimRules.GetDeclareRefuse(
            DominionZoneLockManager.Instance.IsLocked(zoneId),
            isFactionTerritory,
            HeroManager.Instance.IsCurrentHero(declarer),
            owningFactionId != 0,
            declarer.Expedition != null,
            SiegeManager.Instance.IsDeclareDominionWindowOpen(zoneId, DateTime.UtcNow),
            DominionManager.Instance.GetByZoneId(zoneId) != null || GuildDominionManager.Instance.GetByZoneId(zoneId) != null,
            lodestone != null && SiegeGameData.Instance.IsLodestoneTemplate(lodestone.TemplateId));

        if (refuse != DominionDeclareRefuse.None)
        {
            if (DominionClaimRules.MessageFor(refuse) is { } error)
                declarer.SendErrorMessage(error);
            return;
        }

        var claimed = isFactionTerritory
            ? DominionManager.Instance.DeclareForFaction(zoneId, owningFactionId, lodestone, declarer)
            : GuildDominionManager.Instance.Declare(zoneId, expeditionId, lodestone, declarer);
        if (claimed != null)
            ConsumeCastleClaimBackpack(declarer);
    }

    private static void ConsumeCastleClaimBackpack(Character declarer)
    {
        var pack = declarer.Inventory.GetEquippedBySlot(EquipmentItemSlot.Backpack);
        if (pack?.Template is not BackpackTemplate backpack || !DeclareBackpackRules.ShouldConsumeOnDeclare(backpack.BackpackType))
            return;
        if (pack._holdingContainer == null)
            return;
        pack._holdingContainer.ConsumeItem(ItemTaskType.SkillReagents, pack.TemplateId, 1, pack);
    }
}
