using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSAddAccountAttendancePacket() : GamePacket(CSOffsets.CSAddAccountAttendancePacket, 1)
{
    public ulong TypeValue { get; private set; }
    public uint DayOffset { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadUInt64();
        DayOffset = stream.ReadUInt32();
    }

    public override void Execute()
    {
        AccountAttendanceManager.Instance.HandleAdd(Connection.ActiveChar);
    }
}
