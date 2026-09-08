using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.ScheduleItems;

namespace AAEmu.UnitTests.Game.Models.Game;

public class AccountPatronRulesTests
{
    private static readonly uint[] Stacked =
        [(uint)AccountMembership.Ancient, (uint)AccountMembership.Advanced];

    [Test]
    public async Task StackedMemberships_AreAncientAndAdvanced()
    {
        await Assert.That(AccountPatronRules.StackedMemberships).IsEquivalentTo(Stacked);
        await Assert.That(AccountPatronRules.HasStacked(Stacked)).IsTrue();
        await Assert.That(AccountPatronRules.HasStacked([(uint)AccountMembership.Ancient])).IsFalse();
        await Assert.That(AccountPatronRules.IsArcheLife(Stacked)).IsTrue();
        await Assert.That(AccountPatronRules.IsArcheLife([(uint)AccountMembership.Advanced])).IsFalse();
    }

    [Test]
    public async Task ResolvePoint_StackedUsesThePaidFloor()
    {
        await Assert.That(AccountPatronRules.ResolvePoint(0, 1, 400, grantStacked: true, forceMaxGrade: false))
            .IsEqualTo(1);
        await Assert.That(AccountPatronRules.ResolvePoint(80, 1, 400, grantStacked: true, forceMaxGrade: false))
            .IsEqualTo(80);
        await Assert.That(AccountPatronRules.ResolvePoint(0, 1, 400, grantStacked: false, forceMaxGrade: false))
            .IsEqualTo(0);
        await Assert.That(AccountPatronRules.ResolvePoint(0, 1, 400, grantStacked: true, forceMaxGrade: true))
            .IsEqualTo(400);
    }

    [Test]
    public async Task ResolveGrade_StackedUsesTheFirstPaidRow()
    {
        await Assert.That(AccountPatronRules.ResolveGrade(1, 2, 6, grantStacked: true, forceMaxGrade: false))
            .IsEqualTo(2u);
        await Assert.That(AccountPatronRules.ResolveGrade(4, 2, 6, grantStacked: true, forceMaxGrade: false))
            .IsEqualTo(4u);
        await Assert.That(AccountPatronRules.ResolveGrade(1, 2, 6, grantStacked: true, forceMaxGrade: true))
            .IsEqualTo(6u);
        await Assert.That(AccountPatronRules.ResolveGrade(1, 2, 6, grantStacked: false, forceMaxGrade: false))
            .IsEqualTo(1u);
    }

    [Test]
    public async Task WithStacked_AddsBothMembershipsOnce()
    {
        var added = AccountPatronRules.WithStacked([], true).ToArray();
        await Assert.That(added).IsEquivalentTo(Stacked);

        var kept = AccountPatronRules.WithStacked([(uint)AccountMembership.Ancient], true).ToArray();
        await Assert.That(kept).IsEquivalentTo(Stacked);

        var off = AccountPatronRules.WithStacked([], false).ToArray();
        await Assert.That(off).IsEmpty();
    }

    [Test]
    public async Task Schedule_EveryRowAlwaysShows()
    {
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Every, 0, 1, [], 2, isPcBang: false)).IsTrue();
    }

    [Test]
    public async Task Schedule_FreeRowHidesForStackedPatron()
    {
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Free, 0, 2, Stacked, 2, isPcBang: false)).IsFalse();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Free, 0, 1, [], 2, isPcBang: false)).IsTrue();
    }

    [Test]
    public async Task Schedule_PremiumRowNeedsAPaidGradeOrMembership()
    {
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Premium, 0, 2, Stacked, 2, isPcBang: false)).IsTrue();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Premium, 6, 2, Stacked, 2, isPcBang: false)).IsFalse();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Premium, 2, 2, Stacked, 2, isPcBang: false)).IsTrue();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.Premium, 0, 1, [], 2, isPcBang: false)).IsFalse();
    }

    [Test]
    public async Task Schedule_AccountBuffRowNeedsThatMembership()
    {
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.AccountBuff, 1001, 2, Stacked, 2, isPcBang: false)).IsTrue();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.AccountBuff, 1001, 2, [], 2, isPcBang: false)).IsFalse();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.PcBang, 0, 2, Stacked, 2, isPcBang: false)).IsFalse();
        await Assert.That(AccountPatronRules.IsScheduleEligible(
            (int)ScheduleItemKind.PcBang, 0, 2, Stacked, 2, isPcBang: true)).IsTrue();
    }

    [Test]
    public async Task WantedBuffIds_StackedKeepsGradeAndLifeMembership()
    {
        uint[] family = [7149, 7151, 7152, 7153];
        uint[] membership = [7149, 7150];

        await Assert.That(AccountPatronRules.WantedBuffIds(7149, membership, family))
            .IsEquivalentTo(new uint[] { 7149, 7150 });
        await Assert.That(AccountPatronRules.WantedBuffIds(7153, membership, family))
            .IsEquivalentTo(new uint[] { 7153, 7150 });
        await Assert.That(AccountPatronRules.WantedBuffIds(0, [7150], family))
            .IsEquivalentTo(new uint[] { 7150 });
        await Assert.That(AccountPatronRules.WantedBuffIds(0, [], family)).IsEmpty();
    }

    [Test]
    public async Task KnownPatronBuffIds_UnionsGradeAndMembership()
    {
        var known = AccountPatronRules.KnownPatronBuffIds([7149, 7153], [7149, 7150]);
        await Assert.That(known).IsEquivalentTo(new uint[] { 7149, 7153, 7150 });
    }

    [Test]
    public async Task PermanentMembership_UsesUnixEpochSoTheIconHasNoDayCount()
    {
        await Assert.That(AccountPatronRules.PermanentAttributeTime).IsEqualTo(DateTime.UnixEpoch);

        var permanent = new AccountAttribute { Expires = AccountPatronRules.PermanentAttributeTime };
        await Assert.That(permanent.IsExpired).IsFalse();

        var lapsed = new AccountAttribute { Expires = DateTime.UtcNow.AddDays(-1) };
        await Assert.That(lapsed.IsExpired).IsTrue();
    }

    [Test]
    public async Task AuctionFeeDiscount_FollowsUseAuctionConfigNotListingGrant()
    {
        var stacked = Stacked;
        await Assert.That(AccountPatronRules.HasAuctionFeeDiscount(
            stacked, id => id == (uint)AccountMembership.Advanced)).IsTrue();
        await Assert.That(AccountPatronRules.HasAuctionFeeDiscount(
            [(uint)AccountMembership.Ancient], id => id == (uint)AccountMembership.Advanced)).IsFalse();
        await Assert.That(AccountPatronRules.HasAuctionFeeDiscount(
            [], id => id == (uint)AccountMembership.Advanced)).IsFalse();
        await Assert.That(AccountPatronRules.HasAuctionFeeDiscount(stacked, null)).IsFalse();
    }
}
