using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game;

/// <summary>
/// One HUD timer row. Count is written by the packet; each row is type, gave, cumulated, updated.
/// </summary>
public class ScheduleItem : PacketMarshaler
{
    public int Type { get; set; }
    public byte Gave { get; set; }
    public long Cumulated { get; set; }
    public DateTime Updated { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(Type);
        stream.Write(Gave);
        stream.Write(Cumulated);
        stream.Write(Updated);
        return stream;
    }
}
