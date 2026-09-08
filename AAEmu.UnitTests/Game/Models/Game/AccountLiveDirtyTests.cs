using System.Collections.Concurrent;
using AAEmu.Game.Models.Game;

namespace AAEmu.UnitTests.Game.Models.Game;

public class AccountLiveDirtyTests
{
    [Test]
    public async Task ShouldClear_OnlyWhenTheStampIsUnchanged()
    {
        await Assert.That(AccountLiveDirty.ShouldClear(3, 3)).IsTrue();
        await Assert.That(AccountLiveDirty.ShouldClear(3, 4)).IsFalse();
    }

    [Test]
    public async Task ClearIfUnchanged_KeepsAKeyMarkedAgainAfterTheWrite()
    {
        var dirty = new ConcurrentDictionary<uint, int>();
        dirty[7] = 1;
        AccountLiveDirty.ClearIfUnchanged(dirty, 7u, 1);
        await Assert.That(dirty.ContainsKey(7)).IsFalse();

        dirty[7] = 1;
        dirty[7] = 2;
        AccountLiveDirty.ClearIfUnchanged(dirty, 7u, 1);
        await Assert.That(dirty[7]).IsEqualTo(2);
    }
}
