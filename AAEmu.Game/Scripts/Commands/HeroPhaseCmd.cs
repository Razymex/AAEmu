using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.StaticValues;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>
/// GM: forces or releases the election's current phase. hero_schedules windows are weeks apart, so this is
/// how the abstain / ballot / count steps are reached without waiting.
/// </summary>
public class HeroPhaseCmd : ICommand
{
    public string[] CommandNames { get; set; } = ["herophase", "heroperiod"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "[ranking|abstain|voting|period|none|auto]";
    }

    public string GetCommandHelpText()
    {
        return
            "Show or force the hero season's phase.\n" +
            "  /herophase              - where the season stands, and its full schedule\n" +
            "  /herophase ranking      - leadership_ranking: leadership accrues toward candidacy/voting\n" +
            "  /herophase abstain      - hero_abstain: freezes/computes the candidate list, withdrawals open\n" +
            "  /herophase voting       - hero_voting: opens the ballot (CSHeroVotingPacket)\n" +
            "  /herophase period       - hero_period: counts ballots, seats the winner, pays out hero_rewards\n" +
            "  /herophase none         - between phases, as the gaps in hero_schedules genuinely are\n" +
            "  /herophase auto         - stop forcing and follow hero_schedules again\n" +
            "A forced phase sticks until cleared and is announced to everyone online at once. The client " +
            "resolves the phase against its own hero_schedules by season id, so it only agrees while the " +
            "forced phase's season exists in the client data.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length == 0)
        {
            CommandManager.SendNormalText(this, messageOutput, HeroManager.Instance.Describe());
            return;
        }

        var verb = args[0].ToLowerInvariant();

        if (verb is "auto" or "schedule" or "clear")
        {
            HeroManager.Instance.SetOverride(null);
            CommandManager.SendNormalText(this, messageOutput,
                "Following hero_schedules again.\n" + HeroManager.Instance.Describe());
            return;
        }

        var phase = verb switch
        {
            "ranking" or "leadership" or "leadership_ranking" or "1" => (HeroPhase?)HeroPhase.LeadershipRanking,
            "abstain" or "hero_abstain" or "2" => HeroPhase.HeroAbstain,
            "voting" or "vote" or "hero_voting" or "3" => HeroPhase.HeroVoting,
            "period" or "hero_period" or "serving" or "4" => HeroPhase.HeroPeriod,
            "none" or "off" or "0" => HeroPhase.None,
            _ => null
        };

        if (phase == null)
        {
            CommandManager.SendErrorText(this, messageOutput,
                $"'{args[0]}' is not a phase. Use ranking, abstain, voting, period, none or auto.");
            return;
        }

        HeroManager.Instance.SetOverride(phase.Value);
        CommandManager.SendNormalText(this, messageOutput, HeroManager.Instance.Describe());
    }
}
