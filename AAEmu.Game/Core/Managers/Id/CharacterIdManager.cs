using AAEmu.Commons.Utils;
using AAEmu.Game.Utils;

using Microsoft.Extensions.DependencyInjection;

namespace AAEmu.Game.Core.Managers.Id;

public class CharacterIdManager() : IdManager("CharacterIdManager", FirstId, LastId, ObjTables, Exclude), ICharacterIdManager
{
    private static CharacterIdManager _instance;
    // 10.x clients treat character ids below 1000 as system/reserved (owner checks fail for them),
    // so player characters (and shared-pool slaves) must allocate at 1001+. See housing deco gate.
    private const uint FirstId = 0x000003E9; // 1001
    private const uint LastId = 0x00FFFFFF;
    private static readonly uint[] Exclude = [];
    private static readonly string[,] ObjTables = { { "characters", "id" }, { "slaves", "id" } };

    public static CharacterIdManager Instance =>
        _instance ??= SingletonContainer.ServiceProvider?.GetService<CharacterIdManager>() ?? new CharacterIdManager();
}
