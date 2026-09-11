using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Component skills/buffs/AI run once when the component first succeeds.
/// Re-eval (same target still selected, orb use, later ticks) must not fire them again.
/// A play-cinema-before-bubble buff (the post-scene teleport) waits until that
/// cinema ends. Talk skills on the same quest fire when their component succeeds.
/// </summary>
public static class QuestComponentEffectRules
{
    public static bool TryMarkApplied(uint componentId, ISet<uint> applied)
    {
        if (componentId == 0 || applied == null)
            return false;
        return applied.Add(componentId);
    }

    public static bool ShouldDeferUntilCinema(bool playCinemaBeforeBubble, uint buffId, uint cinemaId) =>
        playCinemaBeforeBubble && buffId != 0 && cinemaId != 0;

    public static void ApplySkillAndBuff(ICharacter owner, QuestComponentTemplate component, ISkillManager skillManager)
    {
        if (owner == null || component == null)
            return;

        if (component.SkillId > 0)
        {
            if (component.SkillSelf)
            {
                owner.UseSkill(component.SkillId, owner);
            }
            else if (component.NpcId > 0 && owner is Character character)
            {
                var npc = character.ParentWorld.GetNpcByTemplateId(component.NpcId);
                npc?.UseSkill(component.SkillId, npc);
            }
        }

        if (component.BuffId > 0 && skillManager != null)
        {
            var buff = skillManager.GetBuffTemplate(component.BuffId);
            if (buff != null)
            {
                owner.Buffs.AddBuff(new Buff(
                    owner,
                    owner,
                    SkillCaster.GetByType(SkillCasterType.Unit),
                    buff,
                    null,
                    DateTime.UtcNow));
            }
        }
    }
}
