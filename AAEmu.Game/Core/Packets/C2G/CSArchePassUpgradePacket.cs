using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Spend the catalog upgrade item on the in-progress pass. No body.</summary>
public class CSArchePassUpgradePacket() : GamePacket(CSOffsets.CSArchePassUpgradePacket, 1)
{
    public override void Read(PacketStream stream)
    {
        Connection.ActiveChar?.ArchePass?.TryUpgrade();
    }
}
