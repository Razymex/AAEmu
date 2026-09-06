using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>One seated hero: same shape as a candidate row with the hero grade in place of votes/reputation.</summary>
public readonly record struct HeroListEntry(
    uint SeasonId, uint CharacterId, uint TopFactionId, uint ExpeditionId,
    int Ranking, int Score, int AccumPoint, byte HeroGrade);

/// <summary>The seated heroes of the latest finalized cycle. The leading i32 is unused by the panel and sent as 0.</summary>
public sealed class SCHeroListPacket(IReadOnlyList<HeroListEntry> heroes) : GamePacket(SCOffsets.SCHeroListPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(0);
        stream.Write(heroes.Count);
        foreach (var hero in heroes)
        {
            stream.Write(hero.SeasonId);
            stream.Write((ulong)hero.CharacterId);
            stream.Write(hero.TopFactionId);
            stream.Write(hero.ExpeditionId);
            stream.Write(hero.Ranking);
            stream.Write(hero.Score);
            stream.Write(hero.AccumPoint);
            stream.Write(hero.HeroGrade);
        }
        return stream;
    }
}
