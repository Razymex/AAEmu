using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestCinemaRulesTests
{
    [Test]
    public async Task Permission_UsesTheComponentCinemaNotAQuestLookup()
    {
        var template = Template(startCinema: 0, readyCinema: 78);
        var ready = template.GetComponents(QuestComponentKind.Ready)[0];

        await Assert.That(QuestCinemaRules.CinemaIdForPermission(ready, null)).IsEqualTo(78u);
        await Assert.That(QuestCinemaRules.CinemaIdForPermission(null, template, QuestComponentKind.Ready))
            .IsEqualTo(78u);
        await Assert.That(QuestCinemaRules.CinemaIdForPermission(null, null)).IsEqualTo(0u);
        await Assert.That(QuestCinemaRules.CinemaIdOnComponent(ready)).IsEqualTo(78u);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(null)).IsEqualTo(0u);
    }

    [Test]
    public async Task PreferStep_Wins()
    {
        var template = Template(startCinema: 124, readyCinema: 55);

        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template, QuestComponentKind.Ready))
            .IsEqualTo(55u);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template, QuestComponentKind.Start))
            .IsEqualTo(124u);
    }

    [Test]
    public async Task NoPrefer_UsesReadyThenStart()
    {
        var template = Template(startCinema: 124, readyCinema: 55);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template)).IsEqualTo(55u);
    }

    [Test]
    public async Task MissingTemplate_IsZero()
    {
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(null)).IsEqualTo(0u);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(new QuestTemplate { Id = 1 })).IsEqualTo(0u);
    }

    [Test]
    public async Task ProgressCinemaAct_IsUsedWhenComponentColumnIsEmpty()
    {
        var template = new QuestTemplate { Id = 2 };
        var progress = new QuestComponentTemplate(template)
        {
            Id = 20,
            KindId = QuestComponentKind.Progress
        };
        progress.ActTemplates.Add(new QuestActObjCinema(progress) { CinemaId = 40 });
        template.Components[progress.Id] = progress;

        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template)).IsEqualTo(40u);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template, QuestComponentKind.Progress))
            .IsEqualTo(40u);
        await Assert.That(QuestCinemaRules.FirstCinemaComponentId(template, QuestComponentKind.Progress))
            .IsEqualTo(20u);
    }

    [Test]
    public async Task ProgressStep_DoesNotFallThroughToReadyCinema()
    {
        var template = Template(startCinema: 0, readyCinema: 78);

        await Assert.That(QuestCinemaRules.FirstCinema(template, QuestComponentKind.Progress))
            .IsEqualTo(0u);
        await Assert.That(QuestCinemaRules.CinemaIdForDirecting(template, QuestComponentKind.Progress))
            .IsEqualTo(78u);
    }

    private static QuestTemplate Template(uint startCinema, uint readyCinema)
    {
        var template = new QuestTemplate { Id = 2385 };
        var start = new QuestComponentTemplate(template)
        {
            Id = 10254,
            KindId = QuestComponentKind.Start,
            CinemaId = startCinema
        };
        var ready = new QuestComponentTemplate(template)
        {
            Id = 10255,
            KindId = QuestComponentKind.Ready,
            CinemaId = readyCinema
        };
        template.Components[start.Id] = start;
        template.Components[ready.Id] = ready;
        return template;
    }
}
