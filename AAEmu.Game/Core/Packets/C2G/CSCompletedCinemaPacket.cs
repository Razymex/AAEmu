using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSCompletedCinemaPacket() : GamePacket(CSOffsets.CSCompletedCinemaPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        // Empty struct
        var character = Connection.ActiveChar;
        var cinemaId = character.Quests.ResolvePlayingCinemaId(character.CurrentlyPlayingCinemaId);
        if (cinemaId != 0)
            character.CurrentlyPlayingCinemaId = cinemaId;
        Logger.Warn("CompletedCinema cinema={0}", cinemaId);
        character.Quests.ApplyCinemaEndEffects(cinemaId);
        WorldManager.ResendVisibleObjectsToCharacter(character, clientDroppedVisibility: true);
        character.Events.OnCinemaEnded(character, new OnCinemaEndedArgs { CinemaId = cinemaId });
        if (character.CurrentlyPlayingCinemaId == cinemaId)
            character.CurrentlyPlayingCinemaId = 0;
    }
}
