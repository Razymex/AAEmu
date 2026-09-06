using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Tasks.Dominions;

public class DominionTaxPayoutTask : Task
{
    public override void Execute()
    {
        DominionManager.Instance.PayoutTax();
    }
}
