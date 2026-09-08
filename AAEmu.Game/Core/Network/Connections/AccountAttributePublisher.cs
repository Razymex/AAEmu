using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;

namespace AAEmu.Game.Core.Network.Connections;

/// <summary>
/// Sends the account's attribute list, including the stacked Patron memberships.
/// </summary>
/// <remarks>
/// Shared by the first handshake and by the return from world to character select. The client keeps its
/// membership flags on an object that does not survive that trip - coming back from in-world dropped
/// the account straight back to "Patron 0" - so the list has to go out on both paths, exactly like the
/// character list itself.
/// </remarks>
public static class AccountAttributePublisher
{
    public static void Send(GameConnection connection)
    {
        if (connection == null)
            return;

        var worldId = AppConfiguration.Instance.Id;
        var attributes = AccountAttributeManager.Instance.Get(connection.AccountId, worldId);

        // Memberships are synthesised per session rather than stored, so clearing the setting takes
        // effect on the next login and leaves no rows behind.
        if (AccountPatron.GrantStacked)
        {
            foreach (var membership in AccountPatronRules.StackedMemberships)
            {
                if (attributes.Any(a => a.KindId == (uint)AccountAttributeKind.AccountBuff &&
                                        a.KindValue == membership))
                    continue;

                attributes.Add(new AccountAttribute
                {
                    AccountId = connection.AccountId,
                    KindId = (uint)AccountAttributeKind.AccountBuff,
                    KindValue = membership,
                    WorldId = 0,
                    Count = 1,
                    Starts = AccountPatronRules.PermanentAttributeTime,
                    Expires = AccountPatronRules.PermanentAttributeTime
                });
            }
        }

        AccountAttributeGrantRules.EnsureListingGrant(attributes, connection.AccountId);

        if (attributes.Count == 0)
        {
            connection.SendPacket(new SCAccountAttributeListPacket([]));
            return;
        }

        foreach (var batch in attributes.Chunk(SCAccountAttributeListPacket.MaxAttributes))
            connection.SendPacket(new SCAccountAttributeListPacket(batch));
    }
}
