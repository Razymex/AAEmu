using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>
/// GM tool: resets a Dominion territory back to unclaimed so it can be claimed again. See
/// DominionManager.UnclaimTerritory for what is reset.
/// </summary>
public class UnclaimTerritory : ICommand
{
    public string[] CommandNames { get; set; } = ["unclaimterritory"];

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
        return "Releases a claimed Dominion territory (by zone GROUP id, e.g. 33 for Salpimari) back to unclaimed - "
             + "resets the lodestone House, removes its claim/guard-tower buffs, despawns its Territory Agent NPC, "
             + "and deletes the dominions row. The territory can then be re-claimed normally or via /claimterritory.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length < 1 || !ushort.TryParse(args[0], out var zoneGroupId))
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        // Guild claims live in GuildDominionManager; check both stores and release from the one that holds it.
        var existing = DominionManager.Instance.GetByZoneId(zoneGroupId);
        var isGuildOwned = existing == null;
        if (isGuildOwned)
            existing = GuildDominionManager.Instance.GetByZoneId(zoneGroupId);

        if (existing == null)
        {
            CommandManager.SendErrorText(this, messageOutput, $"Zone group {zoneGroupId} has no active Dominion claim.");
            return;
        }

        var previousOwner = existing.OwningFactionId != 0
            ? $"faction {(AAEmu.Game.Models.StaticValues.FactionsEnum)existing.OwningFactionId}"
            : $"Expedition {existing.ExpeditionId}";
        var unclaimed = isGuildOwned
            ? GuildDominionManager.Instance.UnclaimTerritory(zoneGroupId)
            : DominionManager.Instance.UnclaimTerritory(zoneGroupId);
        if (!unclaimed)
        {
            CommandManager.SendErrorText(this, messageOutput, $"Failed to unclaim zone group {zoneGroupId}.");
            return;
        }

        CommandManager.SendNormalText(this, messageOutput,
            $"Zone group {zoneGroupId} unclaimed (was owned by {previousOwner}).");
    }
}
