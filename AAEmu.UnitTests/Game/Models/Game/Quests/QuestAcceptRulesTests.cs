using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestAcceptRulesTests
{
    [Test]
    public async Task StartAcceptNpc_FillsUnknownAcceptor()
    {
        var template = TemplateWithStartAct(start =>
            start.ActTemplates.Add(new QuestActConAcceptNpc(start) { NpcId = 7816 }));

        var type = QuestAcceptorType.Unknown;
        var id = 0u;
        QuestAcceptRules.FillUnknownAcceptor(template, ref type, ref id);

        await Assert.That(type).IsEqualTo(QuestAcceptorType.Npc);
        await Assert.That(id).IsEqualTo(7816u);
    }

    [Test]
    public async Task ExplicitAcceptor_IsLeftAlone()
    {
        var template = TemplateWithStartAct(start =>
            start.ActTemplates.Add(new QuestActConAcceptNpc(start) { NpcId = 7816 }));

        var type = QuestAcceptorType.Npc;
        var id = 99u;
        QuestAcceptRules.FillUnknownAcceptor(template, ref type, ref id);

        await Assert.That(type).IsEqualTo(QuestAcceptorType.Npc);
        await Assert.That(id).IsEqualTo(99u);
    }

    [Test]
    public async Task MissingStartAct_LeavesUnknown()
    {
        var template = new QuestTemplate { Id = 1 };
        var type = QuestAcceptorType.Unknown;
        var id = 0u;
        QuestAcceptRules.FillUnknownAcceptor(template, ref type, ref id);

        await Assert.That(type).IsEqualTo(QuestAcceptorType.Unknown);
        await Assert.That(id).IsEqualTo(0u);
    }

    private static QuestTemplate TemplateWithStartAct(Action<QuestComponentTemplate> fill)
    {
        var template = new QuestTemplate { Id = 2386 };
        var start = new QuestComponentTemplate(template)
        {
            Id = 10256,
            KindId = QuestComponentKind.Start
        };
        fill(start);
        template.Components[start.Id] = start;
        return template;
    }
}
