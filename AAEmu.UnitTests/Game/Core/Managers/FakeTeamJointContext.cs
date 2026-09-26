using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Team;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>In-memory stand-in for the World so joint/summon flows run without a live server.</summary>
internal sealed class FakeTeamJointContext : ITeamJointContext
{
    private readonly Dictionary<uint, TeamJointTeamSnapshot> _teams = new();
    private readonly Dictionary<uint, TeamJointCharacterSnapshot> _characters = new();

    public List<(uint CharacterId, GamePacket Packet)> Sent { get; } = [];
    public List<(uint CharacterId, ErrorMessageType Error)> Errors { get; } = [];
    public List<(uint TeamId, uint RecipientId)> HeadersSent { get; } = [];
    public List<uint> LootRulesReset { get; } = [];
    public List<(uint CharacterId, uint TaskId, string Argv0, string Argv1)> Dialogs { get; } = [];
    public List<(uint CharacterId, TeamSummonDestination Destination)> Teleports { get; } = [];
    public List<(uint CharacterId, uint ItemTemplateId)> Consumed { get; } = [];
    public HashSet<uint> OnCooldown { get; } = [];
    public HashSet<uint> NoLandingSpace { get; } = [];
    public bool TeleportSucceeds { get; set; } = true;
    public bool HasFlame { get; set; } = true;
    public sbyte LocalWorldId { get; set; }

    public FakeTeamJointContext AddTeam(uint teamId, uint ownerId, bool isParty = false, params uint[] members)
    {
        var online = members.Length == 0 ? [ownerId] : members;
        _teams[teamId] = new TeamJointTeamSnapshot(teamId, isParty, ownerId, 0, online.Length, online, 0, false, 0);
        return this;
    }

    public FakeTeamJointContext SetOfficer(uint teamId, ulong officerId)
    {
        var team = _teams[teamId];
        _teams[teamId] = team with { OfficerId = officerId };
        return this;
    }

    public FakeTeamJointContext AddCharacter(uint id, string name, bool isOnline = true, bool isInBattle = false)
    {
        _characters[id] = new TeamJointCharacterSnapshot(id, name, isOnline, isInBattle, 133u, 10f, 20f, 30f);
        return this;
    }

    public FakeTeamJointContext GoOffline(uint id) => SetOnline(id, false);

    public FakeTeamJointContext SetOnline(uint id, bool online)
    {
        _characters[id] = _characters[id] with { IsOnline = online };
        return this;
    }

    public FakeTeamJointContext SetInBattle(uint id, bool inBattle)
    {
        // The live context reports combat as a shipped caution state, so the fake must too.
        var character = _characters[id];
        var states = inBattle
            ? character.BlockedStates | TeamSummonRefusal.InCombat
            : character.BlockedStates & ~TeamSummonRefusal.InCombat;
        _characters[id] = character with { IsInBattle = inBattle, BlockedStates = states };
        return this;
    }

    public FakeTeamJointContext SetBlockedStates(uint id, TeamSummonRefusal states)
    {
        _characters[id] = _characters[id] with { BlockedStates = states };
        return this;
    }

    public FakeTeamJointContext SetPosition(uint id, float x, float y, float z, uint? zoneId = null)
    {
        var character = _characters[id];
        _characters[id] = character with
        {
            X = x,
            Y = y,
            Z = z,
            ZoneId = zoneId ?? character.ZoneId,
        };
        return this;
    }

    public FakeTeamJointContext RemoveTeam(uint teamId)
    {
        _teams.Remove(teamId);
        return this;
    }

    public TeamJointTeamSnapshot? Team(uint teamId) => _teams.GetValueOrDefault(teamId);

    public int CountPackets<T>() where T : GamePacket => Sent.Count(entry => entry.Packet is T);

    public IReadOnlyList<T> PacketsTo<T>(uint characterId) where T : GamePacket =>
        Sent.Where(entry => entry.CharacterId == characterId && entry.Packet is T).Select(entry => (T)entry.Packet).ToArray();

    public TeamJointTeamSnapshot? FindTeam(uint teamId) => _teams.GetValueOrDefault(teamId);

    public TeamJointTeamSnapshot? FindTeamByMember(uint unitId) =>
        _teams.Values.FirstOrDefault(team => team.OnlineMemberIds.Contains(unitId));

    public TeamJointCharacterSnapshot? FindCharacterByName(string name) =>
        _characters.Values.FirstOrDefault(character => character.Name == name);

    public TeamJointCharacterSnapshot? FindCharacterById(uint characterId) => _characters.GetValueOrDefault(characterId);

    /// <summary>Set per test to model what a character currently has selected.</summary>
    public readonly Dictionary<uint, uint> SelectedTargets = [];

    public TeamJointCharacterSnapshot? FindSelectedTarget(uint characterId) =>
        SelectedTargets.TryGetValue(characterId, out var targetId)
            ? _characters.GetValueOrDefault(targetId)
            : null;

    public bool IsLocalWorld(sbyte worldId) => worldId == LocalWorldId;

    public void Send(uint characterId, GamePacket packet) => Sent.Add((characterId, packet));

    public void SendError(uint characterId, ErrorMessageType error) => Errors.Add((characterId, error));

    public void SendTeamHeader(uint teamId, uint recipientId) => HeadersSent.Add((teamId, recipientId));

    public void ApplyJoint(uint teamId, uint jointId, bool isLeader, int order) =>
        _teams[teamId] = _teams[teamId] with { JointId = jointId, IsJointLeader = isLeader, JointOrder = order };

    public void ClearJoint(uint teamId) =>
        _teams[teamId] = _teams[teamId] with { JointId = 0, IsJointLeader = false, JointOrder = 0 };

    public bool TryTeleport(uint characterId, TeamSummonDestination destination)
    {
        if (!TeleportSucceeds)
            return false;
        Teleports.Add((characterId, destination));
        var character = _characters[characterId];
        _characters[characterId] = character with
        {
            ZoneId = destination.ZoneId,
            X = destination.X,
            Y = destination.Y,
            Z = destination.Z,
        };
        return true;
    }

    public bool HasSummonFlame(uint characterId, uint itemTemplateId) => HasFlame;

    public bool TryConsumeSummonFlame(uint characterId, uint itemTemplateId)
    {
        if (!HasFlame)
            return false;
        Consumed.Add((characterId, itemTemplateId));
        return true;
    }

    public bool IsOnSummonCooldown(uint characterId, uint skillId) => OnCooldown.Contains(characterId);

    public bool HasSummonLandingSpace(uint characterId) => !NoLandingSpace.Contains(characterId);

    public void ResetLootRules(uint teamId) => LootRulesReset.Add(teamId);

    public void SendDialogTask(uint characterId, uint taskId, string argv0, string argv1) =>
        Dialogs.Add((characterId, taskId, argv0, argv1));
}
