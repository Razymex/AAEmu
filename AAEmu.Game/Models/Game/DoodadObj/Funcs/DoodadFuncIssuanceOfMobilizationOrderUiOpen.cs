using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// The nation rally flag's F-key interaction. It has no configuration row; the client opens the issue
/// dialog itself once its cached counters name this flag's zone group, so the server refreshes those
/// counters here. The confirm arrives as CSFactionIssuanceOfMobilizationOrderPacket.
/// </summary>
public class DoodadFuncIssuanceOfMobilizationOrderUiOpen : DoodadFuncTemplate
{
    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        if (caster is not Character character)
            return;

        HeroManager.Instance.SendMobilizationOrderCount(character, MobilizationOrderAction.None);
    }
}
