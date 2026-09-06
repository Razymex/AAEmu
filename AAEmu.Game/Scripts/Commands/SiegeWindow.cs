using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class SiegeWindow : ICommand
{
    public string[] CommandNames { get; set; } = ["siegewindow"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "[zoneGroupId]";
    }

    public string GetCommandHelpText()
    {
        return "Toggles a testing override that forces the Dominion declare window open for a zone group " +
               "(defaults to your current zone group), regardless of the real siege_plans/siege_zones schedule. " +
               "Not persisted - resets on World restart.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        uint zoneGroupId;
        if (args.Length > 0)
        {
            if (!uint.TryParse(args[0], out zoneGroupId))
            {
                CommandManager.SendErrorText(this, messageOutput, "Usage: /siegewindow [zoneGroupId]");
                return;
            }
        }
        else
        {
            var zone = ZoneManager.Instance.GetZoneByKey(character.Transform.ZoneId);
            if (zone == null)
            {
                CommandManager.SendErrorText(this, messageOutput, "Could not resolve your current zone group - specify one explicitly.");
                return;
            }
            zoneGroupId = zone.GroupId;
        }

        var nowForced = SiegeManager.Instance.ToggleDeclareWindowOverride(zoneGroupId);
        CommandManager.SendNormalText(this, messageOutput,
            nowForced
                ? $"Declare window FORCED OPEN for zone group {zoneGroupId}."
                : $"Declare window override cleared for zone group {zoneGroupId} - back to the real schedule.");
    }
}
