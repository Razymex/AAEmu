using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Tasks.Heroes;

public class HeroTickTask : Task
{
    public override void Execute()
    {
        HeroManager.Instance.Tick();
    }
}
