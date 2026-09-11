using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.CashShop;

public class PremiumDetail : PacketMarshaler
{
    public int CId { get; set; }
    public string CName { get; set; }
    public ushort PId { get; set; }
    public byte IsSell { get; set; }
    public byte IsHidden { get; set; }
    public int PTime { get; set; }
    public byte PType { get; set; }
    public int Price { get; set; }
    public uint Id { get; set; }
    public int BCount { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DiscountPrice { get; set; }
    public int BuyLimit { get; set; }

    /// <summary>Placeholder row for a closed catalog. Size on the list packet is 0.</summary>
    public static PremiumDetail Unlisted { get; } = new()
    {
        CName = string.Empty,
        IsHidden = 1,
        Url = string.Empty
    };

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(CId);
        stream.Write(CName ?? string.Empty);
        stream.Write(PId);
        stream.Write(IsSell);
        stream.Write(IsHidden);
        stream.Write(PTime);
        stream.Write(PType);
        stream.Write(Price);
        stream.Write(Id);
        stream.Write(BCount);
        stream.Write(Url ?? string.Empty);
        stream.Write(DiscountPrice);
        stream.Write(BuyLimit);
        return stream;
    }
}

