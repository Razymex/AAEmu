using AAEmu.Game.Models.Game;

namespace AAEmu.UnitTests.Game.Models.Game;

public class AccountLiveWalletTests
{
    [Test]
    public async Task Queue_HoldsCreditUntilConfirm()
    {
        const uint account = 91001;
        AccountLiveWallet.QueueCredits(account, 200);
        await Assert.That(AccountLiveWallet.PeekCredits(account)).IsEqualTo(200);

        AccountLiveWallet.UnqueueCredits(account, 200);
        await Assert.That(AccountLiveWallet.PeekCredits(account)).IsEqualTo(0);
    }

    [Test]
    public async Task Queue_HoldsLoyaltyUntilUnqueue()
    {
        const uint account = 91002;
        AccountLiveWallet.QueueLoyalty(account, 5);
        await Assert.That(AccountLiveWallet.PeekLoyalty(account)).IsEqualTo(5);

        AccountLiveWallet.UnqueueLoyalty(account, 5);
        await Assert.That(AccountLiveWallet.PeekLoyalty(account)).IsEqualTo(0);
    }

    [Test]
    public async Task DiscardPendingClears_KeepsTheQueueForRetry()
    {
        const uint account = 91003;
        AccountLiveWallet.QueueCredits(account, 100);
        AccountLiveWallet.DiscardPendingClears();
        await Assert.That(AccountLiveWallet.PeekCredits(account)).IsEqualTo(100);

        AccountLiveWallet.UnqueueCredits(account, 100);
        await Assert.That(AccountLiveWallet.PeekCredits(account)).IsEqualTo(0);
    }

    [Test]
    public async Task Unqueue_DoesNotDropALaterQueue()
    {
        const uint account = 91004;
        AccountLiveWallet.QueueLoyalty(account, 3);
        AccountLiveWallet.QueueLoyalty(account, 2);
        AccountLiveWallet.UnqueueLoyalty(account, 3);
        await Assert.That(AccountLiveWallet.PeekLoyalty(account)).IsEqualTo(2);

        AccountLiveWallet.UnqueueLoyalty(account, 2);
    }
}
