using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>The Hero's territory dialog distributing Dominion Points; carries the territory's zone group.</summary>
public class CSHeroGiveDominionPointPacket() : GamePacket(CSOffsets.CSHeroGiveDominionPointPacket, 1)
{
    public ushort ZoneId { get; private set; }

    public override void Read(PacketStream stream)
    {
        ZoneId = stream.ReadUInt16();
        if (Connection?.ActiveChar != null)
            HeroManager.Instance.GiveDominionPoint(Connection.ActiveChar, ZoneId);
    }
}
