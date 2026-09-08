using AAEmu.Game.Models.Game;

namespace AAEmu.UnitTests.Game.Models.Game;

public class WorldSaveCommitRulesTests
{
    [Test]
    public async Task CharacterPersistFailure_CancelsTheWholeSnapshot()
    {
        await Assert.That(WorldSaveCommitRules.MustRollback(true)).IsTrue();
        await Assert.That(WorldSaveCommitRules.CanCommit(4, true)).IsFalse();
        await Assert.That(WorldSaveCommitRules.CanCommit(4, false)).IsTrue();
        await Assert.That(WorldSaveCommitRules.CanCommit(0, false)).IsFalse();
    }
}
