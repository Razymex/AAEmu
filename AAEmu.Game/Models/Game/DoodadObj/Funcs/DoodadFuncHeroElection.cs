using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// The Hero election voting machine (doodad_func_hero_elections, id-only). Interacting opens the ballot
/// with the current candidate data.
/// </summary>
public class DoodadFuncHeroElection : DoodadFuncTemplate
{
    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        if (caster is not Character character)
            return;

        HeroManager.Instance.SendHeroInfo(character, showUi: true);
    }
}
