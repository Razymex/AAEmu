using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Opcode 0x38A. Six u32 fields the client names worldLevel, serverDays, expModifier,
/// hardCapLevel, characterLevel, levelDiff.
/// </summary>
public class SCWorldLevelInfoPacket(WorldLevelInfoWire info) : GamePacket(SCOffsets.SCWorldLevelInfoPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(info.WorldLevel);
        stream.Write(info.ServerDays);
        stream.Write(info.ExpModifier);
        stream.Write(info.HardCapLevel);
        stream.Write(info.CharacterLevel);
        stream.Write(info.LevelDiff);
        return stream;
    }
}
