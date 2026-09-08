using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.AccountAttendance;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// 31 fixed slots. Each slot is 16 bytes: unix time, archelife flag, then 7 pad bytes.
/// A zero time means that day has not been claimed.
/// </summary>
public class SCAccountAttendancePacket(long[] times = null, bool[] archelife = null)
    : GamePacket(SCOffsets.SCAccountAttendancePacket, 1)
{
    private readonly long[] _times = times ?? new long[AccountAttendanceRules.DaysInPacket];
    private readonly bool[] _archelife = archelife ?? new bool[AccountAttendanceRules.DaysInPacket];

    public override PacketStream Write(PacketStream stream)
    {
        for (var i = 0; i < AccountAttendanceRules.DaysInPacket; i++)
        {
            stream.Write(_times[i]);
            stream.Write(_archelife[i]);
            for (var pad = 0; pad < AccountAttendanceRules.SlotBytes - 9; pad++)
                stream.Write((byte)0);
        }
        return stream;
    }
}
