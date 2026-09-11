using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncQuest : DoodadFuncTemplate
{
    // doodad_funcs
    public uint QuestKindId { get; set; }
    public uint QuestId { get; set; }

    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        Logger.Trace($"DoodadFuncQuest : skillId {skillId}, QuestKindId {QuestKindId}, QuestId {QuestId}");

        if (caster is Character character)
        {
            character.Quests.ObserveQuestDoodad(owner.ObjId, owner.TemplateId);

            if (character.Quests.ActiveQuests.TryGetValue(QuestId, out var quest)
                && quest.Template != null
                && DoodadQuestFuncRules.ShouldOfferComplete(
                    quest.GetQuestObjectiveStatus(),
                    quest.Template.LetItDone,
                    quest.Status,
                    quest.Step))
            {
                Logger.Info(
                    "DoodadFuncQuest complete-offer tpl={0} obj={1} quest={2}",
                    owner.TemplateId,
                    owner.ObjId,
                    QuestId);
                character.SendPacket(new SCDoodadCompleteQuestPacket(owner.ObjId, QuestId));
                return;
            }

            if (character.Quests.HasQuest(QuestId))
                return;

            var repeatable = QuestManager.Instance.GetTemplate(QuestId)?.Repeatable == true;
            if (!DoodadQuestFuncRules.ShouldOfferAccept(
                    QuestKindId,
                    hasQuest: false,
                    character.Quests.HasQuestCompleted(QuestId),
                    repeatable))
                return;

            Logger.Info(
                "DoodadFuncQuest offer tpl={0} obj={1} quest={2}",
                owner.TemplateId,
                owner.ObjId,
                QuestId);
            character.SendPacket(new SCDoodadQuestAcceptPacket(owner.ObjId, QuestId));
        }
    }
}
