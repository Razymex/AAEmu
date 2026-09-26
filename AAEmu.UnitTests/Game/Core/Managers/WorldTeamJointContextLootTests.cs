using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Team;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

using GameTeam = AAEmu.Game.Models.Game.Team.Team;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>
/// Exercises the live <see cref="WorldTeamJointContext"/> against a real <see cref="GameTeam"/> and a
/// recording team manager.
/// </summary>
/// <remarks>
/// The joint flow tests drive the manager through a fake world, so they can only prove that a reset
/// was requested. The dice bid is server state reached through <c>ITeamManager.ChangeDiceBidRule</c>,
/// and that loop lives in the live context — so without this class the promise made by the shipped
/// jointed/dismissed texts ("the dice bid resets") would be entirely untested and would still pass if
/// the loop were deleted.
/// </remarks>
public class WorldTeamJointContextLootTests
{
    private const uint TeamId = 100u;
    private const uint Alice = 1u;
    private const uint Carol = 3u;
    private const uint Dave = 4u;

    private sealed class RecordingTeamManager : ITeamManager
    {
        private readonly GameTeam _team;

        public RecordingTeamManager(GameTeam team) => _team = team;

        /// <summary>Every rule change the context asked for, in call order.</summary>
        public List<(Character Unit, int TeamId, ulong MemberId, DiceBidRuleKind Rule, bool ByIdleState)> DiceBidCalls { get; } = [];

        public void Load() { }
        public GameTeam GetActiveTeamByUnit(uint unitId) => _team;
        public GameTeam GetTeamByObjId(uint objId) => _team;
        public GameTeam GetActiveTeam(uint teamId) => teamId == TeamId ? _team : null;
        public void MemberRemoveFromTeam(Character unit, Character source, RiskyAction leaveType) { }

        public void ChangeDiceBidRule(Character unit, int teamId, ulong memberId, DiceBidRuleKind rule, bool byIdleState) =>
            DiceBidCalls.Add((unit, teamId, memberId, rule, byIdleState));
    }

    /// <summary>Only the members the loot reset walks are needed; nothing here touches the world.</summary>
    private sealed class UnusedWorldManager : IWorldManager
    {
        public void Load() { }
        public WorldInstance MainWorld { get; set; }
        public void CreateStaticInstances() { }
        public WorldInstance CreateWorldInstance(WorldTemplate worldTemplate, uint channelId, bool overrideInstanceId = false, uint fixedInstanceId = 0, Character notifyPlayer = null) => null;
        public WorldTemplate CreateWorldTemplate(string worldName) => null;
        public Character GetCharacterByObjId(uint id) => null;
        public Character GetCharacterById(uint id) => null;
        public Character GetCharacter(string name) => null;
        public List<Character> GetAllCharacters() => [];
        public uint GetZoneId(WorldTemplate worldTemplate, float x, float y) => 0;
        public WorldTemplate GetWorldTemplateByName(string worldName) => null;
        public WorldTemplate GetWorldTemplateByZoneKey(uint zoneKey) => null;
        public WorldInstance[] GetWorlds() => [];
        public WorldInstance GetWorld(uint worldInstanceId) => null;
        public List<uint> GetZoneKeysByWorldId(uint worldId) => [];
        public void BroadcastPacketToServer(GamePacket packet) { }
        public Character GetTargetOrSelf(Character character, string targetName, out int firstNonNameArgument) { firstNonNameArgument = 0; return null; }
        public bool TryRemoveCharacter(uint playerObjId) => false;
        public void Initialize() { }
    }

    private static Character Member(uint id)
    {
        // The pattern the other unit tests use for a character that only needs an identity.
        return new Character(new UnitCustomModelParams()) { Id = id };
    }

    private static (WorldTeamJointContext Context, RecordingTeamManager Teams, GameTeam Team) Build(
        params uint[] memberIds)
    {
        var team = new GameTeam();
        // Members go into consecutive slots. GetIndex cannot be used to place them: it only finds a
        // member that is already in the array, which is the opposite of what a fixture needs.
        for (var i = 0; i < memberIds.Length; i++)
        {
            team.Members[i] = new TeamMember(Member(memberIds[i]))
            {
                DiceBidRule = DiceBidRuleKind.AutoGiveUp,
            };
        }

        var teams = new RecordingTeamManager(team);
        return (new WorldTeamJointContext(new UnusedWorldManager(), teams), teams, team);
    }

    [Test]
    public async Task ResetLootRules_PutsEveryMemberBackToTheDefaultBid()
    {
        var (context, teams, _) = Build(Alice, Carol, Dave);

        context.ResetLootRules(TeamId);

        // Every member, not just the leader: the shipped text says the bidding method resets for the
        // joint, which is a property of the whole raid.
        await Assert.That(teams.DiceBidCalls.Count).IsEqualTo(3);
        await Assert.That(teams.DiceBidCalls.Select(call => call.MemberId).Order())
            .IsEquivalentTo(new[] { (ulong)Alice, (ulong)Carol, (ulong)Dave });
        await Assert.That(teams.DiceBidCalls.All(call => call.Rule == DiceBidRuleKind.Default)).IsTrue();
        await Assert.That(teams.DiceBidCalls.All(call => call.TeamId == (int)TeamId)).IsTrue();
        // The reset is an explicit change, not an idle-state artefact.
        await Assert.That(teams.DiceBidCalls.All(call => call.ByIdleState == false)).IsTrue();
    }

    [Test]
    public async Task ResetLootRules_RebuildsTheAcquisitionRule()
    {
        var (context, _, team) = Build(Alice);
        team.LootingRule.LootMethod = LootingRuleMethod.LootMaster;
        team.LootingRule.LootMaster = 999;

        context.ResetLootRules(TeamId);

        // Back to the configured default, which is whatever a fresh rule starts as — not a value
        // hardcoded here, and not necessarily FreeForAll.
        var fresh = new LootingRule();
        await Assert.That(team.LootingRule.LootMethod).IsEqualTo(fresh.LootMethod);
        await Assert.That(team.LootingRule.LootMaster).IsEqualTo(fresh.LootMaster);
        await Assert.That(team.LootingRule.LootMethod).IsNotEqualTo(LootingRuleMethod.LootMaster);
    }

    [Test]
    public async Task ResetLootRules_IgnoresATeamThatIsNotThere()
    {
        var (context, teams, _) = Build(Alice);

        // No throw, and nothing recorded: a raid that has gone away is not an error here.
        context.ResetLootRules(9999u);
        await Assert.That(teams.DiceBidCalls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ResetLootRules_SkipsEmptyMemberSlots()
    {
        // A raid is a fixed-size array; the tail is null until members fill it.
        var (context, teams, _) = Build(Alice);

        context.ResetLootRules(TeamId);

        await Assert.That(teams.DiceBidCalls.Count).IsEqualTo(1);
        await Assert.That(teams.DiceBidCalls.Single().MemberId).IsEqualTo((ulong)Alice);
    }
}
