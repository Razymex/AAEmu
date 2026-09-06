using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>A ballot: count-prefixed candidate character ids (multi-seat) followed by the voter's character id.</summary>
public class CSHeroVotingPacket() : GamePacket(CSOffsets.CSHeroVotingPacket, 1)
{
    public List<ulong> CandidateCharacterIds { get; private set; } = [];
    public ulong VoterCharacterId { get; private set; }

    public override void Read(PacketStream stream)
    {
        var count = stream.ReadInt32();
        CandidateCharacterIds = new List<ulong>(Math.Max(count, 0));
        for (var i = 0; i < count; i++)
            CandidateCharacterIds.Add(stream.ReadUInt64());
        VoterCharacterId = stream.ReadUInt64();

        HeroManager.Instance.Vote(Connection, CandidateCharacterIds);
    }
}
