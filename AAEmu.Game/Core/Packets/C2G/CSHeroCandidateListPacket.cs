using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// The client asks for the Hero data when opening the panel or the voting machine. This is the one request
/// answered with the ballot window open (showUi), so the player's own action opens it.
/// </summary>
public class CSHeroCandidateListPacket() : GamePacket(CSOffsets.CSHeroCandidateListPacket, 1)
{
    public override void Read(PacketStream stream)
    {
    }

    public override void Execute()
    {
        if (Connection?.ActiveChar != null)
            HeroManager.Instance.SendHeroInfo(Connection.ActiveChar, showUi: true);
    }
}
