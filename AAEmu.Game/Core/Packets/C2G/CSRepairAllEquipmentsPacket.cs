using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSRepairAllEquipmentsPacket() : GamePacket(CSOffsets.CSRepairAllEquipmentsPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var autoUseAAPoint = stream.ReadBoolean();
        var inBag = stream.ReadBoolean();

        Logger.Debug("RepairAllEquipments, AutoUseAAPoint: {0}, InBag: {1}", autoUseAAPoint, inBag);

        var source = inBag
            ? Connection.ActiveChar.Inventory.Bag.Items
            : Connection.ActiveChar.Inventory.Equipment.Items;
        Connection.ActiveChar.DoRepair([.. source], autoUseAAPoint);
    }
}
