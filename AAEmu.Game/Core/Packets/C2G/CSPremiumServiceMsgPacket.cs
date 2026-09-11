using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSPremiumServiceMsgPacket() : GamePacket(CSOffsets.CSPremiumServiceMsgPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var stage = stream.ReadInt32();
        Logger.Info("PremiumServieceMsg, stage {0}", stage);
        // NOTE: do NOT reply with SCAccountWarned here (old code did) - its 1.2 body is too
        // short for the 10.0.2.13 layout, the client overruns into countdownTime and char-select breaks.
        // the client polls premium state at login (stage 1) and ~30s later
        // (stage 2) and currently gets NO reply, so the premium service list stays empty and the
        // account flag (+0x28 bit 8, sale permission) never sets -> grey house-sale buttons.
        // Answer both stages with the service list (same entry the LIST handler sends).
        // Answer with the same data-backed catalog the LIST handler pages out, so the poll cannot
        // disagree with it or advertise placeholder SKUs.
        var rows = PremiumServiceCatalog.BuildRows();
        PremiumServiceCatalog.Send(Connection);

        // patron window also waits for point/grade.
        var ch = Connection.ActiveChar;
        if (ch != null)
        {
            Connection.SendPacket(new SCPremiumPointChangedPacket((uint)ch.ObjId, ch.Point));
            var grade = (byte)System.Math.Min(255u, ch.PremiumGrade);
            Connection.SendPacket(new SCUpdatePremiumPointPacket(ch.Point, grade, grade));
            // Bonus rows belong to the listed product; a closed catalog has none.
            Connection.SendPacket(new SCPremiumBonusListPacket(rows.Count > 0 ? (uint)rows[0].CId : 0u, 0, 0));
        }
    }
}













