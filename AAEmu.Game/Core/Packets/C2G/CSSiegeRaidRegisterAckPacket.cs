using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// TypeValue is read as the zone_group_id (small values, matches the pattern) and BRegister as join/leave.
/// TypeValue2's meaning is unconfirmed - not used here (SiegeManager currently defaults an unqualified
/// registration to defense side; no confirmed way to read an offense/defense choice off this packet yet).
/// </summary>
public class CSSiegeRaidRegisterAckPacket() : GamePacket(CSOffsets.CSSiegeRaidRegisterAckPacket, 1)
{
    public short TypeValue { get; private set; }
    public ulong TypeValue2 { get; private set; }
    public bool BRegister { get; private set; }

    public override void Read(PacketStream stream)
    {
        TypeValue = stream.ReadInt16();
        TypeValue2 = stream.ReadUInt64();
        BRegister = stream.ReadBoolean();

        var zoneId = (ushort)TypeValue;
        if (BRegister)
            SiegeManager.Instance.RegisterForRaidTeam(Connection, zoneId, false);
        else
            SiegeManager.Instance.UnregisterFromRaidTeam(Connection, zoneId);
    }
}
