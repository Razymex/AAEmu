using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.CrossServer;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// Client asks to leave this server for another one.
/// </summary>
/// <remarks>
/// The departure request has no body; extra bytes are rejected instead of being ignored.
/// The destination is resolved from configured server metadata (see <see cref="ICrossServerDirectory"/>).
/// </remarks>
public class CSDepartToForeignServerPacket() : GamePacket(CSOffsets.CSDepartToForeignServerPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        if (stream.LeftBytes != 0)
            throw new InvalidDataException(
                $"Expected a 0-byte departure body (0x1C7 has no fields), got {stream.LeftBytes} bytes.");

        var connection = Connection;
        var character = connection?.ActiveChar;
        if (connection == null || character == null)
        {
            Logger.Error("CSDepartToForeignServer arrived outside a world session; departure refused.");
            return;
        }

        if (!connection.ForeignPassportIssued)
        {
            Logger.Error(
                "Cross-server departure refused for {0}: no passport was issued on this connection.",
                character.Name);
            return;
        }

        // characters.transfer_request_time is the park marker and is written by every
        // Character.Save(), so the live character must carry the same timestamp the journal is
        // about to persist; on refusal the previous value is put back untouched.
        var parkedAtUtc = DateTime.UtcNow;
        var previousTransferRequest = character.TransferRequestTime;
        character.TransferRequestTime = parkedAtUtc;

        var result = CrossServerTransferManager.Instance.RequestDeparture(
            character.Id, connection.AccountId, targetServerKey: null, parkedAtUtc,
            character.Money, character.Money2, character.AaPoint);

        if (result.Outcome != CrossServerTransferOutcome.Granted)
        {
            character.TransferRequestTime = previousTransferRequest;
            // Refusals are logged rather than answered; this handler does not emit a refusal packet.
            Logger.Error(
                "Cross-server departure refused for {0} (ObjId {1}): {2}.",
                character.Name, character.ObjId, result.Outcome);
            return;
        }

        connection.SendPacket(new SCDepartureServerGrantedPacket());
    }
}
