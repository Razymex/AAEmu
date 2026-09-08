using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.UnitTests.Game.GameData;

public class ArchePassGameDataTests
{
    [Test]
    public async Task CurrentTier_IsTheLastReachedThreshold()
    {
        Seed();
        var data = ArchePassGameData.Instance;
        await Assert.That(data.CurrentTier(88, 0)).IsEqualTo(1u);
        await Assert.That(data.CurrentTier(88, 99)).IsEqualTo(1u);
        await Assert.That(data.CurrentTier(88, 100)).IsEqualTo(2u);
    }

    [Test]
    public async Task NextReward_SkipsClaimedAndEmptyTracks()
    {
        Seed();
        var data = ArchePassGameData.Instance;
        await Assert.That(data.TryNextReward(88, 0, 0, false, out var first)).IsTrue();
        await Assert.That(first.Tier).IsEqualTo(1u);
        await Assert.That(data.TryNextReward(88, 0, 1, false, out _)).IsFalse();
        await Assert.That(data.TryNextReward(88, 100, 1, false, out var second)).IsTrue();
        await Assert.That(second.Tier).IsEqualTo(2u);
        await Assert.That(data.TryNextReward(88, 100, 0, true, out var premium)).IsTrue();
        await Assert.That(premium.Tier).IsEqualTo(1u);
    }

    [Test]
    public async Task NextReward_SkipsANullFreeItem()
    {
        ArchePassGameData.Instance.SetForTest(new ArchePassDesc { Id = 87, MaxTier = 2 });
        ArchePassGameData.Instance.SetTiersForTest(87,
        [
            new ArchePassTierDesc { PassId = 87, Tier = 1, Point = 0, RewardItemId = 0, RewardItemCount = 0 },
            new ArchePassTierDesc
            {
                PassId = 87, Tier = 2, Point = 10,
                RewardItemId = 5, RewardItemCount = 1
            }
        ]);

        var data = ArchePassGameData.Instance;
        await Assert.That(data.TryNextReward(87, 0, 0, false, out _)).IsFalse();
        await Assert.That(data.TryNextReward(87, 10, 0, false, out var next)).IsTrue();
        await Assert.That(next.Tier).IsEqualTo(2u);
    }

    private static void Seed()
    {
        ArchePassGameData.Instance.SetForTest(new ArchePassDesc { Id = 88, MaxTier = 2 });
        ArchePassGameData.Instance.SetTiersForTest(88,
        [
            new ArchePassTierDesc
            {
                PassId = 88, Tier = 1, Point = 0,
                RewardItemId = 1, RewardItemCount = 1,
                PremiumRewardItemId = 2, PremiumRewardItemCount = 1
            },
            new ArchePassTierDesc
            {
                PassId = 88, Tier = 2, Point = 100,
                RewardItemId = 3, RewardItemCount = 1,
                PremiumRewardItemId = 4, PremiumRewardItemCount = 1
            }
        ]);
    }
}
