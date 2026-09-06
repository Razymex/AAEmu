using AAEmu.Commons.Network;
using AAEmu.Commons.Utils;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game;

public class DominionData : PacketMarshaler
{
    public ushort ZoneId { get; set; }
    public uint ExpeditionId { get; set; }

    /// <summary>
    /// Persisted nation ownership for a siege_zones territory. Zero on a guild-owned claim, where
    /// <see cref="ExpeditionId"/> is the owner. Not written on the wire.
    /// </summary>
    public uint OwningFactionId { get; set; }

    /// <summary>
    /// Alliance faction written after ZoneId. The territory UI compares this to the viewer's top-level
    /// faction. Guild id stays on <see cref="ExpeditionId"/>.
    /// </summary>
    public FactionsEnum FactionId { get; set; }
    public uint House { get; set; } // TODO id?
    public int TaxRate { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int CurHouseTaxMoney { get; set; }
    public int CurHuntTaxMoney { get; set; }
    public int PeaceTaxMoney { get; set; }
    public int CurHouseTaxAaPoint { get; set; }
    public int PeaceTaxAaPoint { get; set; }
    public DateTime LastPaidTime { get; set; }
    public DateTime LastSiegeEndTime { get; set; }
    public DateTime ReignStartTime { get; set; }
    public DateTime LastTaxRateChangedTime { get; set; } // TODO in struct long
    public uint ObjId { get; set; }
    public DominionTerritoryData TerritoryData { get; set; }
    public DominionSiegeTimers SiegeTimers { get; set; } // TODO mb not correct namings
    public DateTime NonPvPStart { get; set; }
    public ushort NonPvPDuration { get; set; }

    /// <summary>Trailing bytes the dedicate reader still consumes after the last named field.</summary>
    public const int RequiredPaddingBytes = 36;

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(ZoneId);
        stream.Write((uint)FactionId);
        stream.Write(House);
        stream.Write(TaxRate);
        stream.Write(Helpers.ConvertLongX(X));
        stream.Write(Helpers.ConvertLongY(Y));
        stream.Write(Z);
        stream.Write((long)CurHouseTaxMoney);
        stream.Write((long)CurHuntTaxMoney);
        stream.Write((long)PeaceTaxMoney);
        stream.Write((long)CurHouseTaxAaPoint);
        stream.Write((long)PeaceTaxAaPoint);
        stream.Write((ulong)Helpers.UnixTime(LastPaidTime));
        stream.Write((ulong)Helpers.UnixTime(LastSiegeEndTime));
        stream.Write((ulong)Helpers.UnixTime(ReignStartTime));
        stream.Write((ulong)Helpers.UnixTime(LastTaxRateChangedTime));
        stream.Write(0);
        stream.WriteBc(0u);
        stream.Write(TerritoryData ?? new DominionTerritoryData());
        stream.Write(SiegeTimers?.SiegePeriod ?? (byte)0);
        WriteEmptyRosterRecord(stream);
        stream.Write(0u);
        stream.Write(false);
        stream.Write((ulong)Helpers.UnixTime(NonPvPStart));
        stream.Write(NonPvPDuration);

        for (var i = 0; i < RequiredPaddingBytes; i++)
            stream.Write((byte)0);

        return stream;
    }

    private static void WriteEmptyRosterRecord(PacketStream stream)
    {
        stream.Write(0u);      // unnamed leading 4-byte field
        stream.WriteBc(0u);    // id
        stream.Write(0L);      // Point.x
        stream.Write(0L);      // Point.y
        stream.Write(0f);      // Point.z
        stream.Write((byte)0); // Limit-array: limit
        stream.Write((byte)0); // Limit-array: count (0 entries)
        stream.Write((byte)0); // 32-byte-sub-record array: count (0 entries)
        stream.Write(0u);      // teamId
        stream.Write(0);       // scorePoint
    }
}

public class DominionTerritoryData : PacketMarshaler
{
    public uint Id { get; set; }
    public uint Id2 { get; set; }
    public byte MaxGates { get; set; }
    public byte MaxWalls { get; set; }
    public short RadiusDeclare { get; set; }
    public ushort RadiusDominion { get; set; }
    public short RadiusOffenseHq { get; set; }
    public short RadiusSiege { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(Id);
        stream.Write(Id2);
        stream.Write(MaxGates);
        stream.Write(MaxWalls);
        stream.Write(RadiusDeclare);
        stream.Write(RadiusDominion);
        stream.Write(RadiusOffenseHq);
        stream.Write(RadiusSiege);
        return stream;
    }
}

public class DominionSiegeTimers : PacketMarshaler
{
    public int[] Durations { get; set; } = new int[5];
    public DateTime Started { get; set; }
    public DateTime Fixed { get; set; }
    public int Bdm { get; set; }

    public byte SiegePeriod { get; set; }

    public DominionUnkData UnkData { get; set; }
    public DominionUnkData Unk2Data { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        foreach (var duration in Durations)
            stream.Write(duration);
        stream.Write(Started);
        stream.Write(Fixed);
        stream.Write(Bdm);
        // ---------------------------------
        stream.Write(SiegePeriod);
        // ---------------------------------
        stream.Write(UnkData);
        stream.Write(Unk2Data);
        return stream;
    }
}

public class DominionUnkData : PacketMarshaler
{
    public uint Id { get; set; } // TODO ExpeditionId
    public uint ObjId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public byte Ni { get; set; }
    public byte Nr { get; set; }

    public byte Limit { get; set; }
    public uint[] UnkIds { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(Id);
        stream.WriteBc(ObjId);
        stream.Write(Helpers.ConvertLongX(X));
        stream.Write(Helpers.ConvertLongY(Y));
        stream.Write(Z);
        stream.Write(Ni);
        stream.Write(Nr);
        // -------------------------------
        stream.Write(Limit);
        stream.Write((byte)UnkIds.Length);
        foreach (var unkId in UnkIds)
            stream.Write(unkId);
        return stream;
    }
}
