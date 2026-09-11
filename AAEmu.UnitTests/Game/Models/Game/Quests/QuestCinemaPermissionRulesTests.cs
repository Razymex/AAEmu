using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestCinemaPermissionRulesTests
{
    [Test]
    public async Task Bind_NeedsComponentCinemaAndANearbySource()
    {
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            78, true, true, QuestComponentKind.Progress, npcAllowed: false, doodadAllowed: true)).IsTrue();
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            78, true, true, QuestComponentKind.Progress, npcAllowed: false, doodadAllowed: false)).IsFalse();
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            0, true, true, QuestComponentKind.Progress, npcAllowed: false, doodadAllowed: true)).IsFalse();
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            78, false, true, QuestComponentKind.Progress, npcAllowed: false, doodadAllowed: true)).IsFalse();
    }

    [Test]
    public async Task StartCinema_MayBindBeforeTheQuestIsActive()
    {
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            167, true, false, QuestComponentKind.Start, npcAllowed: true, doodadAllowed: false)).IsTrue();
        await Assert.That(QuestCinemaPermissionRules.CanBind(
            78, true, false, QuestComponentKind.Progress, npcAllowed: true, doodadAllowed: false)).IsFalse();
    }

    [Test]
    public async Task Source_MustExistInsideTheInteractBand()
    {
        await Assert.That(QuestCinemaPermissionRules.SourceAllowed(true, true, 2.5f)).IsTrue();
        await Assert.That(QuestCinemaPermissionRules.SourceAllowed(true, true, 3.1f)).IsFalse();
        await Assert.That(QuestCinemaPermissionRules.SourceAllowed(true, false, 0f)).IsFalse();
        await Assert.That(QuestCinemaPermissionRules.SourceAllowed(false, true, 0f)).IsFalse();
    }
}
