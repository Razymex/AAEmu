using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>One hero_candidates row for the Hero panel and the voting machine.</summary>
public readonly record struct HeroCandidateEntry(
    uint SeasonId, uint CharacterId, uint TopFactionId, uint ExpeditionId,
    int Ranking, int Score, int AccumPoint, int VoteCount, int Reputation);

/// <summary>
/// A faction's candidate list. <paramref name="showUi"/> opens the ballot window; a background refresh
/// sends false so an open ballot keeps its ticked row. Header: factionId, then season (heros.id).
/// </summary>
public sealed class SCHeroCandidateListPacket(bool showUi, int factionId, int season, IReadOnlyList<HeroCandidateEntry> candidates)
    : GamePacket(SCOffsets.SCHeroCandidateListPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(showUi);
        stream.Write(factionId);
        stream.Write(season);
        stream.Write(candidates.Count);
        foreach (var candidate in candidates)
        {
            stream.Write(candidate.SeasonId);
            stream.Write((ulong)candidate.CharacterId);
            stream.Write(candidate.TopFactionId);
            stream.Write(candidate.ExpeditionId);
            stream.Write(candidate.Ranking);
            stream.Write(candidate.Score);
            stream.Write(candidate.AccumPoint);
            stream.Write(candidate.VoteCount);
            stream.Write(candidate.Reputation);
        }
        return stream;
    }
}
