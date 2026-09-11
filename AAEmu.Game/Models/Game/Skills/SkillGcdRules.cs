namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Shared GCD length. Weapon swings use attack-speed (<c>GlobalCooldownMul</c>).
/// Spells use cast speed. Flamebolt is <c>use_weapon_cooldown_time=f</c> — applying
/// attack speed to its 1000 ms GCD made hold-repeat look like a machine gun.
/// </summary>
public static class SkillGcdRules
{
    public static float SharedGcdMultiplier(bool useWeaponCooldownTime, float globalCooldownMul, float castTimeMul)
    {
        if (useWeaponCooldownTime)
            return globalCooldownMul / 100f;
        return castTimeMul;
    }
}
