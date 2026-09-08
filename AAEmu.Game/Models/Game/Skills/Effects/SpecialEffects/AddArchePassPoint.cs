using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class AddArchePassPoint : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.AddArchePassPoint;

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
        if (value1 <= 0)
            return;

        var player = caster as Character ?? target as Character;
        player?.ArchePass?.TryAddPoints(value1);
    }
}
