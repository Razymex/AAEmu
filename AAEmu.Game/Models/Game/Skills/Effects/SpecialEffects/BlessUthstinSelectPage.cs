using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// Completes a Bless Uthstin page activate after the client casts skill 37244.
/// The page is stashed when CSStartSkill sees that skill id.
/// </summary>
public class BlessUthstinSelectPage : SpecialEffectAction
{
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
        if (caster is not Character character)
            return;

        if (skillObject is SkillObjectBlessUthstinPage page)
            character.BlessUthstin?.SetPendingSelectPage(page.PageIndex);

        character.BlessUthstin?.TrySelectPending();
    }
}
