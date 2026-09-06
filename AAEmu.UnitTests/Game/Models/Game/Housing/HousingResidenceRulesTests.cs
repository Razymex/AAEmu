using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.UnitTests.Game.Models.Game.Housing;

public class HousingResidenceRulesTests
{
    [Test]
    public async Task IsExpeditionResidenceFamily_UsesTheShippedFamilyPrefix()
    {
        await Assert.That(HousingResidenceRules.IsExpeditionResidenceFamily("hs_expedition_house_complete")).IsTrue();
        await Assert.That(HousingResidenceRules.IsExpeditionResidenceFamily("hs_nuia_mansion_01_complete")).IsFalse();
        await Assert.That(HousingResidenceRules.IsExpeditionResidenceFamily(null)).IsFalse();
        await Assert.That(HousingResidenceRules.IsExpeditionResidenceFamily("")).IsFalse();
    }
}
