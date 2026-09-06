using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Mails;

namespace AAEmu.UnitTests.Game.Models.Game.Mails;

public class MailDeliveryRulesTests
{
    [Test]
    public async Task PrepareAttachments_SetsMailSlotAndReceiverOwner()
    {
        var mail = new BaseMail { ReceiverName = "tester" };
        mail.Header.ReceiverId = 9;
        mail.Body.CopperCoins = 10;
        var first = new Item(1) { Id = 100, Count = 2 };
        var second = new Item(1) { Id = 101, Count = 1 };
        mail.Body.Attachments.Add(first);
        mail.Body.Attachments.Add(second);

        MailDeliveryRules.PrepareAttachments(mail);

        await Assert.That(mail.Header.Attachments).IsEqualTo((byte)3);
        await Assert.That(first.SlotType).IsEqualTo(SlotType.Mail);
        await Assert.That(first.Slot).IsEqualTo(0);
        await Assert.That(first.OwnerId).IsEqualTo(9u);
        await Assert.That(second.Slot).IsEqualTo(1);
        await Assert.That(second.OwnerId).IsEqualTo(9u);
        await Assert.That(MailDeliveryRules.CanPersistAttachment(first)).IsTrue();
    }

    [Test]
    public async Task CanPersistAttachment_RejectsUnownedOrNonMailSlots()
    {
        await Assert.That(MailDeliveryRules.CanPersistAttachment(null)).IsFalse();
        await Assert.That(MailDeliveryRules.CanPersistAttachment(new Item(1) { SlotType = SlotType.None, OwnerId = 0 }))
            .IsFalse();
        await Assert.That(MailDeliveryRules.CanPersistAttachment(new Item(1) { SlotType = SlotType.Mail, OwnerId = 0 }))
            .IsFalse();
        await Assert.That(MailDeliveryRules.CanPersistAttachment(new Item(1) { SlotType = SlotType.Inventory, OwnerId = 4 }))
            .IsFalse();
    }

    [Test]
    public async Task IsPublished_IgnoresALetterWaitingOnCommit()
    {
        await Assert.That(MailDeliveryRules.IsPublished(null)).IsFalse();
        await Assert.That(MailDeliveryRules.IsPublished(new BaseMail())).IsTrue();
        await Assert.That(MailDeliveryRules.IsPublished(new BaseMail { IsPendingPublish = true })).IsFalse();
    }
}
