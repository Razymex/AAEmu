using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// A Hero confirms the issue dialog on a rally flag. Carries the flag doodad the dialog was opened on.
/// </summary>
public class CSFactionIssuanceOfMobilizationOrderPacket() : GamePacket(CSOffsets.CSFactionIssuanceOfMobilizationOrderPacket, 1)
{
    public uint DoodadObjId { get; private set; }

    public override void Read(PacketStream stream)
    {
        DoodadObjId = stream.ReadBc();

        if (Connection.ActiveChar != null)
            HeroManager.Instance.IssueMobilizationOrder(Connection.ActiveChar, DoodadObjId);
    }
}
