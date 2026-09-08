using AAEmu.Game.GameData;

namespace AAEmu.UnitTests.Game.GameData;

public class ContentConfigGameDataTests
{
    [Test]
    public async Task RequireInt_ReturnsASeededRow()
    {
        ContentConfigGameData.Instance.SetForTest("bless_uthstin_select_cost", 600);
        await Assert.That(ContentConfigGameData.Instance.RequireInt("bless_uthstin_select_cost")).IsEqualTo(600);
    }

    [Test]
    public async Task RequireInt_ThrowsWhenTheRowIsMissing()
    {
        var threw = false;
        try
        {
            ContentConfigGameData.Instance.RequireInt("definitely_missing_content_config");
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }
}
