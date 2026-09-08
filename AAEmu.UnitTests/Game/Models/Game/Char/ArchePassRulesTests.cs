using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class ArchePassRulesTests
{
    [Test]
    public async Task Buy_AllowsInvalidAndDroppedOnly()
    {
        await Assert.That(ArchePassRules.CanBuy(ArchePassStatus.Invalid)).IsTrue();
        await Assert.That(ArchePassRules.CanBuy(ArchePassStatus.Dropped)).IsTrue();
        await Assert.That(ArchePassRules.CanBuy(ArchePassStatus.Owned)).IsFalse();
        await Assert.That(ArchePassRules.CanBuy(ArchePassStatus.Progress)).IsFalse();
        await Assert.That(ArchePassRules.CanBuy(ArchePassStatus.Completed)).IsFalse();
    }

    [Test]
    public async Task Start_AllowsOwnedOnly()
    {
        await Assert.That(ArchePassRules.CanStart(ArchePassStatus.Owned)).IsTrue();
        await Assert.That(ArchePassRules.CanStart(ArchePassStatus.Invalid)).IsFalse();
        await Assert.That(ArchePassRules.CanStart(ArchePassStatus.Progress)).IsFalse();
    }

    [Test]
    public async Task Remove_AllowsOwnedOrProgress()
    {
        await Assert.That(ArchePassRules.CanRemove(ArchePassStatus.Owned)).IsTrue();
        await Assert.That(ArchePassRules.CanRemove(ArchePassStatus.Progress)).IsTrue();
        await Assert.That(ArchePassRules.CanRemove(ArchePassStatus.Dropped)).IsFalse();
        await Assert.That(ArchePassRules.CanRemove(ArchePassStatus.Completed)).IsFalse();
    }

    [Test]
    public async Task Upgrade_NeedsProgressNotPremiumAndAnItem()
    {
        await Assert.That(ArchePassRules.CanUpgrade(ArchePassStatus.Progress, false, 54232)).IsTrue();
        await Assert.That(ArchePassRules.CanUpgrade(ArchePassStatus.Progress, true, 54232)).IsFalse();
        await Assert.That(ArchePassRules.CanUpgrade(ArchePassStatus.Progress, false, 0)).IsFalse();
        await Assert.That(ArchePassRules.CanUpgrade(ArchePassStatus.Owned, false, 54232)).IsFalse();
    }

    [Test]
    public async Task Complete_NeedsProgressNotPremiumAndLastFreeReward()
    {
        await Assert.That(ArchePassRules.CanComplete(ArchePassStatus.Progress, false, 20, 20)).IsTrue();
        await Assert.That(ArchePassRules.CanComplete(ArchePassStatus.Progress, true, 20, 20)).IsFalse();
        await Assert.That(ArchePassRules.CanComplete(ArchePassStatus.Progress, false, 19, 20)).IsFalse();
        await Assert.That(ArchePassRules.CanComplete(ArchePassStatus.Owned, false, 20, 20)).IsFalse();
    }

    [Test]
    public async Task LastPremium_WaitsForTheFreeLastReward()
    {
        await Assert.That(ArchePassRules.CanClaimLastPremium(19, 20)).IsFalse();
        await Assert.That(ArchePassRules.CanClaimLastPremium(20, 20)).IsTrue();
    }

    [Test]
    public async Task ChangeMission_StopsAtTheWeeklyCap()
    {
        await Assert.That(ArchePassRules.CanChangeMission(0, 6)).IsTrue();
        await Assert.That(ArchePassRules.CanChangeMission(5, 6)).IsTrue();
        await Assert.That(ArchePassRules.CanChangeMission(6, 6)).IsFalse();
    }

    [Test]
    public async Task WeekStart_UsesConfiguredWeekday()
    {
        var wednesday = new DateTime(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);
        var week = ArchePassRules.WeekStartUtc(wednesday);
        await Assert.That(week.DayOfWeek).IsEqualTo(DayOfWeek.Monday);
        await Assert.That(week.Date).IsEqualTo(new DateTime(2026, 9, 7));
    }

    [Test]
    public async Task WeekStart_DoesNotRollOnADailyMidnightInsideTheWeek()
    {
        var mondayNight = new DateTime(2026, 9, 7, 23, 59, 0, DateTimeKind.Utc);
        var tuesdayMorning = new DateTime(2026, 9, 8, 0, 1, 0, DateTimeKind.Utc);
        var nextMonday = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        var mondayWeek = ArchePassRules.WeekStartUtc(mondayNight);
        var tuesdayWeek = ArchePassRules.WeekStartUtc(tuesdayMorning);
        var nextWeek = ArchePassRules.WeekStartUtc(nextMonday);

        await Assert.That(tuesdayWeek).IsEqualTo(mondayWeek);
        await Assert.That(nextWeek).IsEqualTo(mondayWeek.AddDays(7));
    }

    [Test]
    public async Task CompletedBits_PackAndTestPassIds()
    {
        var words = ArchePassRules.PackCompleted([1, 88, 64]);
        await Assert.That(ArchePassRules.IsCompleted(words, 1)).IsTrue();
        await Assert.That(ArchePassRules.IsCompleted(words, 88)).IsTrue();
        await Assert.That(ArchePassRules.IsCompleted(words, 64)).IsTrue();
        await Assert.That(ArchePassRules.IsCompleted(words, 2)).IsFalse();
        await Assert.That(words.Count).IsEqualTo(2);
        await Assert.That(words[0].Idx).IsEqualTo(0u);
        await Assert.That(words[1].Idx).IsEqualTo(1u);
    }

    [Test]
    public async Task WriteRow_IsPassThenTiersThenPointPremiumStatus()
    {
        var row = new ArchePassProgress
        {
            PassId = 88,
            LastRewardTier = 1,
            LastPremiumRewardTier = 2,
            Point = 15,
            Premium = true,
            Status = ArchePassStatus.Owned
        };
        var stream = new PacketStream();
        ArchePassRules.WriteRow(stream, row);
        var body = stream.GetBytes();

        await Assert.That(BitConverter.ToUInt32(body, 0)).IsEqualTo(88u);
        await Assert.That(BitConverter.ToUInt32(body, 4)).IsEqualTo(1u);
        await Assert.That(BitConverter.ToUInt32(body, 8)).IsEqualTo(2u);
        await Assert.That(BitConverter.ToInt64(body, 12)).IsEqualTo(15L);
        await Assert.That(body[20]).IsEqualTo((byte)1);
        await Assert.That(body[21]).IsEqualTo((byte)ArchePassStatus.Owned);
        await Assert.That(body.Length).IsEqualTo(22);
    }

    [Test]
    public async Task RowsForWire_ClampsToTen()
    {
        var rows = Enumerable.Range(1, 12)
            .Select(id => new ArchePassProgress { PassId = (uint)id, Status = ArchePassStatus.Owned })
            .ToList();
        var wire = ArchePassRules.RowsForWire(rows);
        await Assert.That(wire.Count).IsEqualTo(ArchePassRules.MaxListedPasses);
    }
}
