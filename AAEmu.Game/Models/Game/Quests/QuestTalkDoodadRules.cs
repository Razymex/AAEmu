using System.Globalization;

using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Doodad templates quests accept, report, or interact with, plus
/// <c>npctype://</c> companions that share the same level-pack pad.
/// Placements come from the level pack; this does not invent coordinates.
/// </summary>
public static class QuestTalkDoodadRules
{
    public const float SamePlacementMetres = 1f;

    /// <summary>
    /// 3901 extras sit ≤8 m from report 14226. The 17722 arrival square is ~300 m away.
    /// </summary>
    public const float SceneCompanionPadMetres = 12f;

    public const string NpcTypeModelPrefix = "npctype://";

    public readonly record struct Placement(uint TemplateId, float X, float Y, float Z, float YawDegrees);

    public readonly record struct Existing(uint TemplateId, float X, float Y, float Z);

    public static bool TryParseNpcTypeModel(string model, out uint npcId)
    {
        npcId = 0;
        if (string.IsNullOrEmpty(model) ||
            !model.StartsWith(NpcTypeModelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = model.AsSpan(NpcTypeModelPrefix.Length);
        return uint.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out npcId) &&
               npcId != 0;
    }

    public static bool IsOnScenePad(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return dx * dx + dy * dy + dz * dz <= SceneCompanionPadMetres * SceneCompanionPadMetres;
    }

    public static void AddTalkDoodads(IQuestTemplate template, ISet<uint> dest)
    {
        if (template?.Components == null || dest == null)
            return;

        foreach (var component in template.Components.Values)
        {
            if (component?.ActTemplates == null)
                continue;

            foreach (var act in component.ActTemplates)
            {
                switch (act)
                {
                    case QuestActConAcceptDoodad accept when accept.DoodadId != 0:
                        dest.Add(accept.DoodadId);
                        break;
                    case QuestActConReportDoodad report when report.DoodadId != 0:
                        dest.Add(report.DoodadId);
                        break;
                    case QuestActObjInteraction interact when interact.DoodadId != 0:
                        dest.Add(interact.DoodadId);
                        break;
                    case QuestActObjDoodadPhaseCheck phase when phase.DoodadId != 0:
                        dest.Add(phase.DoodadId);
                        break;
                }
            }
        }
    }

    public static void ExceptTowerAlmighty(ISet<uint> dest, IReadOnlySet<uint> towerAlmightyIds)
    {
        if (dest == null || towerAlmightyIds == null || towerAlmightyIds.Count == 0)
            return;

        dest.ExceptWith(towerAlmightyIds);
    }

    public static bool IsSamePlacement(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return dx * dx + dy * dy + dz * dz <= SamePlacementMetres * SamePlacementMetres;
    }

    /// <summary>
    /// Catalog rows to spawn. Skips a placement that already has that template within
    /// <see cref="SamePlacementMetres"/>. Wanted ids with no catalog row are not invented.
    /// </summary>
    public static IReadOnlyList<Placement> Plan(
        IReadOnlyCollection<uint> wanted,
        IReadOnlyList<Placement> catalog,
        IReadOnlyList<Existing> existing)
    {
        if (wanted == null || wanted.Count == 0 || catalog == null || catalog.Count == 0)
            return [];

        var wantedSet = wanted as ISet<uint> ?? wanted.ToHashSet();
        var live = existing ?? [];
        var planned = new List<Placement>();
        foreach (var place in catalog)
        {
            if (place.TemplateId == 0 || !wantedSet.Contains(place.TemplateId))
                continue;
            if (HasExisting(live, place))
                continue;
            planned.Add(place);
        }

        return planned;
    }

    /// <summary>
    /// <c>npctype://</c> catalog rows on the same pad as a talk doodad, excluding the
    /// talk templates themselves. Live F on 3901 uses doodad 14227 — no NPC 11966.
    /// </summary>
    public static IReadOnlyList<Placement> PlanCompanions(
        IReadOnlyCollection<uint> talkIds,
        IReadOnlyList<Placement> talkCatalog,
        IReadOnlyList<Placement> npcTypeCatalog,
        IReadOnlyList<Existing> existing)
    {
        if (talkIds == null || talkCatalog == null || npcTypeCatalog == null)
            return [];

        var talkSet = talkIds as ISet<uint> ?? talkIds.ToHashSet();
        if (talkSet.Count == 0)
            return [];

        List<Placement>? anchors = null;
        foreach (var place in talkCatalog)
        {
            if (place.TemplateId == 0 || !talkSet.Contains(place.TemplateId))
                continue;
            anchors ??= [];
            anchors.Add(place);
        }

        if (anchors == null || anchors.Count == 0)
            return [];

        var live = existing ?? [];
        var planned = new List<Placement>();
        foreach (var place in npcTypeCatalog)
        {
            if (place.TemplateId == 0 || talkSet.Contains(place.TemplateId))
                continue;
            if (!NearAnyAnchor(anchors, place))
                continue;
            if (HasExisting(live, place))
                continue;
            planned.Add(place);
        }

        return planned;
    }

    public static void AddCompanionTemplateIds(
        ISet<uint> dest,
        IReadOnlyCollection<uint> talkIds,
        IReadOnlyList<Placement> talkCatalog,
        IReadOnlyList<Placement> npcTypeCatalog)
    {
        if (dest == null)
            return;

        foreach (var place in PlanCompanions(talkIds, talkCatalog, npcTypeCatalog, []))
            dest.Add(place.TemplateId);
    }

    private static bool NearAnyAnchor(IReadOnlyList<Placement> anchors, Placement place)
    {
        foreach (var anchor in anchors)
        {
            if (IsOnScenePad(anchor.X, anchor.Y, anchor.Z, place.X, place.Y, place.Z))
                return true;
        }

        return false;
    }

    private static bool HasExisting(IReadOnlyList<Existing> existing, Placement place)
    {
        foreach (var row in existing)
        {
            if (row.TemplateId != place.TemplateId)
                continue;
            if (IsSamePlacement(row.X, row.Y, row.Z, place.X, place.Y, place.Z))
                return true;
        }

        return false;
    }
}
