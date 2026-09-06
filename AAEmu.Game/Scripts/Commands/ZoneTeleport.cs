using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>
/// GM: teleports to the approximate centre of a zone given its zone_key (zones.id accepted as a fallback).
/// Zones have no stored centre, so the region grid is scanned for the cells of that key and the centroid
/// is used.
/// </summary>
public class ZoneTeleport : ICommand
{
    public string[] CommandNames { get; set; } = ["zonetp", "ztp"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "<zoneKey>";
    }

    public string GetCommandHelpText()
    {
        return "Teleports you to the (approximate center of the) zone with the given zone_key. " +
               "Use the zone's zone_key/id as listed in the zones table (or /siege-related tooling), not a display name.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length != 1 || !uint.TryParse(args[0], out var zoneKey))
        {
            CommandManager.SendNormalText(this, messageOutput,
                $"Usage: {CommandManager.CommandPrefix}{CommandNames[0]} <zoneKey>");
            return;
        }

        if (character.Transform.InstanceId != WorldManager.DefaultInstanceId)
        {
            CommandManager.SendErrorText(this, messageOutput, "Zone teleports are not allowed inside an instance");
            return;
        }

        var zone = ZoneManager.Instance.GetZoneByKey(zoneKey) ?? ZoneManager.Instance.GetZoneById(zoneKey);
        if (zone == null)
        {
            CommandManager.SendErrorText(this, messageOutput, $"Unknown zone [{zoneKey}]");
            return;
        }

        var template = character.ParentWorld?.Template;
        if (template?.ZoneKeyByRegions == null)
        {
            CommandManager.SendErrorText(this, messageOutput, "No world grid loaded to resolve zone coordinates from");
            return;
        }

        long sumX = 0, sumY = 0;
        var hits = 0;
        var width = template.ZoneKeyByRegions.GetLength(0);
        var height = template.ZoneKeyByRegions.GetLength(1);
        for (var sx = 0; sx < width; sx++)
        {
            for (var sy = 0; sy < height; sy++)
            {
                if (template.ZoneKeyByRegions[sx, sy] != zone.ZoneKey)
                    continue;
                sumX += sx;
                sumY += sy;
                hits++;
            }
        }

        if (hits == 0)
        {
            CommandManager.SendErrorText(this, messageOutput,
                $"Zone [{zone.Name}] (key {zone.ZoneKey}) has no region cells in this world's grid - it may be instance-only or unused in this build");
            return;
        }

        var targetX = ((float)sumX / hits + 0.5f) * WorldManager.REGION_SIZE;
        var targetY = ((float)sumY / hits + 0.5f) * WorldManager.REGION_SIZE;
        var targetZ = WorldManager.Instance.GetHeight(zone.ZoneKey, targetX, targetY, 5000f);

        if (targetZ == 0f)
        {
            CommandManager.SendNormalText(this, messageOutput,
                $"|cFFFF0000Warning:|r target height came back |cFFFFFFFFzero|r for zone [{zone.Name}] (key {zone.ZoneKey}) - " +
                "the centroid may have landed out of bounds (odd-shaped zone). Teleporting anyway with Z=0; adjust with /move if needed.");
        }

        CommandManager.SendNormalText(this, messageOutput,
            $"Teleporting to zone |cFFFFFFFF{zone.Name}|r (key {zone.ZoneKey}, {hits} region cell(s)) at X:{targetX:0} Y:{targetY:0} Z:{targetZ:0}");
        character.ForceDismount();
        character.DisabledSetPosition = true;
        character.SendPacket(new SCTeleportUnitPacket(0, 0, targetX, targetY, targetZ, 0));
    }
}
