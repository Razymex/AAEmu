using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Reset one Bless Uthstin page. The client also echoes that page's applied stats.</summary>
public class CSBlessUthstinInitStatsPacket() : GamePacket(CSOffsets.CSBlessUthstinInitStatsPacket, 1)
{
    public int UthstinPageIndex { get; private set; }

    public override void Read(PacketStream stream)
    {
        UthstinPageIndex = stream.ReadInt32();
        _ = stream.ReadInt32(); // changeStat — client echo, not authoritative
        for (var i = 0; i < BlessUthstinRules.StatCount; i++)
            _ = stream.ReadInt32();

        Connection.ActiveChar?.BlessUthstin?.TryInit(UthstinPageIndex);
    }
}
