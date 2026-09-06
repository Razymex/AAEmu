using System.Globalization;

using NLog;

namespace AAEmu.Game.Models.Game.Housing;

/// <summary>
/// Authored house placements under <c>worlds/*/houses/{zoneKey}/house.g</c> on ZoneGameDataRoot.
/// </summary>
public static class HouseGPlacementCatalog
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<HouseGPlacement> LoadFromRoots(IEnumerable<string> roots)
    {
        var list = new List<HouseGPlacement>();
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
                var housesDir = Path.Combine(worldDir, "houses");
                if (!Directory.Exists(housesDir))
                    continue;

                foreach (var zoneDir in Directory.EnumerateDirectories(housesDir))
                {
                    var folder = Path.GetFileName(zoneDir);
                    if (!uint.TryParse(folder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoneKey) ||
                        zoneKey == 0)
                        continue;
                    var path = Path.Combine(zoneDir, "house.g");
                    if (!File.Exists(path))
                        continue;

                    try
                    {
                        list.AddRange(HouseGPlacement.Parse(File.ReadAllText(path), zoneKey));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to read house.g {0}", path);
                    }
                }
            }
        }

        return list;
    }

    public static Dictionary<uint, HouseGPlacement> IndexLiveByDesign(IEnumerable<HouseGPlacement> placements)
    {
        var map = new Dictionary<uint, HouseGPlacement>();
        if (placements == null)
            return map;

        foreach (var row in placements)
        {
            if (row.Removed || row.DesignId == 0)
                continue;
            map[row.DesignId] = row;
        }

        return map;
    }
}
