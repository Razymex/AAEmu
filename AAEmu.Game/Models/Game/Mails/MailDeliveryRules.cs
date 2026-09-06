using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Models.Game.Mails;

/// <summary>
/// Attachment items must be mail-owned before they are written. Item persistence
/// skips <see cref="SlotType.None"/> rows with no owner, and a later load drops
/// those orphaned ids from the letter.
/// </summary>
public static class MailDeliveryRules
{
    public static void PrepareAttachments(BaseMail mail)
    {
        if (mail == null)
            return;

        mail.Header.Attachments = mail.GetTotalAttachmentCount();
        for (var i = 0; i < mail.Body.Attachments.Count; i++)
        {
            var item = mail.Body.Attachments[i];
            if (item == null)
                continue;
            item.SlotType = SlotType.Mail;
            item.Slot = i;
            item.OwnerId = mail.Header.ReceiverId;
        }
    }

    public static bool CanPersistAttachment(Item item) =>
        item is { SlotType: SlotType.Mail, OwnerId: > 0 };
}
