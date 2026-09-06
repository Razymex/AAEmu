using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Withdraws the caller's own candidacy. The payload id is not trusted; only the caller can abstain.</summary>
public class CSHeroAbstainPacket() : GamePacket(CSOffsets.CSHeroAbstainPacket, 1)
{
    public ulong TypeValue { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadUInt64();
        HeroManager.Instance.Abstain(Connection);
    }
}
