using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestNoneSceneRulesTests
{
    [Test]
    public async Task OrbWithProgressCinema_StartsTheScene()
    {
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneCinema(true, 167)).IsTrue();
    }

    [Test]
    public async Task Accept_StartsWhenNonePlayCinemaAndProgressCinemaExist()
    {
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneOnAccept(SceneTemplate())).IsTrue();
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneOnAccept(new QuestTemplate { Id = 1 })).IsFalse();
    }

    [Test]
    public async Task NoProgressCinema_DoesNotBind()
    {
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneCinema(true, 0)).IsFalse();
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneCinema(false, 167)).IsFalse();
        await Assert.That(QuestNoneSceneRules.ShouldStartSceneCinema(false, 0)).IsFalse();
    }

    [Test]
    public async Task SceneReportTalk_IsNoneWithProgressCinema()
    {
        await Assert.That(QuestNoneSceneRules.IsSceneReportTalk(QuestComponentKind.None, true, 167)).IsTrue();
        await Assert.That(QuestNoneSceneRules.IsSceneReportTalk(QuestComponentKind.None, true, 0)).IsFalse();
        await Assert.That(QuestNoneSceneRules.IsSceneReportTalk(QuestComponentKind.Ready, true, 167)).IsFalse();
    }

    private static QuestTemplate SceneTemplate()
    {
        var template = new QuestTemplate { Id = 3900 };
        var none = new QuestComponentTemplate(template)
        {
            Id = 18603,
            KindId = QuestComponentKind.None,
            PlayCinemaBeforeBubble = true
        };
        var progress = new QuestComponentTemplate(template)
        {
            Id = 32016,
            KindId = QuestComponentKind.Progress
        };
        progress.ActTemplates.Add(new QuestActObjCinema(progress) { CinemaId = 167 });
        template.Components[none.Id] = none;
        template.Components[progress.Id] = progress;
        return template;
    }
}
