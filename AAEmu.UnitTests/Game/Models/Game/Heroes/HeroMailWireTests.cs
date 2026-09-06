using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.Heroes;

namespace AAEmu.UnitTests.Game.Models.Game.Heroes;

public class HeroMailWireTests
{
    [Test]
    public async Task Senders_AreLocaleMailFunctions()
    {
        await Assert.That(HeroMailWire.CandidateSender).IsEqualTo(".heroCandidateAlarm");
        await Assert.That(HeroMailWire.ElectionSender).IsEqualTo(".heroElectionItem");
        await Assert.That(HeroMailWire.BonusSender).IsEqualTo(".heroBonusItem");
        await Assert.That(HeroMailWire.MobilizationSender).IsEqualTo(".mobilizationOrderItem");
        await Assert.That(HeroMailWire.LocaleTitle).IsEqualTo("title");
        await Assert.That(HeroMailWire.LocaleBody).IsEqualTo("body");
    }

    [Test]
    public async Task FormatPeriodBody_WrapsMsgAndUnixWindow()
    {
        var start = DateTime.UnixEpoch.AddSeconds(1_700_000_000);
        var end = DateTime.UnixEpoch.AddSeconds(1_700_086_400);
        var text = HeroMailWire.FormatPeriodBody("you are a candidate", start, end);
        await Assert.That(text).IsEqualTo(
            $"body('you are a candidate', '{Helpers.UnixTime(start)}', '{Helpers.UnixTime(end)}')");
    }

    [Test]
    public async Task EscapeLuaSingleQuoted_QuotesAndNewlines()
    {
        await Assert.That(HeroMailWire.EscapeLuaSingleQuoted("it's\nok")).IsEqualTo("it\\'s\\nok");
        await Assert.That(HeroMailWire.EscapeLuaSingleQuoted(null)).IsEqualTo(string.Empty);
    }
}
