using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Single-hero update with the same row shape as <see cref="SCHeroListPacket"/>. This is what fills the
/// client's per-character hero map (its IsHero checks); the list packet alone does not.
/// </summary>
public sealed class SCHeroInfoUpdatedPacket(HeroListEntry hero) : GamePacket(SCOffsets.SCHeroInfoUpdatedPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(hero.SeasonId);
        stream.Write((ulong)hero.CharacterId);
        stream.Write(hero.TopFactionId);
        stream.Write(hero.ExpeditionId);
        stream.Write(hero.Ranking);
        stream.Write(hero.Score);
        stream.Write(hero.AccumPoint);
        stream.Write(hero.HeroGrade);
        return stream;
    }
}
