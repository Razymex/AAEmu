using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// Rotate Building confirm from housing_manager.lua HOUSE_ROTATE_CONFIRM.
/// bc u24 (house objId) + zRot f32 + height f32 (client native X2House:RotateHouse).
/// </summary>
public class CSRotateHousePacket() : GamePacket(CSOffsets.CSRotateHousePacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var bc = stream.ReadBc();
        var zRot = stream.ReadSingle();
        var height = stream.ReadSingle();
        Logger.Debug("RotateHouse, Bc: {0}, ZRot: {1}, Height: {2}", bc, zRot, height);
        HousingManager.Instance.RotateHouse(Connection, bc, zRot, height);
    }
}
