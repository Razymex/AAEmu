using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Tasks.Sieges;

public class SiegeTickTask : Task
{
    public override void Execute()
    {
        SiegeManager.Instance.Tick();
    }
}
