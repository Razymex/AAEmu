using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterBlessUthstinTests
{
    private const uint TestItemId = 9001;

    [Test]
    public async Task ApplyTrue_CommitsThePendingRoll()
    {
        var state = Create();
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsTrue();
        await Assert.That(state.TryPeekPendingForTests(out _, out var itemType, out var inc, out var dec, out var incPts, out var decPts)).IsTrue();
        await Assert.That(state.TryApply(true, (int)itemType, inc, dec, incPts, decPts, 0)).IsTrue();

        await Assert.That(state.Pages[0].Strength).IsEqualTo(2);
        await Assert.That(state.Pages[0].Dexterity).IsEqualTo(-1);
        await Assert.That(state.Pages[0].ApplyNormalCount).IsEqualTo(1);
        await Assert.That(state.TryPeekPendingForTests(out _, out _, out _, out _, out _, out _)).IsFalse();
    }

    [Test]
    public async Task ApplyFalse_DropsTheRollAndKeepsThePage()
    {
        var state = Create();
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsTrue();
        await Assert.That(state.TryApply(false, 0, 0, 0, 0, 0, 0)).IsTrue();
        await Assert.That(state.Pages[0].Strength).IsEqualTo(0);
        await Assert.That(state.Pages[0].ApplyNormalCount).IsEqualTo(0);
        await Assert.That(state.TryApply(true, (int)TestItemId, 0, 1, 2, 1, 0)).IsFalse();
    }

    [Test]
    public async Task NormalApplyLimit_BlocksASecondConsumeUntilInit()
    {
        var state = Create();
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsTrue();
        state.TryPeekPendingForTests(out _, out var itemType, out var inc, out var dec, out var incPts, out var decPts);
        await Assert.That(state.TryApply(true, (int)itemType, inc, dec, incPts, decPts, 0)).IsTrue();
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsFalse();

        await Assert.That(state.TryInit(0)).IsTrue();
        await Assert.That(state.Pages[0].Strength).IsEqualTo(0);
        await Assert.That(state.Pages[0].ApplyNormalCount).IsEqualTo(0);
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsTrue();
    }

    [Test]
    public async Task ExpandCopyAndSelect_UseZeroBasedPages()
    {
        var state = Create();
        state.Pages[0].Strength = 5;
        state.Pages[0].ApplyNormalCount = 1;

        await Assert.That(state.TryExpand()).IsTrue();
        await Assert.That(state.Pages.Count).IsEqualTo(2);
        await Assert.That(state.TryCopy(0, 1)).IsTrue();
        await Assert.That(state.Pages[1].Strength).IsEqualTo(5);
        await Assert.That(state.Pages[1].ApplyNormalCount).IsEqualTo(1);
        await Assert.That(state.TrySelect(1)).IsTrue();
        await Assert.That(state.SelectPageIndex).IsEqualTo(1);
    }

    [Test]
    public async Task Extend_RaisesTheCapByTheConfiguredStep()
    {
        var state = Create();
        await Assert.That(state.TryExtend()).IsTrue();
        await Assert.That(state.ExtendMaxStats).IsEqualTo(BlessUthstinRules.ExtendPerPoint);
        await Assert.That(state.ApplyExtendCount).IsEqualTo(1);
    }

    [Test]
    public async Task PersistFailure_RevertsExtendExpandAndSelect()
    {
        var state = Create();
        state.FailNextPersist = true;
        await Assert.That(state.TryExtend()).IsFalse();
        await Assert.That(state.ExtendMaxStats).IsEqualTo(0);
        await Assert.That(state.ApplyExtendCount).IsEqualTo(0);

        state.FailNextPersist = true;
        await Assert.That(state.TryExpand()).IsFalse();
        await Assert.That(state.Pages.Count).IsEqualTo(1);

        await Assert.That(state.TryExpand()).IsTrue();
        state.FailNextPersist = true;
        await Assert.That(state.TrySelect(1)).IsFalse();
        await Assert.That(state.SelectPageIndex).IsEqualTo(0);
    }

    [Test]
    public async Task PersistFailure_RevertsAnAppliedRoll()
    {
        var state = Create();
        await Assert.That(state.TryConsumeApply(TestItemId, 0)).IsTrue();
        await Assert.That(state.TryPeekPendingForTests(out _, out var itemType, out var inc, out var dec, out var incPts, out var decPts)).IsTrue();
        state.FailNextPersist = true;
        await Assert.That(state.TryApply(true, (int)itemType, inc, dec, incPts, decPts, 0)).IsFalse();
        await Assert.That(state.Pages[0].Strength).IsEqualTo(0);
        await Assert.That(state.Pages[0].ApplyNormalCount).IsEqualTo(0);
    }

    [Test]
    public async Task SelectPending_UsesTheStashedZeroBasedPage()
    {
        var state = Create();
        await Assert.That(state.TryExpand()).IsTrue();
        state.SetPendingSelectPage(1);
        await Assert.That(state.TrySelectPending()).IsTrue();
        await Assert.That(state.SelectPageIndex).IsEqualTo(1);
    }

    private static CharacterBlessUthstin Create()
    {
        BlessUthstinGameData.Instance.SetForTest(new BlessUthstinItem
        {
            ItemId = TestItemId,
            FunctionId = BlessUthstinItem.FunctionNormal,
            RiseCount = 2,
            DropCount = 1,
            RiseWeights = [1, 0, 0, 0, 0],
            DropWeights = [0, 1, 0, 0, 0]
        });

        var character = new Character(new UnitCustomModelParams())
        {
            Id = 1,
            Name = "UthstinTester",
            Level = 50
        };
        return new CharacterBlessUthstin(character)
        {
            BypassChargesForTests = true,
            TestLiveStats = [80, 80, 80, 80, 80],
            TestNext = _ => 0
        };
    }
}
