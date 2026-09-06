using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>The Hero panel's ranking tab asking for one faction's roster (the selected faction id).</summary>
public class CSHeroRankingListPacket() : GamePacket(CSOffsets.CSHeroRankingListPacket, 1)
{
    public int FactionId { get; private set; }

    public override void Read(PacketStream stream)
    {
        FactionId = stream.ReadInt32();
    }

    public override void Execute()
    {
        if (Connection?.ActiveChar != null)
            HeroManager.Instance.SendHeroInfoForRequestedFaction(Connection.ActiveChar, (uint)FactionId);
    }
}
