using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Mails;

namespace AAEmu.UnitTests.Game.Models.Game;

public class LiveDirtyGateTests
{
    [Test]
    public async Task TryCapture_FailsWhenClean()
    {
        var gate = new LiveDirtyGate();
        await Assert.That(gate.TryCapture(out var stamp)).IsFalse();
        await Assert.That(stamp).IsEqualTo(0);
    }

    [Test]
    public async Task TryClear_ClearsOnlyWhenTheStampIsUnchanged()
    {
        var gate = new LiveDirtyGate();
        gate.Mark();
        await Assert.That(gate.TryCapture(out var stamp)).IsTrue();
        await Assert.That(gate.TryClear(stamp)).IsTrue();
        await Assert.That(gate.IsDirty).IsFalse();
    }

    [Test]
    public async Task TryClear_KeepsAChangeMadeAfterCapture()
    {
        var gate = new LiveDirtyGate();
        gate.Mark();
        await Assert.That(gate.TryCapture(out var stamp)).IsTrue();
        gate.Mark();
        await Assert.That(gate.TryClear(stamp)).IsFalse();
        await Assert.That(gate.IsDirty).IsTrue();
    }

    [Test]
    public async Task Item_UseAfterCapture_KeepsTheNewCountDirty()
    {
        var item = new Item { IsDirty = false };
        item.Count = 10;
        await Assert.That(item.TryCaptureDirtyStamp(out var stamp)).IsTrue();
        item.Count = 9;
        await Assert.That(item.TryClearDirty(stamp)).IsFalse();
        await Assert.That(item.IsDirty).IsTrue();
        await Assert.That(item.Count).IsEqualTo(9);
    }

    [Test]
    public async Task Item_UnchangedAfterCapture_Clears()
    {
        var item = new Item { IsDirty = false };
        item.Count = 10;
        await Assert.That(item.TryCaptureDirtyStamp(out var stamp)).IsTrue();
        await Assert.That(item.TryClearDirty(stamp)).IsTrue();
        await Assert.That(item.IsDirty).IsFalse();
        await Assert.That(item.Count).IsEqualTo(10);
    }

    [Test]
    public async Task Container_ChangeAfterCapture_StaysDirty()
    {
        var container = new ItemContainer(1, SlotType.Inventory, false, null);
        container.IsDirty = false;
        container.ContainerSize = 50;
        await Assert.That(container.TryCaptureDirtyStamp(out var stamp)).IsTrue();
        container.ContainerSize = 80;
        await Assert.That(container.TryClearDirty(stamp)).IsFalse();
        await Assert.That(container.IsDirty).IsTrue();
        await Assert.That(container.ContainerSize).IsEqualTo(80);
    }

    [Test]
    public async Task Mail_ChangeAfterCapture_StaysDirty()
    {
        var mail = new BaseMail();
        mail.IsDirty = false;
        mail.Title = "written";
        await Assert.That(mail.TryCaptureDirtyStamp(out var stamp)).IsTrue();
        mail.Title = "opened";
        await Assert.That(mail.TryClearDirty(stamp)).IsFalse();
        await Assert.That(mail.IsDirty).IsTrue();
        await Assert.That(mail.Title).IsEqualTo("opened");
    }
}
