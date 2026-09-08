using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>Claim one free or premium reward tier on the in-progress pass.</summary>
public class CSArchePassGetRewardItemPacket() : GamePacket(CSOffsets.CSArchePassGetRewardItemPacket, 1)
{
    public uint Tier { get; private set; }
    public bool Premium { get; private set; }

    public override void Read(PacketStream stream)
    {
        Tier = stream.ReadUInt32();
        Premium = stream.ReadBoolean();
        Connection.ActiveChar?.ArchePass?.TryClaim(Tier, Premium);
    }
}
