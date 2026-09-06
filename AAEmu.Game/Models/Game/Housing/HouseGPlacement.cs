using System.Globalization;

namespace AAEmu.Game.Models.Game.Housing;

/// <summary>
/// One row from <c>worlds/{world}/houses/{zoneKey}/house.g</c>. XY is stored as world metres
/// times <see cref="StoredXyScale"/>; Z and yaw are already metres / radians.
/// </summary>
public readonly record struct HouseGPlacement(
    uint Id,
    uint DesignId,
    uint ZoneKey,
    bool Removed,
    float X,
    float Y,
    float Z,
    float Yaw)
{
    /// <summary>house.g <c>posX</c>/<c>posY</c> multiplier. Z in the same file is metres.</summary>
    public const long StoredXyScale = 1L << 44;

    public static float WorldFromStoredXy(long stored) =>
        (float)(stored / (double)StoredXyScale);

    public static bool ShouldApplyToUnownedLodestone(bool isLodestone, uint ownerId, uint accountId, bool removed) =>
        isLodestone && ownerId == 0 && accountId == 0 && !removed;

    /// <summary>Parse one house.g body. <paramref name="zoneKey"/> is the folder name.</summary>
    public static List<HouseGPlacement> Parse(string text, uint zoneKey)
    {
        var list = new List<HouseGPlacement>();
        if (string.IsNullOrWhiteSpace(text) || zoneKey == 0)
            return list;

        uint id = 0, design = 0;
        var removed = false;
        long posX = 0, posY = 0;
        var posZ = 0f;
        var rotZ = 0f;
        var haveId = false;

        void Flush()
        {
            if (!haveId || design == 0)
                return;
            list.Add(new HouseGPlacement(
                id,
                design,
                zoneKey,
                removed,
                WorldFromStoredXy(posX),
                WorldFromStoredXy(posY),
                posZ,
                rotZ));
            haveId = false;
            id = 0;
            design = 0;
            removed = false;
            posX = 0;
            posY = 0;
            posZ = 0f;
            rotZ = 0f;
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;
            if (string.Equals(line, "house", StringComparison.Ordinal))
            {
                Flush();
                continue;
            }

            var parts = line.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;
            var key = parts[0];
            var val = parts[1].Trim();
            switch (key)
            {
                case "id" when uint.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId):
                    id = parsedId;
                    haveId = true;
                    break;
                case "design" when uint.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDesign):
                    design = parsedDesign;
                    break;
                case "removed":
                    removed = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1";
                    break;
                case "posX" when long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedX):
                    posX = parsedX;
                    break;
                case "posY" when long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedY):
                    posY = parsedY;
                    break;
                case "posZ" when float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedZ):
                    posZ = parsedZ;
                    break;
                case "rotZ" when float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRot):
                    rotZ = parsedRot;
                    break;
            }
        }

        Flush();
        return list;
    }
}
