namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// StartedCinema is an empty body. Bind only when permission / Progress
/// enter already named this cinema, or the event carries the act id.
/// An unnamed start is a different bubble and must not steal the quest cinema.
/// </summary>
public static class QuestCinemaBindRules
{
    public static bool ShouldBindStarted(uint eventCinemaId, uint actCinemaId, uint currentlyPlaying)
    {
        if (actCinemaId == 0)
            return false;
        if (eventCinemaId != 0 && eventCinemaId != actCinemaId)
            return false;
        if (eventCinemaId == 0 && currentlyPlaying != actCinemaId)
            return false;
        return true;
    }

    /// <summary>
    /// Talk / permission can start the cinema while the step is still None.
    /// Entering Progress must credit that already-playing cinema so Reward
    /// and the next accept happen during the film, before the teleport buff.
    /// </summary>
    public static bool ShouldCreditOnEnterProgress(uint currentlyPlaying, uint progressCinemaId) =>
        progressCinemaId != 0 && currentlyPlaying == progressCinemaId;

    /// <summary>
    /// Accepting the next quest walks a missing Progress step and must not
    /// replace the film already playing (that cinema still owns the teleport).
    /// </summary>
    public static bool ShouldReplacePlaying(uint currentlyPlaying, uint incoming) =>
        incoming != 0 && (currentlyPlaying == 0 || currentlyPlaying == incoming);

    /// <summary>
    /// CompletedCinema has an empty body. A skip before permission/start
    /// leaves the playing id at 0; the only safe name is a single deferred
    /// cinema on this character.
    /// </summary>
    public static uint ResolveCompletedCinema(uint currentlyPlaying, IReadOnlyList<uint> deferredCinemaIds)
    {
        if (currentlyPlaying != 0)
            return currentlyPlaying;
        if (deferredCinemaIds == null)
            return 0;

        uint found = 0;
        foreach (var cinemaId in deferredCinemaIds)
        {
            if (cinemaId == 0)
                continue;
            if (found == 0)
                found = cinemaId;
            else if (found != cinemaId)
                return 0;
        }

        return found;
    }
}
