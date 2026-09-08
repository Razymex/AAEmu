using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Bless Uthstin apply result. Login sends this once so the window can rebuild
/// <c>statsTable</c> after CharacterState has already listed the pages.
/// </summary>
public class SCBlessUthstinApplyStatsPacket(
    uint bc,
    bool bResult,
    BlessUthstinPage page,
    int targetPageIndex,
    bool bLogin) : GamePacket(SCOffsets.SCBlessUthstinApplyStatsPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(bc);
        stream.Write(bResult);
        BlessUthstinRules.WriteAppliedStats(stream, page);
        stream.Write(targetPageIndex);
        stream.Write(page?.ApplyNormalCount ?? 0);
        stream.Write(page?.ApplySpecialCount ?? 0);
        stream.Write(bLogin);
        return stream;
    }
}
