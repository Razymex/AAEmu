using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Managers;

public interface IHeroManager : ILoadable
{
    /// <summary>Advances the election through its phases; idempotent, safe to run every minute.</summary>
    void Tick();

    /// <summary>Whether this character holds a seat in their nation's latest finalized election (backs the Hero unit requirement).</summary>
    bool IsCurrentHero(Character character);

    /// <summary>Character id of the highest-ranked seated Hero for this nation, or 0.</summary>
    uint TopSeatedCharacterId(uint nationFactionId);

    /// <summary>Whether this character is a standing candidate in the most recently computed cycle for their nation.</summary>
    bool IsCandidate(Character character);

    /// <summary>The character's hero_grades tier, or 0 when not seated. Drives the Hero board's grade range.</summary>
    int GradeOf(Character character);

    /// <summary>Pushes the Hero panel data to a character. <paramref name="showUi"/> opens the ballot and belongs only to the client's own list request.</summary>
    void SendHeroInfo(Character character, bool showUi = false);

    /// <summary>Same as <see cref="SendHeroInfo"/>, but the candidate/ranking/score lists are scoped to one faction.</summary>
    void SendHeroInfoForRequestedFaction(Character character, uint requestedFactionId, bool showUi = false);

    /// <summary>Records a ballot; the whole selection is accepted or rejected together.</summary>
    void Vote(GameConnection connection, IReadOnlyCollection<ulong> candidateCharacterIds);

    /// <summary>Withdraws the caller's own candidacy during HeroAbstain.</summary>
    void Abstain(GameConnection connection);

    /// <summary>Reverses the caller's own withdrawal during the same HeroAbstain phase.</summary>
    void DropoutComeback(GameConnection connection);
}
