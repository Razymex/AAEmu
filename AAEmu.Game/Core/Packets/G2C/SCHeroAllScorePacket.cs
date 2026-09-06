using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>One score row of the Hero mission tab: term leadership, previous-period leadership and orders issued.</summary>
public readonly record struct HeroScoreEntry(ulong CharacterId, int Score, int PeriodScore, int MobilizationCount);

/// <summary>
/// Answers the mission tab's per-faction score request. Row layout: character id (u64), type (i32, 0),
/// score, periodScore, mobilizationCount, then a count-prefixed (key, value) map that is sent empty.
/// </summary>
public sealed class SCHeroAllScorePacket(int factionId, IReadOnlyList<HeroScoreEntry> scores) : GamePacket(SCOffsets.SCHeroAllScorePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(factionId);
        stream.Write(scores.Count);
        foreach (var score in scores)
        {
            stream.Write(score.CharacterId);
            stream.Write(0); // type
            stream.Write(score.Score);
            stream.Write(score.PeriodScore);
            stream.Write(score.MobilizationCount);
            stream.Write(0); // today: empty map
        }
        return stream;
    }
}
