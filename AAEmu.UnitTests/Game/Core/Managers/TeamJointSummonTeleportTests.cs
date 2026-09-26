using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Team;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>
/// The summon as a move, plus the joint follow-ups that ship with it: the break ask's expiry, the
/// break capability push, the loot reset, the dialog prompts, and the refusal matrix.
/// </summary>
public class TeamJointSummonTeleportTests
{
    private const uint TeamA = 100u;
    private const uint Alice = 1u;   // owner of TeamA, the summoner
    private const uint Carol = 3u;   // plain member of TeamA, the one summoned
    private const uint Bob = 2u;     // owner of the other raid
    private const uint TeamB = 200u;

    /// <summary>Where Alice stands. Carol starts somewhere else so a move is observable.</summary>
    private const float SummonerX = 111f;
    private const float SummonerY = 222f;
    private const float SummonerZ = 333f;

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan by) => Now += by;
    }

    private sealed class FakeSummonContent : ITeamSummonContent
    {
        public bool Available { get; set; } = true;

        public bool TryResolve(out TeamJointSummonContent.SummonTemplate template, out string reason)
        {
            if (!Available)
            {
                template = null;
                reason = "const_item_types 'team_summon' is missing";
                return false;
            }

            // Shaped like the shipped rows: flame 46130, attempt skill 39700 (1000 ms cooldown,
            // 3000 ms cast, 4 m range), channelling skill 39701 (60000 ms, buff 23368).
            template = new TeamJointSummonContent.SummonTemplate(
                46130u, 39700u, 39701u, 23368u, 1000, 3000, 60000, 4);
            reason = null;
            return true;
        }
    }

    private static (TeamJointManager Manager, FakeTeamJointContext World, Clock Time, FakeSummonContent Content) Build()
    {
        var clock = new Clock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var world = new FakeTeamJointContext { LocalWorldId = 1 };
        world.AddCharacter(Alice, "Alice").AddCharacter(Carol, "Carol");
        world.AddTeam(TeamA, Alice, false, Alice, Carol);
        world.SetPosition(Alice, SummonerX, SummonerY, SummonerZ);
        world.SetPosition(Carol, 1f, 2f, 3f);
        var content = new FakeSummonContent();
        return (new TeamJointManager(world, clock, content), world, clock, content);
    }

    // ---------- the summon moves the member ----------

    [Test]
    public async Task Accept_MovesTheMemberToTheSummonerPosition()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsTrue();

        // The landing is the summoner's position, not the recipient's own.
        var move = world.Teleports.Single();
        await Assert.That(move.CharacterId).IsEqualTo(Carol);
        await Assert.That(move.Destination.X).IsEqualTo(SummonerX);
        await Assert.That(move.Destination.Y).IsEqualTo(SummonerY);
        await Assert.That(move.Destination.Z).IsEqualTo(SummonerZ);
        await Assert.That(move.Destination.ZoneId).IsEqualTo(133u);

        var carol = world.FindCharacterById(Carol);
        await Assert.That(carol.X).IsEqualTo(SummonerX);
        await Assert.That(carol.Y).IsEqualTo(SummonerY);
        await Assert.That(carol.Z).IsEqualTo(SummonerZ);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(1);
    }

    [Test]
    public async Task Accept_SpendsTheSummonersFlameOnce()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsTrue();

        var spend = world.Consumed.Single();
        await Assert.That(spend.CharacterId).IsEqualTo(Alice);
        await Assert.That(spend.ItemTemplateId).IsEqualTo(46130u);
    }

    [Test]
    public async Task Accept_WithoutTheFlameRefusesAndMovesNobody()
    {
        var (manager, world, _, _) = Build();
        world.HasFlame = false;
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(0);
        await Assert.That(world.Errors.Any(e => e.CharacterId == Alice && e.Error == ErrorMessageType.SummonFail)).IsTrue();
    }

    [Test]
    public async Task Accept_OnCooldownRefusesAndMovesNobody()
    {
        var (manager, world, _, _) = Build();
        world.OnCooldown.Add(Alice);
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(0);
        // A refused summon must not cost the summoner the flame, so nothing may be consumed.
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Accept_WhenTheWorldRefusesTheMoveReportsFailure()
    {
        var (manager, world, _, _) = Build();
        world.TeleportSucceeds = false;
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(0);
        // The move is what the flame pays for, so a move that never happened costs nothing.
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
        await Assert.That(world.Errors.Any(e => e.CharacterId == Carol && e.Error == ErrorMessageType.SummonFail)).IsTrue();
    }

    [Test]
    public async Task Accept_WhenContentIsMissingRefusesAndMovesNobody()
    {
        var (manager, world, _, content) = Build();
        content.Available = false;
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Accept_WithNoLandingSpaceRefusesBeforeSpendingTheFlame()
    {
        var (manager, world, _, _) = Build();
        world.NoLandingSpace.Add(Carol);
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.Errors.Any(e => e.Error == ErrorMessageType.SummonNotEnoughSpace)).IsTrue();
    }

    [Test]
    public async Task ExactlyOnce_ARepeatedAcceptMovesNobodyASecondTime()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsTrue();
        // The round is consumed by the first accept, so the replay finds nothing to answer.
        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();

        await Assert.That(world.Teleports.Count).IsEqualTo(1);
        await Assert.That(world.Consumed.Count).IsEqualTo(1);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(1);
    }

    [Test]
    public async Task Decline_MovesNobodyAndSpendsNothing()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: false, "Alice")).IsTrue();
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(0);
    }

    // ---------- the shipped refusal matrix ----------

    [Test]
    [Arguments(TeamSummonRefusal.InDungeon)]
    [Arguments(TeamSummonRefusal.InTrial)]
    [Arguments(TeamSummonRefusal.Imprisoned)]
    [Arguments(TeamSummonRefusal.InSiege)]
    [Arguments(TeamSummonRefusal.Incapacitated)]
    [Arguments(TeamSummonRefusal.Dead)]
    [Arguments(TeamSummonRefusal.WearingBackpack)]
    [Arguments(TeamSummonRefusal.InCombat)]
    public async Task EveryShippedCautionStateRefusesTheMove(TeamSummonRefusal state)
    {
        var (manager, world, _, _) = Build();
        world.SetBlockedStates(Carol, state);
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
        await Assert.That(world.CountPackets<SCTeamSummonPacket>()).IsEqualTo(0);
        await Assert.That(world.Errors.Any(e => e.Error == ErrorMessageType.SummonFail)).IsTrue();
    }

    [Test]
    public async Task TheCautionListIsTheSevenShippedConditions()
    {
        // ui_texts "team_summon_notice" names exactly these, in this order. Combat is refused too, but
        // the client never gets as far as showing a frame for it.
        await Assert.That(TeamSummonRefusalRules.ShippedList).IsEquivalentTo(new[]
        {
            TeamSummonRefusal.InDungeon,
            TeamSummonRefusal.InTrial,
            TeamSummonRefusal.Imprisoned,
            TeamSummonRefusal.InSiege,
            TeamSummonRefusal.Incapacitated,
            TeamSummonRefusal.Dead,
            TeamSummonRefusal.WearingBackpack,
        });
        await Assert.That(TeamSummonRefusalRules.ShippedList.Contains(TeamSummonRefusal.InCombat)).IsFalse();
        await Assert.That(TeamSummonRefusalRules.Refuses(TeamSummonRefusal.InCombat)).IsTrue();
        await Assert.That(TeamSummonRefusalRules.Refuses(TeamSummonRefusal.None)).IsFalse();
    }

    [Test]
    public async Task RefusalLogNamesCombatEvenThoughTheCautionBoxDoesNot()
    {
        // Combat is not in the shipped list, so a naive walk would log no reason at all.
        await Assert.That(TeamSummonRefusalRules.ActiveNames(TeamSummonRefusal.InCombat))
            .IsEquivalentTo(new[] { "in combat" });
        await Assert.That(TeamSummonRefusalRules.ActiveNames(
                TeamSummonRefusal.Dead | TeamSummonRefusal.InCombat))
            .IsEquivalentTo(new[] { "dead", "in combat" });
        await Assert.That(TeamSummonRefusalRules.ActiveNames(TeamSummonRefusal.None)).IsEmpty();
    }

    [Test]
    public async Task NoCautionStateButContentMissingAlsoSpendsNothing()
    {
        var (manager, world, _, content) = Build();
        content.Available = false;
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsFalse();
        await Assert.That(world.Consumed.Count).IsEqualTo(0);
        await Assert.That(world.Teleports.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoCautionStateLetsTheMoveThrough()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        await Assert.That(manager.ReplyToSummon(Carol, accepted: true, "Alice")).IsTrue();
        await Assert.That(world.Teleports.Count).IsEqualTo(1);
    }

    // ---------- the break ask expires ----------

    [Test]
    public async Task BreakAsk_IsGoneAfterTheClientFrameCountdown()
    {
        var (manager, world, clock, _) = Build();
        Joint(manager, world);
        manager.RespondToJointBreak(Alice, ask: true, accept: false);
        await Assert.That(manager.PendingBreakCount).IsEqualTo(1);

        // One tick before the client's 120 s frame ends, the ask is still answerable.
        clock.Advance(TimeSpan.FromMilliseconds(119_000));
        manager.RespondToJointBreak(Bob, ask: false, accept: true);
        await Assert.That(manager.SessionCount).IsEqualTo(0);
    }

    [Test]
    public async Task BreakAsk_IsUnanswerableOnceTheFrameHasEnded()
    {
        var (manager, world, clock, _) = Build();
        Joint(manager, world);
        manager.RespondToJointBreak(Alice, ask: true, accept: false);

        clock.Advance(TimeSpan.FromMilliseconds(120_001));
        manager.RespondToJointBreak(Bob, ask: false, accept: true);

        // The ask is gone, so a late answer cannot dissolve anything.
        await Assert.That(manager.PendingBreakCount).IsEqualTo(0);
        await Assert.That(manager.SessionCount).IsEqualTo(1);
    }

    // ---------- loot reset, dialogs ----------

    [Test]
    public async Task FormingAJoint_ResetsTheLootRulesOfBothRaids()
    {
        var (manager, world, _, _) = Build();
        Joint(manager, world);

        // The shipped notices promise a reset when the raids federate.
        await Assert.That(world.LootRulesReset.Distinct().Order()).IsEquivalentTo(new[] { TeamA, TeamB });
    }

    [Test]
    public async Task Dissolving_JointResetsTheLootRulesAgain()
    {
        var (manager, world, _, _) = Build();
        Joint(manager, world);
        world.LootRulesReset.Clear();

        // Alice leads, so she asks; the other raid's owner answers, and only they can.
        manager.RespondToJointBreak(Alice, ask: true, accept: false);
        manager.RespondToJointBreak(Bob, ask: false, accept: true);

        await Assert.That(manager.SessionCount).IsEqualTo(0);
        await Assert.That(world.LootRulesReset.Distinct().Order()).IsEquivalentTo(new[] { TeamA, TeamB });
    }

    [Test]
    public async Task JointRequest_OpensTheRequestFrameOnTheAsker()
    {
        var (manager, world, _, _) = Build();
        AddSecondRaid(world);
        manager.RequestJointInfo(Alice, 0x5A5Au, TeamJointModes.MenuChatRequest, "Bob", 1);

        var dialog = world.Dialogs.Single(entry => entry.CharacterId == Alice);
        await Assert.That(dialog.TaskId).IsEqualTo(TeamJointDialogTasks.RequestRaidJoint);
        await Assert.That(dialog.Argv0).IsEqualTo("Bob");
    }

    [Test]
    public async Task JointAnswer_OpensTheResponseFrameOnTheOtherOwner()
    {
        var (manager, world, _, _) = Build();
        AddSecondRaid(world);
        manager.RequestJointInfo(Alice, 0x5A5Au, TeamJointModes.MenuChatRequest, "Bob", 1);
        manager.RespondToJoint(Alice, 0x5A5Au, myTeamLeader: true, accept: true, timeout: false);

        var dialog = world.Dialogs.Single(entry => entry.CharacterId == Bob);
        await Assert.That(dialog.TaskId).IsEqualTo(TeamJointDialogTasks.ResponseRaidJoint);
        await Assert.That(dialog.Argv0).IsEqualTo("Alice");
    }

    [Test]
    public async Task SummonRequest_OpensTheSuggestFrameOnEveryTarget()
    {
        var (manager, world, _, _) = Build();
        manager.RequestSummons(Alice);

        var dialog = world.Dialogs.Single(entry => entry.CharacterId == Carol);
        await Assert.That(dialog.TaskId).IsEqualTo(TeamJointDialogTasks.TeamSummonSuggest);
        await Assert.That(dialog.Argv0).IsEqualTo("Alice");
    }

    // ---------- the dialog task ids the client dispatches on ----------

    [Test]
    public async Task DialogTaskIdsMatchTheClientConstantTable()
    {
        // The client registers a handler under each of these names and dispatches on the id.
        await Assert.That(TeamJointDialogTasks.RequestRaidJoint).IsEqualTo(88u);
        await Assert.That(TeamJointDialogTasks.ResponseRaidJoint).IsEqualTo(93u);
        await Assert.That(TeamJointDialogTasks.TeamSummonSuggest).IsEqualTo(94u);
    }

    /// <summary>Adds the other raid, so a joint can be driven without going through the helper.</summary>
    private static void AddSecondRaid(FakeTeamJointContext world)
    {
        world.AddCharacter(Bob, "Bob");
        world.AddTeam(TeamB, Bob, false, Bob);
    }

    /// <summary>Drives Alice's and Bob's raids into a joint with Alice leading.</summary>
    /// <remarks>
    /// The leader flag is read in the response dialog's polarity, where leader == true means the
    /// RESPONDER is the owner, and <c>CommitJoint</c> resolves
    /// <c>leaderTeamId = LeaderChoice ? TargetTeamId : SourceTeamId</c>. Bob is the responder, so
    /// he must echo <c>false</c> for Alice's (the source) raid to lead. Echoing the same value the
    /// server offered would instead hand leadership to Bob's raid.
    /// </remarks>
    private static void Joint(TeamJointManager manager, FakeTeamJointContext world)
    {
        AddSecondRaid(world);
        manager.RequestJointInfo(Alice, 0x5A5Au, TeamJointModes.MenuChatRequest, "Bob", 1);
        manager.RespondToJoint(Alice, 0x5A5Au, myTeamLeader: true, accept: true, timeout: false);
        manager.RespondToJoint(Bob, 0x5A5Au, myTeamLeader: false, accept: true, timeout: false);
    }
}
