using AAEmu.Commons.Utils;

namespace AAEmu.Game.Models.Game.Heroes;

/// <summary>
/// Client mailbox functions under <c>locale.mail</c>. Sender must start with '.' and match the
/// function name; title is the locale key <c>title</c>; period mails use
/// <c>body(msg, startUnix, endUnix)</c> so the UI can show abstain / activity dates.
/// </summary>
public static class HeroMailWire
{
    public const string CandidateSender = ".heroCandidateAlarm";
    public const string ElectionSender = ".heroElectionItem";
    public const string BonusSender = ".heroBonusItem";
    public const string MobilizationSender = ".mobilizationOrderItem";
    public const string LocaleTitle = "title";
    public const string LocaleBody = "body";

    /// <summary>
    /// <c>X2Time:TimeToDate</c> is typed as a string. Tax mail already quotes unix the same way.
    /// Bare numbers parse, then the read view throws and the window never opens.
    /// </summary>
    public static string FormatPeriodBody(string msg, DateTime periodStartUtc, DateTime periodEndUtc) =>
        $"body('{EscapeLuaSingleQuoted(msg)}', '{Helpers.UnixTime(periodStartUtc)}', '{Helpers.UnixTime(periodEndUtc)}')";

    public static string EscapeLuaSingleQuoted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal);
    }
}
