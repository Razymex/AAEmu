using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

/// <summary>
/// Deferred cinema-end effects. The client never reports the film ending once a session is
/// gone, so a graceful leave applies them and a dropped connection restores them on login.
/// </summary>
public class CharacterCinemaEndFlushTests
{
    private const uint QuestId = 5804;
    private const uint CinemaId = 228;
    private const uint ComponentId = 25087;

    private static QuestComponentTemplate BuildComponent() =>
        new(new QuestTemplate { Id = QuestId })
        {
            Id = ComponentId,
            CinemaId = CinemaId,
            // Zero buff/skill keeps the apply path a no-op; these tests are about the entry.
            BuffId = 0,
            SkillId = 0
        };

    private static CharacterQuests NewQuests() =>
        new(new Character(new UnitCustomModelParams()));

    private static CharacterQuests WithPendingCinema()
    {
        var quests = NewQuests();
        quests.EnqueueCinemaEndEffects(CinemaId, BuildComponent());
        return quests;
    }

    [Test]
    public async Task Flush_WithoutPendingEffects_IsANoOp()
    {
        var quests = NewQuests();

        quests.FlushPendingCinemaEndEffects();

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }

    [Test]
    public async Task Flush_ConsumesTheEntryForACompletedQuest()
    {
        var quests = WithPendingCinema();
        await Assert.That(quests.DeferredCinemaIds()).Count().IsEqualTo(1);

        quests.SetCompletedQuestFlag(QuestId, true);
        quests.FlushPendingCinemaEndEffects();

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }

    [Test]
    public async Task Flush_DropsTheEntryForAnAbandonedQuest()
    {
        var quests = WithPendingCinema();

        quests.FlushPendingCinemaEndEffects();

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }

    [Test]
    public async Task Restore_QueuesTheEntryUntilWorldEntry()
    {
        var quests = NewQuests();
        quests.SetCompletedQuestFlag(QuestId, true);

        quests.RestorePendingCinemaEndEffects(
            new[] { (QuestId, CinemaId, ComponentId) },
            _ => BuildComponent());

        // Queued, not applied: the buff packet needs the live connection world entry brings.
        await Assert.That(quests.DeferredCinemaIds()).Count().IsEqualTo(1);

        quests.FlushPendingCinemaEndEffects();

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }

    [Test]
    public async Task Restore_WithNoRows_IsANoOp()
    {
        var quests = NewQuests();

        quests.RestorePendingCinemaEndEffects([], _ => BuildComponent());

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }

    [Test]
    public async Task Restore_DropsARowWhoseComponentIsGone()
    {
        var quests = NewQuests();

        quests.RestorePendingCinemaEndEffects(
            new[] { (QuestId, CinemaId, 999_999u) },
            _ => null);

        await Assert.That(quests.DeferredCinemaIds()).IsEmpty();
    }
}
