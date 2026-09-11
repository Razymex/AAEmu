using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestTalkDoodadRulesTests
{
    [Test]
    public async Task AcceptAndReport_AreTalkDoodads()
    {
        var dest = new HashSet<uint>();
        var template = new QuestTemplate { Id = 2388 };
        var start = new QuestComponentTemplate(template) { Id = 10265, KindId = QuestComponentKind.Start };
        start.ActTemplates.Add(new QuestActConAcceptDoodad(start) { DoodadId = 14178 });
        var ready = new QuestComponentTemplate(template) { Id = 10268, KindId = QuestComponentKind.Ready };
        ready.ActTemplates.Add(new QuestActConReportDoodad(ready) { DoodadId = 14178 });
        template.Components[start.Id] = start;
        template.Components[ready.Id] = ready;

        QuestTalkDoodadRules.AddTalkDoodads(template, dest);

        await Assert.That(dest.Contains(14178)).IsTrue();
    }

    [Test]
    public async Task HighlightOnly_IsNotTalkDoodad()
    {
        var dest = new HashSet<uint>();
        var template = new QuestTemplate { Id = 1 };
        var progress = new QuestComponentTemplate(template) { Id = 2, KindId = QuestComponentKind.Progress };
        progress.ActTemplates.Add(new QuestActObjMonsterHunt(progress) { NpcId = 4175, HighlightDoodadId = 999 });
        template.Components[progress.Id] = progress;

        QuestTalkDoodadRules.AddTalkDoodads(template, dest);

        await Assert.That(dest.Contains(999)).IsFalse();
    }

    [Test]
    public async Task ExceptTowerAlmighty_RemovesEventTargets()
    {
        var dest = new HashSet<uint> { 14178, 8410 };
        QuestTalkDoodadRules.ExceptTowerAlmighty(dest, new HashSet<uint> { 8410 });

        await Assert.That(dest.Contains(14178)).IsTrue();
        await Assert.That(dest.Contains(8410)).IsFalse();
    }

    [Test]
    public async Task Plan_SkipsJsonDuplicate_DoesNotInventMissing()
    {
        var wanted = new HashSet<uint> { 14178, 14177 };
        var catalog = new[]
        {
            new QuestTalkDoodadRules.Placement(14178, 10267.32f, 15267.89f, 239.54f, 90f)
        };
        var existing = new[]
        {
            new QuestTalkDoodadRules.Existing(14178, 10267.32f, 15267.89f, 239.54f)
        };

        var planned = QuestTalkDoodadRules.Plan(wanted, catalog, existing);

        await Assert.That(planned).IsEmpty();
    }

    [Test]
    public async Task Plan_SpawnsCatalogRowWhenWorldEmpty()
    {
        var wanted = new HashSet<uint> { 14178 };
        var catalog = new[]
        {
            new QuestTalkDoodadRules.Placement(14178, 10267.32f, 15267.89f, 239.54f, 45f)
        };

        var planned = QuestTalkDoodadRules.Plan(wanted, catalog, []);

        await Assert.That(planned.Count).IsEqualTo(1);
        await Assert.That(planned[0].TemplateId).IsEqualTo(14178u);
        await Assert.That(planned[0].X).IsEqualTo(10267.32f);
        await Assert.That(planned[0].YawDegrees).IsEqualTo(45f);
    }

    [Test]
    public async Task TryParseNpcTypeModel_ReadsNpcId()
    {
        await Assert.That(QuestTalkDoodadRules.TryParseNpcTypeModel("npctype://11966", out var feos)).IsTrue();
        await Assert.That(feos).IsEqualTo(11966u);
        await Assert.That(QuestTalkDoodadRules.TryParseNpcTypeModel("cgf://x", out _)).IsFalse();
        await Assert.That(QuestTalkDoodadRules.TryParseNpcTypeModel("npctype://0", out _)).IsFalse();
    }

    [Test]
    public async Task PlanCompanions_TakesPadNpcTypeRows_SkipsFarAndTalkSelf()
    {
        var talkIds = new HashSet<uint> { 14226 };
        var talk = new[]
        {
            new QuestTalkDoodadRules.Placement(14226, 11546.626f, 11829.929f, 110.011f, 0f)
        };
        var npcType = new[]
        {
            new QuestTalkDoodadRules.Placement(14226, 11546.626f, 11829.929f, 110.011f, 0f),
            new QuestTalkDoodadRules.Placement(14227, 11544.631f, 11827.183f, 110.172f, 0f),
            new QuestTalkDoodadRules.Placement(14228, 11538.821f, 11828.881f, 110.356f, 0f),
            new QuestTalkDoodadRules.Placement(14228, 11547.599f, 11823.275f, 110.101f, 0f),
            new QuestTalkDoodadRules.Placement(14178, 11468.733f, 11550.127f, 124.619f, 0f)
        };

        var planned = QuestTalkDoodadRules.PlanCompanions(talkIds, talk, npcType, []);

        await Assert.That(planned.Any(p => p.TemplateId == 14226)).IsFalse();
        await Assert.That(planned.Count(p => p.TemplateId == 14227)).IsEqualTo(1);
        await Assert.That(planned.Count(p => p.TemplateId == 14228)).IsEqualTo(2);
        await Assert.That(planned.Any(p => p.TemplateId == 14178)).IsFalse();
    }

    [Test]
    public async Task PlanCompanions_SkipsExisting_DoesNotInvent()
    {
        var talkIds = new HashSet<uint> { 14226 };
        var talk = new[]
        {
            new QuestTalkDoodadRules.Placement(14226, 11546.626f, 11829.929f, 110.011f, 0f)
        };
        var npcType = new[]
        {
            new QuestTalkDoodadRules.Placement(14227, 11544.631f, 11827.183f, 110.172f, 0f)
        };
        var existing = new[]
        {
            new QuestTalkDoodadRules.Existing(14227, 11544.631f, 11827.183f, 110.172f)
        };

        var planned = QuestTalkDoodadRules.PlanCompanions(talkIds, talk, npcType, existing);

        await Assert.That(planned).IsEmpty();
        await Assert.That(
            QuestTalkDoodadRules.PlanCompanions(talkIds, talk, [], [])).IsEmpty();
    }
}
