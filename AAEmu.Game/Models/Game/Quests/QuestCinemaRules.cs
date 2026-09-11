using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Cinema id the client already owns on a Start/Ready/Progress component
/// (column or cinema act). Directing permission records it so
/// Started/Completed cinema events match.
/// </summary>
public static class QuestCinemaRules
{
    /// <summary>
    /// Directing permission sends a component id, not a quest id.
    /// Use that component's cinema when present.
    /// </summary>
    public static uint CinemaIdForPermission(
        QuestComponentTemplate component,
        IQuestTemplate template,
        QuestComponentKind? preferStep = null)
    {
        var fromComponent = CinemaIdOnComponent(component);
        if (fromComponent != 0)
            return fromComponent;
        return CinemaIdForDirecting(template, preferStep);
    }

    public static uint CinemaIdOnComponent(QuestComponentTemplate component)
    {
        if (component == null)
            return 0;
        if (component.CinemaId != 0)
            return component.CinemaId;
        if (component.ActTemplates == null)
            return 0;
        foreach (var act in component.ActTemplates)
        {
            if (act is QuestActObjCinema cinema && cinema.CinemaId != 0)
                return cinema.CinemaId;
        }

        return 0;
    }

    public static uint CinemaIdForDirecting(IQuestTemplate template, QuestComponentKind? preferStep = null)
    {
        if (template == null)
            return 0;

        if (preferStep is { } step)
        {
            var preferred = FirstCinema(template, step);
            if (preferred != 0)
                return preferred;
        }

        foreach (var kind in new[]
                 {
                     QuestComponentKind.Ready,
                     QuestComponentKind.Start,
                     QuestComponentKind.Progress
                 })
        {
            var cinemaId = FirstCinema(template, kind);
            if (cinemaId != 0)
                return cinemaId;
        }

        return 0;
    }

    public static uint FirstCinema(IQuestTemplate template, QuestComponentKind kind) =>
        FirstCinemaHit(template, kind).CinemaId;

    public static uint FirstCinemaComponentId(IQuestTemplate template, QuestComponentKind kind) =>
        FirstCinemaHit(template, kind).ComponentId;

    private static CinemaHit FirstCinemaHit(IQuestTemplate template, QuestComponentKind kind)
    {
        var components = template?.GetComponents(kind);
        if (components == null)
            return default;

        foreach (var component in components)
        {
            if (component == null)
                continue;
            if (component.CinemaId != 0)
                return new CinemaHit(component.Id, component.CinemaId);
            if (component.ActTemplates == null)
                continue;
            foreach (var act in component.ActTemplates)
            {
                if (act is QuestActObjCinema cinema && cinema.CinemaId != 0)
                    return new CinemaHit(component.Id, cinema.CinemaId);
            }
        }

        return default;
    }

    private readonly record struct CinemaHit(uint ComponentId, uint CinemaId);
}
