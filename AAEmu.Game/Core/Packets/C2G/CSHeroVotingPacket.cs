using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Heroes;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>A ballot: count-prefixed candidate character ids (multi-seat) followed by the voter's character id.</summary>
public class CSHeroVotingPacket() : GamePacket(CSOffsets.CSHeroVotingPacket, 1)
{
    public List<ulong> CandidateCharacterIds { get; private set; } = [];
    public ulong VoterCharacterId { get; private set; }

    public override void Read(PacketStream stream)
    {
        var count = HeroElectionRules.BallotPickCount(stream.ReadInt32(), stream.LeftBytes);
        CandidateCharacterIds = new List<ulong>(count);
        for (var i = 0; i < count; i++)
            CandidateCharacterIds.Add(stream.ReadUInt64());
        VoterCharacterId = stream.ReadUInt64();

        HeroManager.Instance.Vote(Connection, CandidateCharacterIds);
    }
}
