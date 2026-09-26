using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Team;
using AAEmu.Game.Models.Game.World;

using NLog;

namespace AAEmu.Game.Core.Managers;

/// <summary>Live <see cref="ITeamJointContext"/> over TeamManager and the World character registry.</summary>
public sealed class WorldTeamJointContext(IWorldManager worldManager, ITeamManager teamManager) : ITeamJointContext
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public TeamJointTeamSnapshot? FindTeam(uint teamId)
    {
        var team = teamManager.GetActiveTeam(teamId);
        return team == null ? null : Snapshot(team);
    }

    public TeamJointTeamSnapshot? FindTeamByMember(uint unitId)
    {
        var team = teamManager.GetActiveTeamByUnit(unitId);
        return team == null ? null : Snapshot(team);
    }

    public TeamJointCharacterSnapshot? FindCharacterByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return Snapshot(worldManager.GetCharacter(name));
    }

    public TeamJointCharacterSnapshot? FindCharacterById(uint characterId) =>
        Snapshot(worldManager.GetCharacterById(characterId));

    public TeamJointCharacterSnapshot? FindSelectedTarget(uint characterId)
    {
        var character = worldManager.GetCharacterById(characterId);
        // Only a live character target can name another character; anything else (an npc, a
        // doodad, nothing) cannot start a joint, so it is reported as no target.
        return Snapshot(character?.CurrentTarget as Character);
    }

    public bool IsLocalWorld(sbyte worldId) =>
        worldId == CharacterBlocked.LocalWorldId || unchecked((byte)worldId) == AppConfiguration.Instance.Id;

    public void Send(uint characterId, GamePacket packet) =>
        worldManager.GetCharacterById(characterId)?.SendPacket(packet);

    public void SendError(uint characterId, ErrorMessageType error) =>
        worldManager.GetCharacterById(characterId)?.SendErrorMessage(error);

    public void SendTeamHeader(uint teamId, uint recipientId)
    {
        var team = teamManager.GetActiveTeam(teamId);
        if (team != null)
            Send(recipientId, new SCJoinedTeamPacket(team));
    }

    public void ApplyJoint(uint teamId, uint jointId, bool isLeader, int order)
    {
        var team = teamManager.GetActiveTeam(teamId);
        if (team == null)
            return;
        team.JointId = jointId;
        team.IsJointLeader = isLeader;
        team.JointOrder = order;
    }

    public void ClearJoint(uint teamId)
    {
        var team = teamManager.GetActiveTeam(teamId);
        if (team == null)
            return;
        team.JointId = 0;
        team.IsJointLeader = false;
        team.JointOrder = 0;
    }

    public bool TryTeleport(uint characterId, TeamSummonDestination destination)
    {
        var character = worldManager.GetCharacterById(characterId);
        if (character == null)
            return false;
        if (character.Transform.ZoneId != destination.ZoneId)
        {
            // Moving between zones is a handoff, not a position write; this slice does not model it.
            Logger.Warn("Summon of character {0} spans zones ({1} to {2}) and is not moved.",
                characterId, character.Transform.ZoneId, destination.ZoneId);
            return false;
        }

        // The blink packet alone only moves the client: the zone keeps simulating the character at
        // the old spot and pulls it back, which reads as "the summon did nothing". Land the character
        // server-side and tell the zone, exactly as the blink effect does. The landing is world-space
        // and SetPosition writes the local transform, so convert first.
        var local = character.Transform.GetLocalFromWorld(destination.X, destination.Y, destination.Z);
        var rotation = character.Transform.Local.Rotation;
        character.SetPosition(local.X, local.Y, local.Z, rotation.X, rotation.Y, rotation.Z);
        character.SendPacket(new SCBlinkUnitPacket(character.ObjId, 0f, 0f, false,
            destination.X, destination.Y, destination.Z));
        if (WorldIntegration.ZoneAuthority)
            WorldIntegration.RelayBlinkToZone?.Invoke(
                character.ObjId, character.ObjId, false, destination.X, destination.Y, destination.Z);
        return true;
    }

    public bool HasSummonFlame(uint characterId, uint itemTemplateId)
    {
        var character = worldManager.GetCharacterById(characterId);
        return character != null && itemTemplateId != 0 &&
               character.Inventory.CheckItems(SlotType.Inventory, itemTemplateId, 1);
    }

    public bool TryConsumeSummonFlame(uint characterId, uint itemTemplateId)
    {
        var character = worldManager.GetCharacterById(characterId);
        if (character == null || itemTemplateId == 0)
            return false;
        return character.Inventory.Bag.ConsumeItem(
            ItemTaskType.SkillReagents, itemTemplateId, 1, null) == 1;
    }

    public bool IsOnSummonCooldown(uint characterId, uint skillId)
    {
        var character = worldManager.GetCharacterById(characterId);
        return character != null && skillId != 0 && character.Cooldowns.CheckCooldown(skillId);
    }

    public bool HasSummonLandingSpace(uint characterId)
    {
        // Nothing in the current world model can prove there is room to land, so this never refuses.
        // The client's own caution list is what the player is shown; see TeamSummonRefusal.
        _ = characterId;
        return true;
    }

    public void ResetLootRules(uint teamId)
    {
        var team = teamManager.GetActiveTeam(teamId);
        if (team == null)
            return;
        // The shipped notices promise the acquisition method goes back to its default and that the
        // dice bid resets. Both are real server state, so both are reset.
        team.LootingRule = new LootingRule();

        // ChangeDiceBidRule validates the kind, stores it and broadcasts the change, so it is used
        // rather than writing the member field directly.
        foreach (var member in team.Members)
        {
            var character = member?.Character;
            if (character == null)
                continue;
            teamManager.ChangeDiceBidRule(character, (int)teamId, character.Id,
                DiceBidRuleKind.Default, byIdleState: false);
        }
    }

    public void SendDialogTask(uint characterId, uint taskId, string argv0, string argv1) =>
        Send(characterId, new SCNotifyUIMessagePacket(taskId, 2, argv0, argv1));

    private static TeamJointTeamSnapshot Snapshot(Team team) =>
        new(team.Id,
            team.IsParty,
            team.OwnerId,
            team.OfficerId,
            team.MembersCount(),
            team.Members
                .Where(member => member?.Character is { IsOnline: true })
                .Select(member => member.Character.Id)
                .ToArray(),
            team.JointId,
            team.IsJointLeader,
            team.JointOrder);

    private static TeamJointCharacterSnapshot? Snapshot(Character character)
    {
        if (character == null)
            return null;
        var position = character.Transform.World.Position;
        return new TeamJointCharacterSnapshot(
            character.Id,
            character.Name,
            character.IsOnline,
            character.IsInBattle,
            character.Transform.ZoneId,
            position.X,
            position.Y,
            position.Z,
            BlockedStates(character));
    }

    /// <summary>
    /// Fills the shipped caution list from the states this server can actually answer today.
    /// <para>
    /// Dead, in combat, imprisoned and a worn backpack all have a real predicate, so they are
    /// reported. Dungeon entry, trial, siege participation and incapacitation have none in the
    /// current world model, so they stay clear rather than being assumed; the rule still refuses on
    /// them, it simply cannot be tripped by this build yet. That gap is recorded in the slice doc.
    /// </para>
    /// </summary>
    private static TeamSummonRefusal BlockedStates(Character character)
    {
        var states = TeamSummonRefusal.None;
        if (character.IsDead)
            states |= TeamSummonRefusal.Dead;
        if (character.IsInBattle)
            states |= TeamSummonRefusal.InCombat;
        if (JusticeManager.IsPrisoner(character))
            states |= TeamSummonRefusal.Imprisoned;
        if (character.Inventory.GetEquippedBySlot(EquipmentItemSlot.Backpack) is not null)
            states |= TeamSummonRefusal.WearingBackpack;
        return states;
    }
}
