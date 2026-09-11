using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// Combo names the next hold-hit for the client. World executes that skill only
/// when the client starts it.
/// </summary>
public class Combo : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.Combo;

    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int comboSkillId,
        int timeFromNow,
        int value3,
        int value4)
    {
    }
}
