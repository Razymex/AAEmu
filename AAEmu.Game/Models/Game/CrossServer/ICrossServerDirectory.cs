namespace AAEmu.Game.Models.Game.CrossServer;

/// <summary>
/// Resolves which peer server a departure is aimed at.
/// <para>
/// The request does not carry a destination. Implementations read configured server metadata;
/// content without one unambiguous peer returns <see langword="null"/> and the departure is refused.
/// </para>
/// </summary>
public interface ICrossServerDirectory
{
    /// <summary>The peer server key for this server's id, or <see langword="null"/> when content offers no single peer.</summary>
    string ResolvePeerKey(byte ownServerId);
}
