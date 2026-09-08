using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterArchePassTests
{
    private const uint TestPassId = 88;

    [Test]
    public async Task Buy_UnknownType_Refuses()
    {
        var state = Create();
        await Assert.That(state.TryBuy(99999)).IsFalse();
        await Assert.That(state.StatusOf(99999)).IsEqualTo(ArchePassStatus.Invalid);
    }

    [Test]
    public async Task BuyThenStart_OwnsThenProgress()
    {
        var state = Create();
        await Assert.That(state.TryBuy(TestPassId)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Owned);
        await Assert.That(state.TryBuy(TestPassId)).IsFalse();

        await Assert.That(state.TryStart(TestPassId)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Progress);
        await Assert.That(state.TryStart(TestPassId)).IsFalse();

        var row = state.Snapshot().Single();
        await Assert.That(row.PassId).IsEqualTo(TestPassId);
        await Assert.That(row.Point).IsEqualTo(0L);
        await Assert.That(row.Premium).IsFalse();
    }

    [Test]
    public async Task Start_WithoutBuy_Refuses()
    {
        var state = Create();
        await Assert.That(state.TryStart(TestPassId)).IsFalse();
    }

    [Test]
    public async Task Start_SecondPass_ParksTheLiveOne()
    {
        var state = Create();
        SeedPass(89);
        await Assert.That(state.TryBuy(TestPassId)).IsTrue();
        await Assert.That(state.TryStart(TestPassId)).IsTrue();
        await Assert.That(state.TryBuy(89)).IsTrue();
        await Assert.That(state.TryStart(89)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Owned);
        await Assert.That(state.StatusOf(89)).IsEqualTo(ArchePassStatus.Progress);
        await Assert.That(state.HasProgress(TestPassId)).IsFalse();
        await Assert.That(state.HasProgress(89)).IsTrue();
    }

    [Test]
    public async Task Remove_DropsAnInProgressPass()
    {
        var state = Started();
        await Assert.That(state.TryRemove(TestPassId)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Dropped);
        await Assert.That(state.TryBuy(TestPassId)).IsTrue();
    }

    [Test]
    public async Task Upgrade_SetsPremiumOnce()
    {
        var state = Started();
        await Assert.That(state.TryUpgrade()).IsTrue();
        await Assert.That(state.Snapshot().Single().Premium).IsTrue();
        await Assert.That(state.TryUpgrade()).IsFalse();
        await Assert.That(state.HasPremium()).IsTrue();
    }

    [Test]
    public async Task AddPoints_UpdatesTheLivePass()
    {
        var state = Started();
        await Assert.That(state.TryAddPoints(300)).IsTrue();
        await Assert.That(state.Snapshot().Single().Point).IsEqualTo(300L);
        await Assert.That(state.TryAddPoints(0)).IsFalse();
    }

    [Test]
    public async Task Claim_TakesTheNextFreeTierOnly()
    {
        var state = Started();
        await Assert.That(state.TryClaim(2, false)).IsFalse();
        await Assert.That(state.TryClaim(1, false)).IsTrue();
        await Assert.That(state.Snapshot().Single().LastRewardTier).IsEqualTo(1u);
        await Assert.That(state.TryClaim(1, false)).IsFalse();
        await Assert.That(state.TryClaim(2, false)).IsFalse();

        await Assert.That(state.TryAddPoints(100)).IsTrue();
        await Assert.That(state.TryClaim(2, false)).IsTrue();
        await Assert.That(state.Snapshot().Single().LastRewardTier).IsEqualTo(2u);
    }

    [Test]
    public async Task Claim_Premium_NeedsUpgradeAndFreeLastFirst()
    {
        var state = Started();
        await Assert.That(state.TryClaim(1, true)).IsFalse();
        await Assert.That(state.TryUpgrade()).IsTrue();
        await Assert.That(state.TryClaim(1, true)).IsTrue();
        await Assert.That(state.Snapshot().Single().LastPremiumRewardTier).IsEqualTo(1u);

        await Assert.That(state.TryAddPoints(200)).IsTrue();
        await Assert.That(state.TryClaim(2, true)).IsFalse();
        await Assert.That(state.TryClaim(1, false)).IsTrue();
        await Assert.That(state.TryClaim(2, false)).IsTrue();
        await Assert.That(state.TryClaim(2, true)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Completed);
    }

    [Test]
    public async Task Complete_NeedsTheLastFreeRewardAndNoPremium()
    {
        var state = Started();
        await Assert.That(state.TryComplete(TestPassId)).IsFalse();
        await Assert.That(state.TryClaim(1, false)).IsTrue();
        await Assert.That(state.TryAddPoints(100)).IsTrue();
        await Assert.That(state.TryClaim(2, false)).IsTrue();
        await Assert.That(state.TryComplete(TestPassId)).IsTrue();
        await Assert.That(state.StatusOf(TestPassId)).IsEqualTo(ArchePassStatus.Completed);
        await Assert.That(state.TryComplete(TestPassId)).IsFalse();
    }

    [Test]
    public async Task MissionComplete_StopsAtTheWeeklyCap()
    {
        var state = Started();
        for (var i = 0; i < ArchePassRules.MissionCompleteMax; i++)
            state.NotifyMissionCompleted();
        var (used, max) = state.MissionCompleteCounts();
        await Assert.That(used).IsEqualTo(max);
        state.NotifyMissionCompleted();
        await Assert.That(state.MissionCompleteCounts().Used).IsEqualTo(max);
    }

    [Test]
    public async Task HasProgress_MatchesTheLivePass()
    {
        var state = Create();
        await Assert.That(state.HasProgress()).IsFalse();
        await Assert.That(state.TryBuy(TestPassId)).IsTrue();
        await Assert.That(state.HasProgress()).IsFalse();
        await Assert.That(state.TryStart(TestPassId)).IsTrue();
        await Assert.That(state.HasProgress()).IsTrue();
        await Assert.That(state.HasProgress(TestPassId)).IsTrue();
        await Assert.That(state.HasProgress(89)).IsFalse();
    }

    private static CharacterArchePass Started()
    {
        var state = Create();
        state.TryBuy(TestPassId);
        state.TryStart(TestPassId);
        return state;
    }

    private static CharacterArchePass Create()
    {
        SeedPass(TestPassId);
        SeedTiers(TestPassId);

        var character = new Character(new UnitCustomModelParams())
        {
            Id = 1,
            Name = "PassTester"
        };
        return new CharacterArchePass(character)
        {
            BypassChargesForTests = true
        };
    }

    private static void SeedPass(uint passId)
    {
        ArchePassGameData.Instance.SetForTest(new ArchePassDesc
        {
            Id = passId,
            Name = "test pass",
            CurrencyId = 0,
            CurrencyValue = 100000,
            UpgradeItemId = 54232,
            MaxTier = 2
        });
    }

    private static void SeedTiers(uint passId)
    {
        ArchePassGameData.Instance.SetTiersForTest(passId,
        [
            new ArchePassTierDesc
            {
                PassId = passId,
                Tier = 1,
                Point = 0,
                RewardItemId = 23633,
                RewardItemCount = 20,
                PremiumRewardItemId = 46828,
                PremiumRewardItemCount = 1
            },
            new ArchePassTierDesc
            {
                PassId = passId,
                Tier = 2,
                Point = 100,
                RewardItemId = 417,
                RewardItemCount = 50,
                PremiumRewardItemId = 52848,
                PremiumRewardItemCount = 3
            }
        ]);
    }
}
