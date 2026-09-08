using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Containers;

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

    /// <summary>
    /// Staging into <c>MailAttachments</c> is not a delivery. Only a successful send is.
    /// Drop the staged rows so a failed send can retry without leaking items.
    /// </summary>
    public static bool TryDiscardStagedAttachments(ItemContainer container, IEnumerable<Item> staged)
    {
        if (container == null)
        {
            if (staged == null)
                return true;
            foreach (var item in staged)
            {
                if (item != null)
                    return false;
            }

            return true;
        }

        var ok = true;
        if (staged == null)
            return true;

        foreach (var item in staged)
        {
            if (item == null)
                continue;
            if (!container.RemoveItem(ItemTaskType.Invalid, item, true))
                ok = false;
        }

        return ok;
    }

    public static bool IsPublished(BaseMail mail) =>
        mail is { IsPendingPublish: false };

    public static bool CanWorldSaveItem(Item item) =>
        item is { ExcludeFromWorldSave: false };

    public static void HoldAttachmentsFromWorldSave(BaseMail mail, bool hold)
    {
        if (mail?.Body.Attachments == null)
            return;
        foreach (var item in mail.Body.Attachments)
        {
            if (item != null)
                item.ExcludeFromWorldSave = hold;
        }
    }
}
