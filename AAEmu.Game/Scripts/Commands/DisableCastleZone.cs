using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>
/// GM tool: blocks new castle-system claims in a zone group. Existing claims in that zone keep working;
/// only a fresh /claimterritory or in-game declare is refused. See IDominionZoneLockManager. Persists in
/// the dominion_locked_zones table.
/// </summary>
public class DisableCastleZone : ICommand
{
    public string[] CommandNames { get; set; } = ["disablecastle"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "<zoneGroupId>";
    }

    public string GetCommandHelpText()
    {
        return "Blocks new castle-system claims in the given zone GROUP id (e.g. 33 for Salpimari, 54 for "
             + "Exeloch). Does not affect any claim already there - use /unclaimterritory separately if you "
             + "also want to remove an existing one. Persists across restarts.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length < 1 || !ushort.TryParse(args[0], out var zoneGroupId))
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        DominionZoneLockManager.Instance.Lock(zoneGroupId);
        CommandManager.SendNormalText(this, messageOutput,
            $"Zone group {zoneGroupId} is now locked - no new castle-system claims will be accepted there.");
    }
}
