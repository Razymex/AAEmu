using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Mails;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class TestHouse : ICommand
{
    /// <summary>
    /// Per-GM "active house" for the /house subcommands. Targeting a house physically is fragile:
    /// /house_binding_move retargets the character onto a doodad, and /doodad remove can retarget
    /// anything, after which every /house subcommand fails its "is House" check. The active house
    /// is kept per character so the subcommands stay usable regardless of the current target.
    /// </summary>
    private static readonly Dictionary<uint, uint> ActiveHouseByCharacter = [];

    public string[] CommandNames { get; set; } = ["house", "test_house", "testhouse"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "[command] [options]";
    }

    public string GetCommandHelpText()
    {
        return "Available commands;\n" +
               "|cFFFFFFFFfind <tl:N|id:N|obj:N|owner:Name|account:N|name:Text>|r -> Finds house(s); a single match becomes the active house\n" +
               "|cFFFFFFFFclear|r -> Clears the active house\n" +
               "|cFFFFFFFFinfo|r -> Shows various house info\n" +
               "|cFFFFFFFFdoodads [--persistent] [--dupes]|r -> Lists runtime bound doodads (and optionally the DB rows) for the house\n" +
               "|cFFFFFFFFdedupe [confirm]|r -> Removes duplicate bound doodads; dry-run unless 'confirm' is given\n" +
               "|cFFFFFFFFtaxmail|r -> Creates a tax due mail for the house owner\n" +
               "|cFFFFFFFFsetforsale <money> [buyer]|r -> Forces a house for sale to be set, with optional [buyer] specified. Specify 0 money to clear the sale. (does not return any certificates)\n" +
               "|cFFFFFFFFsettaxpaid|r -> Sets the house's taxdue date as if just build and paid for.\n" +
               "|cFFFFFFFFsettaxdue|r -> Sets the house's taxdue date a week from now.\n" +
               "|cFFFFFFFFsettaxoverdue|r -> Sets the house's taxdue date to now (alias: settaxduesoon).\n" +
               "|cFFFFFFFFsetdemosoon|r -> Sets the house's demolition date to 20 seconds from now.\n" +
               "|cFFFFFFFFforcedemo|r -> Forcefully demolishes a house right now, but returns all furniture regardless if it is normally returned or not.\n" +
               "";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length <= 0)
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        var a0 = args[0].ToLower();

        // find/clear work without a house selected
        if (a0 == "find")
        {
            if (args.Length < 2)
            {
                CommandManager.SendDefaultHelpText(this, messageOutput);
                return;
            }

            FindHouses(character, args[1], messageOutput);
            return;
        }

        if (a0 == "clear")
        {
            ActiveHouseByCharacter.Remove(character.Id);
            CommandManager.SendNormalText(this, messageOutput, "Active house cleared.");
            return;
        }

        var house = GetActiveHouse(character) ?? character.CurrentTarget as House;
        if (house == null)
        {
            CommandManager.SendErrorText(this, messageOutput,
                "No active house - use /house find <selector>, or target a house");
            return;
        }

        try
        {
            if (a0 == "info")
            {
                CommandManager.SendNormalText(this, messageOutput,
                    $"ObjId: {house.ObjId} - TlId: {house.TlId} - HouseId: {house.Id} - TemplateId: {house.TemplateId} - ModelId: {house.ModelId} - {house.Name}");
                CommandManager.SendNormalText(this, messageOutput,
                    $"Owner: {house.OwnerId} (account {house.AccountId}) - CurrentStep: {house.CurrentStep} - Action: {house.CurrentAction}/{house.AllAction} - Runtime doodads: {house.AttachedDoodads?.Count ?? 0}");

                HousingManager.Instance.CalculateBuildingTaxInfo(house.AccountId, house.Template, false,
                    out var totalTaxAmountDue, out var heavyTaxHouseCount, out var normalTaxHouseCount,
                    out var hostileTaxRate, out _);

                if (DateTime.UtcNow >= house.TaxDueDate)
                {
                    CommandManager.SendNormalText(this, messageOutput,
                        $"Tax Due: {totalTaxAmountDue}{(house.Template.HeavyTax ? "" : "(no heavy tax)")} by {house.TaxDueDate}");
                }
                else if (DateTime.UtcNow >= house.TaxDueDate.AddDays(7))
                {
                    CommandManager.SendNormalText(this, messageOutput,
                        $"Tax Overdue: {totalTaxAmountDue * 2}{(house.Template.HeavyTax ? "" : "(no heavy tax)")}, demolition at {house.ProtectionEndDate}");
                }
            }
            else if (a0 == "doodads")
            {
                DumpDoodads(house, args, messageOutput);
            }
            else if (a0 == "dedupe")
            {
                DedupeDoodads(house, args, messageOutput);
            }
            else if (a0 == "taxmail")
            {
                var newMail = new MailForTax(house);
                newMail.FinalizeMail();
                newMail.Send();
                CommandManager.SendNormalText(this, messageOutput, $"Created tax mail for {house.Name}");
            }
            else if (a0 == "settaxpaid")
            {
                house.ProtectionEndDate = DateTime.UtcNow.AddDays(21);
                HousingManager.Instance.UpdateTaxInfo(house);
                CommandManager.SendNormalText(this, messageOutput, $"Set {house.Name} as tax paid");
            }
            else if (a0 == "settaxdue")
            {
                house.ProtectionEndDate = DateTime.UtcNow.AddDays(14);
                HousingManager.Instance.UpdateTaxInfo(house);
                CommandManager.SendNormalText(this, messageOutput, $"Set {house.Name} as tax due");
            }
            else if (a0 is "settaxoverdue" or "settaxduesoon")
            {
                house.ProtectionEndDate = DateTime.UtcNow.AddDays(7);
                HousingManager.Instance.UpdateTaxInfo(house);
                CommandManager.SendNormalText(this, messageOutput, $"Set {house.Name} as tax overdue");
            }
            else if (a0 == "setdemosoon")
            {
                house.ProtectionEndDate = DateTime.UtcNow.AddSeconds(20);
                HousingManager.Instance.UpdateTaxInfo(house);
                CommandManager.SendNormalText(this, messageOutput,
                    $"Set {house.Name} to demolished in about 20 seconds");
            }
            else if (a0 == "setforsale")
            {
                var price = 0u;
                var buyer = string.Empty;
                var buyerId = 0u;

                if (args.Length > 1)
                {
                    if (uint.TryParse(args[1], out var priceVal))
                    {
                        price = priceVal;
                    }
                    else
                    {
                        CommandManager.SendErrorText(this, messageOutput, $"Parse error, price");
                        return;
                    }
                }

                if (args.Length > 2)
                {
                    buyer = args[2];
                    buyerId = NameManager.Instance.GetCharacterId(buyer);
                    if (buyerId > 0)
                    {
                        buyer = NameManager.Instance.GetCharacterName(buyerId);
                    }
                    else
                    {
                        CommandManager.SendErrorText(this, messageOutput, $"Invalid buyer name {args[2]}");
                        return;
                    }
                }

                if (buyerId <= 0)
                {
                    buyer = "anyone";
                }

                if (price > 0)
                {
                    // Set for sale
                    if (house.SellPrice > 0)
                    {
                        CommandManager.SendErrorText(this, messageOutput,
                            $"Is already for sale, clear first to assign a new value");
                        return;
                    }

                    if (HousingManager.Instance.SetForSale(house, price, buyerId, null, true))
                    {
                        CommandManager.SendNormalText(this, messageOutput,
                            $"Setting {house.Name} for sale with a price of {price} to buy for {buyer}.");
                    }
                    else
                    {
                        CommandManager.SendErrorText(this, messageOutput, $"Failed to set {house.Name} for sale !");
                    }
                }
                else
                {
                    if (house.SellPrice <= 0)
                    {
                        CommandManager.SendErrorText(this, messageOutput, $"This house wasn't for sale.");
                        return;
                    }

                    // Remove sale (for GM commands we don't return certificates)
                    if (HousingManager.Instance.CancelForSale(house, false))
                    {
                        CommandManager.SendNormalText(this, messageOutput, $"{house.Name} is no longer for sale");
                    }
                    else
                    {
                        CommandManager.SendErrorText(this, messageOutput, $"Failed to remove sale from {house.Name} !");
                    }
                }
            }
            else if (a0 == "forcedemo")
            {
                HousingManager.Instance.Demolish(null, house, false, true);
                CommandManager.SendNormalText(this, messageOutput, $"{house.Name} demolished with full restore");
            }
            else
            {
                CommandManager.SendErrorText(this, messageOutput, $"Unknown command: {a0}");
            }
        }
        catch (Exception e)
        {
            CommandManager.SendErrorText(this, messageOutput, $"Exception: {e.Message}");
        }
    }

    private static House GetActiveHouse(Character character)
    {
        if (!ActiveHouseByCharacter.TryGetValue(character.Id, out var houseId))
            return null;

        var house = HousingManager.Instance.GetHouseById(houseId);
        if (house != null)
            return house;

        ActiveHouseByCharacter.Remove(character.Id);
        return null;
    }

    /// <summary>
    /// House lookup by id/name/owner/account so commands don't require physically targeting the house.
    /// </summary>
    private void FindHouses(Character character, string selector, IMessageOutput messageOutput)
    {
        var allHouses = HousingManager.Instance.GetAllHouses().ToList();
        if (allHouses.Count == 0)
        {
            CommandManager.SendErrorText(this, messageOutput, "No houses are loaded.");
            return;
        }

        var query = selector.Trim();
        var matches = new List<House>();
        var kind = string.Empty;

        if (query.StartsWith("tl:", StringComparison.OrdinalIgnoreCase) && ushort.TryParse(query[3..], out var tlId))
        {
            kind = $"tl:{tlId}";
            matches = allHouses.Where(h => h.TlId == tlId).ToList();
        }
        else if (query.StartsWith("id:", StringComparison.OrdinalIgnoreCase) && uint.TryParse(query[3..], out var dbId))
        {
            kind = $"id:{dbId}";
            matches = allHouses.Where(h => h.Id == dbId).ToList();
        }
        else if (query.StartsWith("obj:", StringComparison.OrdinalIgnoreCase) && uint.TryParse(query[4..], out var objId))
        {
            kind = $"obj:{objId}";
            matches = allHouses.Where(h => h.ObjId == objId).ToList();
        }
        else if (query.StartsWith("account:", StringComparison.OrdinalIgnoreCase) && uint.TryParse(query[8..], out var accountId))
        {
            kind = $"account:{accountId}";
            matches = allHouses.Where(h => h.AccountId == accountId).ToList();
        }
        else if (query.StartsWith("owner:", StringComparison.OrdinalIgnoreCase))
        {
            var name = query[6..].Trim();
            var ownerId = NameManager.Instance.GetCharacterId(name);
            kind = $"owner:{name}({ownerId})";
            matches = allHouses.Where(h => h.OwnerId == ownerId).ToList();
        }
        else if (query.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
        {
            var text = query[5..].Trim();
            kind = $"name~{text}";
            matches = allHouses.Where(h => h.Name != null &&
                                           h.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else if (uint.TryParse(query, out var bare))
        {
            // Bare number: tlId first (what the client shows), then db id, then objId
            kind = $"bare:{bare}";
            matches = allHouses.Where(h => h.TlId == bare || h.Id == bare || h.ObjId == bare).ToList();
        }
        else
        {
            kind = $"name~{query}";
            matches = allHouses.Where(h => h.Name != null &&
                                           h.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (matches.Count == 0)
        {
            CommandManager.SendErrorText(this, messageOutput, $"No house matches {kind}");
            return;
        }

        foreach (var h in matches.Take(25))
        {
            CommandManager.SendNormalText(this, messageOutput,
                $"tl={h.TlId} db={h.Id} obj={h.ObjId} tpl={h.TemplateId} step={h.CurrentStep} owner={h.OwnerId} acct={h.AccountId} \"{h.Name}\"");
        }

        if (matches.Count > 25)
            CommandManager.SendNormalText(this, messageOutput, $"...and {matches.Count - 25} more");

        if (matches.Count == 1)
        {
            ActiveHouseByCharacter[character.Id] = matches[0].Id;
            CommandManager.SendNormalText(this, messageOutput,
                $"Active house set to tl={matches[0].TlId} db={matches[0].Id} \"{matches[0].Name}\"");
        }
        else
        {
            CommandManager.SendNormalText(this, messageOutput,
                $"{matches.Count} matches - narrow it down (tl:/id:/obj:/account:/owner:/name:), then set with one match");
        }
    }

    /// <summary>
    /// Lists the runtime bound doodads and, with --persistent, the doodads table rows for this house.
    /// The DB rows are what /house dedupe cannot fix, so they are shown separately, not mixed in.
    /// </summary>
    private void DumpDoodads(House house, string[] args, IMessageOutput messageOutput)
    {
        var includePersistent = args.Any(a => a.Equals("--persistent", StringComparison.OrdinalIgnoreCase));
        var onlyDupes = args.Any(a => a.Equals("--dupes", StringComparison.OrdinalIgnoreCase));

        var runtime = house.AttachedDoodads;
        CommandManager.SendNormalText(this, messageOutput,
            $"House tl={house.TlId} db={house.Id} tpl={house.TemplateId}: {runtime?.Count ?? 0} runtime doodad(s)");

        if (runtime != null && runtime.Count > 0)
        {
            var groups = runtime
                .GroupBy(d => (d.TemplateId, d.AttachPoint))
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.TemplateId);

            foreach (var group in groups)
            {
                if (onlyDupes && group.Count() < 2)
                    continue;

                var objIds = string.Join(",", group.Select(d => d.ObjId));
                var flag = group.Count() > 1 ? "  <-- DUPLICATE" : string.Empty;
                CommandManager.SendNormalText(this, messageOutput,
                    $"  runtime tpl={group.Key.TemplateId} attach={group.Key.AttachPoint} x{group.Count()} objIds=[{objIds}]{flag}");
            }
        }

        if (!includePersistent)
            return;

        var rows = new List<(uint DbId, uint TemplateId, uint AttachPoint, byte OwnerType, ulong ItemId)>();
        using (var connection = MySQL.CreateConnection())
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT id, template_id, attach_point, owner_type, item_id FROM doodads " +
                "WHERE house_id = @houseId ORDER BY template_id, attach_point";
            command.Parameters.AddWithValue("@houseId", house.Id);
            command.Prepare();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((
                    reader.GetUInt32(0),
                    reader.GetUInt32(1),
                    reader.GetUInt32(2),
                    reader.GetByte(3),
                    reader.IsDBNull(4) ? 0UL : reader.GetUInt64(4)));
            }
        }

        CommandManager.SendNormalText(this, messageOutput,
            $"  {rows.Count} persistent row(s) with house_id={house.Id}");

        foreach (var group in rows.GroupBy(r => (r.TemplateId, r.AttachPoint)).OrderByDescending(g => g.Count()))
        {
            if (onlyDupes && group.Count() < 2)
                continue;

            foreach (var row in group)
            {
                CommandManager.SendNormalText(this, messageOutput,
                    $"  db id={row.DbId} tpl={row.TemplateId} attach={row.AttachPoint} ownerType={row.OwnerType} item={(row.ItemId > 0 ? row.ItemId.ToString() : "-")}");
            }
        }
    }

    /// <summary>
    /// Removes duplicate runtime bound doodads (same template + attach point, keeping the lowest
    /// ObjId). Only doodads that match one of the house template's bindings and carry no item are
    /// touched, so placed furniture and item-bearing doodads are never destroyed. Dry-run by default.
    /// </summary>
    private void DedupeDoodads(House house, string[] args, IMessageOutput messageOutput)
    {
        var confirmed = args.Any(a => a.Equals("confirm", StringComparison.OrdinalIgnoreCase));
        var runtime = house.AttachedDoodads;
        var bindings = house.Template?.HousingBindingDoodad;

        if (runtime == null || runtime.Count == 0)
        {
            CommandManager.SendNormalText(this, messageOutput, "This house has no runtime doodads.");
            return;
        }

        if (bindings == null || bindings.Length == 0)
        {
            CommandManager.SendErrorText(this, messageOutput,
                $"House template {house.TemplateId} declares no binding doodads - nothing to dedupe.");
            return;
        }

        var toRemove = new List<Doodad>();
        foreach (var group in runtime.GroupBy(d => (d.TemplateId, d.AttachPoint)))
        {
            var isBinding = bindings.Any(b => b.DoodadId == group.Key.TemplateId
                                              && b.AttachPointId == group.Key.AttachPoint);
            if (!isBinding)
                continue;

            var ordered = group.OrderBy(d => d.ObjId).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].ItemId > 0)
                    continue; // deleting would destroy the linked item

                toRemove.Add(ordered[i]);
            }
        }

        if (toRemove.Count == 0)
        {
            CommandManager.SendNormalText(this, messageOutput, "No duplicate bound doodads found.");
            return;
        }

        var list = string.Join(",", toRemove.Select(d => d.ObjId));
        if (!confirmed)
        {
            CommandManager.SendNormalText(this, messageOutput,
                $"DRY RUN: would remove {toRemove.Count} duplicate bound doodad(s): objIds=[{list}]. Re-run with 'confirm' to apply.");
            return;
        }

        var removed = 0;
        foreach (var doodad in toRemove)
        {
            house.AttachedDoodads.Remove(doodad);
            doodad.Delete();
            removed++;
        }

        CommandManager.SendNormalText(this, messageOutput,
            $"Removed {removed} duplicate bound doodad(s): objIds=[{list}]");
    }
}
