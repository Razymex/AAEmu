using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.ScheduleItems;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCScheduleItemUpdatePacket(List<ScheduleItem> scheduleItems)
    : GamePacket(SCOffsets.SCScheduleItemUpdatePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        var items = scheduleItems ?? [];
        var count = Math.Min(items.Count, ScheduleItemRules.MaxItemsInPacket);
        stream.Write((byte)count);
        for (var i = 0; i < count; i++)
            stream.Write(items[i]);
        return stream;
    }
}
