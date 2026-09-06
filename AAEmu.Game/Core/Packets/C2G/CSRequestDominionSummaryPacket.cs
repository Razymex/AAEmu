using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// TODO: TypeValue's real meaning is unconfirmed (possibly a filter for one specific dominion/zone group rather
/// than "all"). Until that's confirmed against the client, this answers with every currently-claimed dominion,
/// same as CSRequestDominionDataPacket, rather than a filtered summary.
/// </summary>
public class CSRequestDominionSummaryPacket() : GamePacket(CSOffsets.CSRequestDominionSummaryPacket, 1)
{
    public short TypeValue { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadInt16();
        DominionManager.Instance.SendAllDominionsTo(Connection);
        GuildDominionManager.Instance.SendAllDominionsTo(Connection);
    }
}
