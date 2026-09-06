using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>GM tool: lifts a /disablecastle lock. See <see cref="DisableCastleZone"/>.</summary>
public class EnableCastleZone : ICommand
{
    public string[] CommandNames { get; set; } = ["enablecastle"];

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
        return "Removes a castle-system claim lock from the given zone GROUP id, previously set by /disablecastle.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length < 1 || !ushort.TryParse(args[0], out var zoneGroupId))
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        if (!DominionZoneLockManager.Instance.IsLocked(zoneGroupId))
        {
            CommandManager.SendErrorText(this, messageOutput, $"Zone group {zoneGroupId} is not locked.");
            return;
        }

        DominionZoneLockManager.Instance.Unlock(zoneGroupId);
        CommandManager.SendNormalText(this, messageOutput,
            $"Zone group {zoneGroupId} is unlocked - new castle-system claims are allowed again.");
    }
}
