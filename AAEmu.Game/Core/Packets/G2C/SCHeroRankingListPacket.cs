using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>One ranked candidate row of the panel's ranking tab.</summary>
public readonly record struct HeroRankingEntry(uint CharacterId, int Leadership, int Score, uint ExpeditionId);

/// <summary>
/// Answers the ranking tab's per-faction request with the viewer's own standing and the ranked roster.
/// Every request needs a reply, even an empty roster, or the tab keeps its loading overlay.
/// </summary>
public sealed class SCHeroRankingListPacket(uint factionId, int myLeadership, int myScore, IReadOnlyList<HeroRankingEntry> rankings)
    : GamePacket(SCOffsets.SCHeroRankingListPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((int)factionId);
        stream.Write(myLeadership);
        stream.Write(myScore);
        stream.Write(rankings.Count);
        foreach (var ranking in rankings)
        {
            stream.Write((ulong)ranking.CharacterId);
            stream.Write(ranking.Leadership);
            stream.Write(ranking.Score);
            stream.Write((int)ranking.ExpeditionId);
        }
        return stream;
    }
}
