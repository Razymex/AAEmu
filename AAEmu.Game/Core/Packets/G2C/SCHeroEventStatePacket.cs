using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// One running schedule event: the phase, the season (heros.id) it belongs to, and whether this send
/// announces the phase beginning (0), silently resyncs it (1), or announces it ending (2). See
/// <c>HeroElectionRules.BuildEventStateEntries</c>.
/// </summary>
public readonly record struct HeroEventStateEntry(HeroPhase ScheduleEvent, uint Season, byte State = 1);

/// <summary>
/// The Hero schedule state. The client resolves each entry by (event, season) against its own
/// hero_schedules, keys one slot per phase, and fires its election banners only on a state-0 entry.
/// <paramref name="clearAll"/> hides the HUD election icon before applying the entries, so resyncs send
/// false and let the entries overwrite in place.
/// </summary>
public sealed class SCHeroEventStatePacket(bool clearAll, IReadOnlyList<HeroEventStateEntry> entries)
    : GamePacket(SCOffsets.SCHeroEventStatePacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(clearAll);
        stream.Write(entries.Count);
        foreach (var entry in entries)
        {
            stream.Write((byte)entry.ScheduleEvent);
            stream.Write((int)entry.Season);
            stream.Write(entry.State);
        }
        return stream;
    }
}
