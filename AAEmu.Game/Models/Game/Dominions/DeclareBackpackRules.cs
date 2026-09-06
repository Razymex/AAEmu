using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.Game.Models.Game.Dominions;

/// <summary>
/// Skill 13661 has no reagents. The client only requires backpack slot 26; the item that must
/// disappear on a successful declare is <see cref="BackpackType.CastleClaim"/> (Nuia/Haranya/pirate
/// 정화의 아키움). Put-down skills are a different consume path. Trade packs stay on.
/// </summary>
public static class DeclareBackpackRules
{
    public static bool ShouldConsumeOnDeclare(BackpackType backpackType) =>
        backpackType == BackpackType.CastleClaim;
}
