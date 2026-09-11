namespace AAEmu.Game.Models.Game.Slaves;

/// <summary>
/// When a hull's MaxHp rises (kit parts, Ezi +10%), a bar that was already
/// at the old cap stays full. Leaving current HP on the old ceiling makes
/// the client draw wreck LOD and repair doodads on a fresh summon.
/// </summary>
public static class SlaveHealthCapRules
{
    public static int AfterMaxHpChanged(int hp, int oldMaxHp, int newMaxHp)
    {
        if (newMaxHp <= 0)
            return Math.Max(hp, 0);
        if (oldMaxHp > 0 && hp >= oldMaxHp)
            return newMaxHp;
        return Math.Min(hp, newMaxHp);
    }
}
