namespace AAEmu.Game.Models.Game.Units;

/// <summary>
/// Zone can keep simulating a unit after World HP is already 0. World must not
/// start a new skill for that caster.
/// </summary>
public static class ZoneDeadUnitRules
{
    public static bool AcceptsZoneSkill(int hp) => hp > 0;
}
