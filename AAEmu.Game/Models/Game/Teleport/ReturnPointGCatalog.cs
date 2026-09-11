using System.Globalization;

using NLog;

namespace AAEmu.Game.Models.Game.Teleport;

/// <summary>
/// Reads <c>return_point.g</c> under configured ZoneGameDataRoot worlds. No fallback disk path.
/// </summary>
public static class ReturnPointGCatalog
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<ReturnPointFileRules.LocalPoint> LoadFromRoots(IEnumerable<string> roots)
    {
        var list = new List<ReturnPointFileRules.LocalPoint>();
        if (roots == null)
            return list;

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var worlds = Path.Combine(root, "worlds");
            if (!Directory.Exists(worlds))
                continue;

            foreach (var worldDir in Directory.EnumerateDirectories(worlds))
            {
                var zoneRoot = Path.Combine(worldDir, "level_design", "zone");
                if (!Directory.Exists(zoneRoot))
                    continue;

                foreach (var zoneDir in Directory.EnumerateDirectories(zoneRoot))
                {
                    var folder = Path.GetFileName(zoneDir);
                    if (!uint.TryParse(folder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoneKey) ||
                        zoneKey == 0)
                        continue;

                    var path = Path.Combine(zoneDir, "world_server", "return_point.g");
                    if (!File.Exists(path))
                        continue;

                    try
                    {
                        list.AddRange(ReturnPointFileRules.Parse(File.ReadAllText(path), zoneKey));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to read return_point.g {0}", path);
                    }
                }
            }
        }

        return list;
    }
}
