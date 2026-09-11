using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Combo special (type 48) names the next hold-hit. The client starts that skill
/// when its GCD is up. World does not schedule or delay those hits.
/// </summary>
public static class SkillComboRules
{
    public const int MaxChainLength = 8;

    public static bool TryGetComboFollowUp(SkillTemplate template, out uint nextSkillId, out int delayMs)
    {
        nextSkillId = 0;
        delayMs = 0;
        if (template?.Effects == null)
            return false;
        foreach (var effect in template.Effects)
        {
            if (effect.Template is not SpecialEffect special)
                continue;
            if (special.SpecialEffectTypeId != SpecialType.Combo || special.Value1 <= 0)
                continue;
            nextSkillId = (uint)special.Value1;
            delayMs = Math.Max(0, special.Value2);
            return true;
        }
        return false;
    }

    public static uint? NextFollowUp(uint skillId)
    {
        var template = SkillManager.Instance?.GetSkillTemplate(skillId);
        return TryGetComboFollowUp(template, out var next, out _) ? next : null;
    }

    public static List<uint> WalkChain(uint firstFollowUp, Func<uint, uint?> nextFollowUp)
    {
        var chain = new List<uint>();
        var id = firstFollowUp;
        var guard = 0;
        while (id != 0 && guard++ < MaxChainLength)
        {
            chain.Add(id);
            id = nextFollowUp?.Invoke(id) ?? 0;
        }
        return chain;
    }

    /// <summary>
    /// True when <paramref name="maybeFollowUp"/> is a later hold-hit of
    /// <paramref name="rootSkillId"/>. Used so a repeat of the root does not
    /// cancel an in-flight follow-up plot.
    /// </summary>
    public static bool IsComboFollowUpOf(uint rootSkillId, uint maybeFollowUp, Func<uint, uint?> nextFollowUp)
    {
        if (rootSkillId == 0 || maybeFollowUp == 0 || rootSkillId == maybeFollowUp)
            return false;
        foreach (var id in WalkChain(nextFollowUp?.Invoke(rootSkillId) ?? 0, nextFollowUp))
        {
            if (id == maybeFollowUp)
                return true;
        }
        return false;
    }
}
