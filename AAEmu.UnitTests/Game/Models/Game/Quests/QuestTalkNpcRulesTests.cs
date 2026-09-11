using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestTalkNpcRulesTests
{
    [Test]
    public async Task AcceptAndReport_AreTalkNpcs()
    {
        var dest = new HashSet<uint>();
        QuestTalkNpcRules.AddTalkNpcs(TemplateWith(
            start => start.ActTemplates.Add(new QuestActConAcceptNpc(start) { NpcId = 7816 }),
            ready => ready.ActTemplates.Add(new QuestActConReportNpc(ready) { NpcId = 7816 })), dest);

        await Assert.That(QuestTalkNpcRules.IsTalkNpc(dest, 7816)).IsTrue();
        await Assert.That(QuestTalkNpcRules.IsTalkNpc(dest, 1)).IsFalse();
    }

    [Test]
    public async Task HuntNpc_IsNotTalkNpc()
    {
        var dest = new HashSet<uint>();
        var template = new QuestTemplate { Id = 1 };
        var progress = new QuestComponentTemplate(template) { Id = 2, KindId = QuestComponentKind.Progress };
        progress.ActTemplates.Add(new QuestActObjMonsterHunt(progress) { NpcId = 4175 });
        template.Components[progress.Id] = progress;
        QuestTalkNpcRules.AddTalkNpcs(template, dest);

        await Assert.That(QuestTalkNpcRules.IsTalkNpc(dest, 4175)).IsFalse();
    }

    [Test]
    public async Task TalkObjective_IsTalkNpc()
    {
        var dest = new HashSet<uint>();
        var template = new QuestTemplate { Id = 1 };
        var progress = new QuestComponentTemplate(template) { Id = 2, KindId = QuestComponentKind.Progress };
        progress.ActTemplates.Add(new QuestActObjTalk(progress) { NpcId = 4220 });
        template.Components[progress.Id] = progress;
        QuestTalkNpcRules.AddTalkNpcs(template, dest);

        await Assert.That(QuestTalkNpcRules.IsTalkNpc(dest, 4220)).IsTrue();
    }

    private static QuestTemplate TemplateWith(
        Action<QuestComponentTemplate> startFill,
        Action<QuestComponentTemplate> readyFill)
    {
        var template = new QuestTemplate { Id = 2386 };
        var start = new QuestComponentTemplate(template) { Id = 10256, KindId = QuestComponentKind.Start };
        startFill(start);
        var ready = new QuestComponentTemplate(template) { Id = 10255, KindId = QuestComponentKind.Ready };
        readyFill(ready);
        template.Components[start.Id] = start;
        template.Components[ready.Id] = ready;
        return template;
    }
}
