using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.World;

public class WorldLevelInfoRulesTests
{
    [Test]
    public async Task UnlockRow_IsTheFirstGetQuestAtCap()
    {
        var row = WorldLevelInfoRules.SelectUnlockRow(CompactHardCaps(), playerLevelCap: 55);

        await Assert.That(row.MinServerDays).IsEqualTo(9);
        await Assert.That(row.HardCapLevel).IsEqualTo(55);
        await Assert.That(row.GetQuest).IsTrue();
    }

    [Test]
    public async Task ForCharacter_AtCap_UsesUnlockWorldLevelNotCaptureSlots()
    {
        var info = WorldLevelInfoRules.ForCharacter(55, 55, CompactHardCaps(), CompactModifiers());

        await Assert.That(info.WorldLevel).IsEqualTo(55u);
        await Assert.That(info.ServerDays).IsEqualTo(9u);
        await Assert.That(info.ExpModifier).IsEqualTo(10000u);
        await Assert.That(info.HardCapLevel).IsEqualTo(55u);
        await Assert.That(info.CharacterLevel).IsEqualTo(55u);
        await Assert.That(info.LevelDiff).IsEqualTo(0);
    }

    [Test]
    public async Task ForCharacter_BelowWorld_UsesTheUnderLevelExpBand()
    {
        var info = WorldLevelInfoRules.ForCharacter(30, 55, CompactHardCaps(), CompactModifiers());

        await Assert.That(info.WorldLevel).IsEqualTo(55u);
        await Assert.That(info.LevelDiff).IsEqualTo(-25);
        await Assert.That(info.ExpModifier).IsEqualTo(20000u);
    }

    [Test]
    public async Task ServerOpenUnixTime_AgesPastTheUnlockRow()
    {
        const long now = 1_778_000_000;
        var open = WorldLevelInfoRules.ServerOpenUnixTime(now, unlockMinServerDays: 9);

        await Assert.That(open).IsEqualTo(now - 10L * WorldLevelInfoRules.SecondsPerDay);
        await Assert.That(open).IsGreaterThan(0);
    }

    [Test]
    public async Task ServerOpenUnixTime_NeverReturnsZero()
    {
        var open = WorldLevelInfoRules.ServerOpenUnixTime(1, unlockMinServerDays: 9);

        await Assert.That(open).IsEqualTo(1L);
    }

    [Test]
    public async Task MissingGetQuestRow_FailsLoudly()
    {
        var lockedOnly = new[] { new WorldLevelHardCapRow(0, 1, 28, GetQuest: false) };

        await Assert.That(() => WorldLevelInfoRules.SelectUnlockRow(lockedOnly, 55))
            .Throws<InvalidOperationException>();
    }

    private static WorldLevelHardCapRow[] CompactHardCaps() =>
    [
        new(0, 1, 28, false),
        new(6, 6, 50, false),
        new(9, 9999, 55, true)
    ];

    private static WorldLevelExpModifierRow[] CompactModifiers() =>
    [
        new(-999, -10, 20000),
        new(-1, 1, 10000),
        new(10, 999, 5000)
    ];
}
