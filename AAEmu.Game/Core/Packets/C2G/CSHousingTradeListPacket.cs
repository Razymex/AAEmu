using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// TODO: the body is parsed but nothing acts on it yet.
/// </summary>
/// <remarks>
/// Field order, widths and names come from the 10.0.2.13 client's serializer, which passes each
/// value's name alongside the value:
/// </remarks>
public class CSHousingTradeListPacket() : GamePacket(CSOffsets.CSHousingTradeListPacket, 1)
{
    // Fix: live capture shows a single u16 zone group id (e.g. Ynystere=17); filter/search stay client-side
    public short ZoneGroup { get; private set; }

    public override void Read(PacketStream stream)
    {

        ZoneGroup = stream.ReadInt16();
        while (stream.HasBytes)
            stream.ReadByte(); // Fix: drain tail

        HousingManager.Instance.HousingTradeList(Connection, ZoneGroup);
    }
}
