using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;

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
        var component = QuestManager.Instance.GetComponent(packetId);
        var questId = component?.ParentQuestTemplate?.Id ?? packetId;
        QuestComponentKind? preferStep = component?.KindId;
        if (preferStep == null && character?.Quests.ActiveQuests.TryGetValue(questId, out var quest) == true)
            preferStep = quest.Step;
        var cinemaId = QuestCinemaRules.CinemaIdForPermission(
            component,
            QuestManager.Instance.GetTemplate(questId),
            preferStep);
        if (character != null && cinemaId != 0)
            character.Quests.BindPlayingCinema(cinemaId);

        Logger.Warn(
            "CSRequestPermissionToPlayCinemaForDirectingMode id={0} quest={1} npc={2} doodad={3} cinema={4}",
            packetId, questId, npcObjId, doodadObjId, cinemaId);
    }
}
