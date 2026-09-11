using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSRequestPermissionToPlayCinemaForDirectingMode()
    : GamePacket(CSOffsets.CSRequestPermissionToPlayCinemaForDirectingMode, 1)
{
    public override void Read(PacketStream stream)
    {
        var packetId = stream.ReadUInt32();
        var npcObjId = stream.ReadBc();
        var doodadObjId = stream.ReadBc();

        var character = Connection.ActiveChar;
        if (character == null)
            return;

        var component = QuestManager.Instance.GetComponent(packetId);
        var questId = component?.ParentQuestTemplate?.Id ?? 0;
        var cinemaId = QuestCinemaRules.CinemaIdForPermission(component, null);
        var npc = npcObjId != 0 ? character.ParentWorld?.GetNpc(npcObjId) : null;
        var doodad = doodadObjId != 0 ? character.ParentWorld?.GetDoodad(doodadObjId) : null;
        var npcDistance = DistanceTo(character, npc);
        var doodadDistance = DistanceTo(character, doodad);
        var questActive = questId != 0 && character.Quests.HasQuest(questId);
        var allowed = QuestCinemaPermissionRules.CanBind(
            cinemaId,
            component != null,
            questActive,
            component?.KindId,
            QuestCinemaPermissionRules.SourceAllowed(npcObjId != 0, npc != null, npcDistance),
            QuestCinemaPermissionRules.SourceAllowed(doodadObjId != 0, doodad != null, doodadDistance));

        if (allowed)
            character.Quests.BindPlayingCinema(cinemaId);
        else
            cinemaId = 0;

        Logger.Warn(
            "CSRequestPermissionToPlayCinemaForDirectingMode id={0} quest={1} npc={2} doodad={3} cinema={4} allowed={5}",
            packetId, questId, npcObjId, doodadObjId, cinemaId, allowed);
    }

    private static float DistanceTo(Character character, BaseUnit source) =>
        source == null ? float.MaxValue : character.GetDistanceTo(source, true);
}
