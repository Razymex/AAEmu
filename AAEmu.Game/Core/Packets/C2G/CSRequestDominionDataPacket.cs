using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// packet has no body. Every parameterless C2S type folds onto that one function, so a
/// shared serializer here is identical-COMDAT folding, not a base-class fall-through.
/// </summary>
public class CSRequestDominionDataPacket() : GamePacket(CSOffsets.CSRequestDominionDataPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        DominionManager.Instance.SendAllDominionsTo(Connection);
        GuildDominionManager.Instance.SendAllDominionsTo(Connection);
    }
}
