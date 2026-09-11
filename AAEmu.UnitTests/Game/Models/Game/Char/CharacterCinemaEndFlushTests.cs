using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

/// <summary>
/// Leave-world flush of the deferred cinema-end effects. The client never reports
/// the cinema end once the session is gone, and the quest step is already saved, so
/// the pending entry must be applied rather than dropped with the in-memory list.
/// </summary>
public class CharacterCinemaEndFlushTests
{
    private const uint QuestId = 5804;
    private const uint CinemaId = 228;

    private static CharacterQuests WithPendingCinema()
    {
        var character = new Character(new UnitCustomModelParams());
        var quests = new CharacterQuests(character);
        var component = new QuestComponentTemplate(new QuestTemplate { Id = QuestId })
        {
            Id = 25087,
            CinemaId = CinemaId,
            // Zero buff/skill keeps the apply path a no-op; this test is about the entry.
            BuffId = 0,
            SkillId = 0
        };

        quests.EnqueueCinemaEndEffects(CinemaId, component);
        return quests;
    }

    [Test]
    public async Task Flush_WithoutPendingEffects_IsANoOp()
    {
        var character = new Character(new UnitCustomModelParams());
        var quests = new CharacterQuests(character);

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
}
