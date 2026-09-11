using AAEmu.Game.Models.Game.Teleport;

namespace AAEmu.UnitTests.Game.Models.Game.Teleport;

public class ReturnPointFileRulesTests
{
    [Test]
    public async Task Prefix_StripsReturnPoint()
    {
        await Assert.That(ReturnPointFileRules.TryEditorNameFromObject("ReturnPoint_sample", out var name)).IsTrue();
        await Assert.That(name).IsEqualTo("sample");
        await Assert.That(ReturnPointFileRules.TryEditorNameFromObject("returnpoint_sample", out name)).IsTrue();
        await Assert.That(name).IsEqualTo("sample");
        await Assert.That(ReturnPointFileRules.TryEditorNameFromObject("\"ReturnPoint_sample\"", out name)).IsTrue();
        await Assert.That(name).IsEqualTo("sample");
        await Assert.That(ReturnPointFileRules.TryEditorNameFromObject("ReturnPoint_", out _)).IsFalse();
        await Assert.That(ReturnPointFileRules.TryEditorNameFromObject("other_sample", out _)).IsFalse();
    }

    [Test]
    public async Task Parse_ReadsLocalPosAndZRot()
    {
        const string body = """
            object
                name ReturnPoint_sample
                pos ( x 1320.18, y 1580.35, z 109.345 )
                zRot 1.16937
                radius 3
            object
                name ReturnPoint_skip_me
                pos ( x 1, y 2, z 3 )
            """;

        var rows = ReturnPointFileRules.Parse(body, 133);
        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0].EditorName).IsEqualTo("sample");
        await Assert.That(rows[0].ZoneKey).IsEqualTo(133u);
        await Assert.That(rows[0].X).IsEqualTo(1320.18f);
        await Assert.That(rows[0].Y).IsEqualTo(1580.35f);
        await Assert.That(rows[0].Z).IsEqualTo(109.345f);
        await Assert.That(rows[0].ZRotRadians).IsEqualTo(1.16937f);
        await Assert.That(rows[1].EditorName).IsEqualTo("skip_me");
        await Assert.That(rows[1].ZRotRadians).IsEqualTo(0f);
    }

    [Test]
    public async Task Parse_SkipsUnknownNameAndZeroZone()
    {
        await Assert.That(ReturnPointFileRules.Parse("object\n    name NotAReturn\n    pos ( x 1, y 2, z 3 )\n", 10).Count)
            .IsEqualTo(0);
        await Assert.That(ReturnPointFileRules.Parse("object\n    name ReturnPoint_sample\n    pos ( x 1, y 2, z 3 )\n", 0).Count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Match_UsesEditorNameDictionary()
    {
        var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase) { ["sample"] = 9 };
        await Assert.That(ReturnPointFileRules.TryGetReturnPointId(map, "sample", out var id)).IsTrue();
        await Assert.That(id).IsEqualTo(9u);
        await Assert.That(ReturnPointFileRules.TryGetReturnPointId(map, "SAMPLE", out id)).IsTrue();
        await Assert.That(id).IsEqualTo(9u);
        await Assert.That(ReturnPointFileRules.TryGetReturnPointId(map, "missing", out _)).IsFalse();
        await Assert.That(ReturnPointFileRules.TryGetReturnPointId(null, "sample", out _)).IsFalse();
    }

    [Test]
    public async Task JsonOrRecall_WinsOverLevelFile()
    {
        await Assert.That(ReturnPointFileRules.ShouldUseLevelFile(false, false)).IsTrue();
        await Assert.That(ReturnPointFileRules.ShouldUseLevelFile(true, false)).IsFalse();
        await Assert.That(ReturnPointFileRules.ShouldUseLevelFile(false, true)).IsFalse();
        await Assert.That(ReturnPointFileRules.ShouldUseLevelFile(true, true)).IsFalse();
    }

    [Test]
    public async Task ToWorld_AddsOriginCells()
    {
        var (x, y, z) = ReturnPointFileRules.ToWorld(10, 10, 1320.18f, 1580.35f, 109.345f);
        await Assert.That(x).IsEqualTo(11560.18f);
        await Assert.That(y).IsEqualTo(11820.35f);
        await Assert.That(z).IsEqualTo(109.345f);
    }

    [Test]
    public async Task YawDegrees_FromZRotRadians()
    {
        var yaw = ReturnPointFileRules.YawDegreesFromZRot(MathF.PI);
        await Assert.That(MathF.Abs(yaw - 180f)).IsLessThan(0.01f);
    }

    [Test]
    public async Task Catalog_ReadsZoneFolderFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "aaemu-rp-" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, "worlds", "main_world", "level_design", "zone", "133", "world_server");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(
            Path.Combine(dir, "return_point.g"),
            """
            object
                name ReturnPoint_sample
                pos ( x 1, y 2, z 3 )
            """);
        try
        {
            var rows = ReturnPointGCatalog.LoadFromRoots([root]);
            await Assert.That(rows.Count).IsEqualTo(1);
            await Assert.That(rows[0].ZoneKey).IsEqualTo(133u);
            await Assert.That(rows[0].EditorName).IsEqualTo("sample");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
