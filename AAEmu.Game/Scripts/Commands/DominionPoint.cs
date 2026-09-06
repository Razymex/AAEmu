using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>GM view of, and trigger for, the Hero's Dominion Point distribution.</summary>
public class DominionPoint : ICommand
{
    public string[] CommandNames { get; set; } = ["dominionpoint", "dompoint"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "[give <zoneId>]";
    }

    public string GetCommandHelpText()
    {
        return "With no args, shows your current Dominion Point daily/weekly usage. 'give <zoneId>' performs "
             + "the same distribution as the client's button (you must be a seated Hero and the territory at "
             + "<zoneId> must belong to your nation).";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length == 0)
        {
            CommandManager.SendNormalText(this, messageOutput, Describe(character));
            return;
        }

        var verb = args[0].ToLowerInvariant();
        switch (verb)
        {
            case "give":
                if (args.Length < 2 || !ushort.TryParse(args[1], out var zoneId))
                {
                    CommandManager.SendErrorText(this, messageOutput, "Usage: /dominionpoint give <zoneId>");
                    return;
                }

                var result = HeroManager.Instance.GiveDominionPoint(character, zoneId);
                CommandManager.SendNormalText(this, messageOutput, $"GiveDominionPoint({zoneId}): {result}");
                break;
            default:
                CommandManager.SendErrorText(this, messageOutput, "Usage: /dominionpoint [give <zoneId>]");
                break;
        }
    }

    private static string Describe(Character character)
    {
        var (daily, dailyMax, weekly, weeklyMax, remainSec) = HeroManager.Instance.GetDominionPointCount(character);
        return $"Dominion Points: today {daily}/{dailyMax} (next give in {remainSec}s), " +
               $"this week {weekly}/{weeklyMax}.";
    }
}
