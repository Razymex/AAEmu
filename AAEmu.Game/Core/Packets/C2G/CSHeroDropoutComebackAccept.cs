using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Reverses the caller's own withdrawal while the abstain phase is still open.</summary>
public class CSHeroDropoutComebackAccept() : GamePacket(CSOffsets.CSHeroDropoutComebackAccept, 1)
{
    public ulong Type { get; private set; }

    public override void Read(PacketStream stream)
    {
        Type = stream.ReadUInt64();
        HeroManager.Instance.DropoutComeback(Connection);
    }
}
