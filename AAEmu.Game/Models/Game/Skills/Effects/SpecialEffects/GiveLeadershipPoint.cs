using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// Grants leadership to the skill's target (the leadership skills are cast by a commander on the member
/// being credited). Only positive amounts are applied; this effect never deducts.
/// </summary>
public class GiveLeadershipPoint : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.GiveLeadershipPoint;

    public override void Execute(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill, SkillObject skillObject, DateTime time, int amount, int value2, int value3, int value4)
    {
        if (target is Character) { Logger.Debug("Special effects: GiveLeadershipPoint amount {0}, value2 {1}, value3 {2}, value4 {3}", amount, value2, value3, value4); }

        if (amount <= 0)
            return;

        if (target is not Character character)
            return;

        character.ChangeGamePoints(GamePointKind.Leadership, amount);
    }
}
