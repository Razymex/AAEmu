using AAEmu.Game.Models.Game.ScheduleItems;

namespace AAEmu.Game.Models.Game;

/// <summary>
/// Patron is two memberships stacked, not a point-grade id. Compact
/// <c>premium_benefit_lists</c> column 3 is 上古会员 (1001) plus 生活会员 (1002).
/// </summary>
public static class AccountPatronRules
{
    /// <summary>上古会员 + 生活会员, the stacked benefit column.</summary>
    public static IReadOnlyList<uint> StackedMemberships { get; } =
        [(uint)AccountMembership.Ancient, (uint)AccountMembership.Advanced];

    /// <summary>
    /// Wire form for a membership that does not expire. The client paints remaining days on the
    /// matching buff icon from this timestamp; a real end date (the old 2030 payment window)
    /// showed a day count. Zero matches the listing grant.
    /// </summary>
    public static readonly DateTime PermanentAttributeTime = DateTime.UnixEpoch;

    public static bool IsEnabled(bool grantStacked, bool forceMaxGrade) =>
        grantStacked || forceMaxGrade;

    /// <summary>
    /// Point total after the server floor. A stacked grant moves a free account onto the first
    /// paid <c>premium_grades</c> row (the 15/10/6000 labor base). Force-max uses that table's
    /// highest threshold instead.
    /// </summary>
    public static int ResolvePoint(int point, int paidFloorPoint, int maxGradePoint, bool grantStacked, bool forceMaxGrade)
    {
        var value = Math.Max(0, point);
        if (forceMaxGrade)
            return Math.Max(value, Math.Max(0, maxGradePoint));
        if (grantStacked)
            return Math.Max(value, Math.Max(0, paidFloorPoint));
        return value;
    }

    public static uint ResolveGrade(uint gradeFromPoints, uint paidFloorGradeId, uint maxGradeId, bool grantStacked, bool forceMaxGrade)
    {
        if (forceMaxGrade && maxGradeId > 0)
            return maxGradeId;
        if (grantStacked && paidFloorGradeId > gradeFromPoints)
            return paidFloorGradeId;
        return gradeFromPoints;
    }

    public static bool HasAncient(IEnumerable<uint> membershipIds) =>
        membershipIds != null && membershipIds.Contains((uint)AccountMembership.Ancient);

    public static bool HasAdvanced(IEnumerable<uint> membershipIds) =>
        membershipIds != null && membershipIds.Contains((uint)AccountMembership.Advanced);

    public static bool HasStacked(IEnumerable<uint> membershipIds) =>
        HasAncient(membershipIds) && HasAdvanced(membershipIds);

    /// <summary>
    /// Extra attendance rewards follow 上古会员, not the payment-window subscription flag.
    /// </summary>
    public static bool IsArcheLife(IEnumerable<uint> membershipIds) => HasAncient(membershipIds);

    public static bool IsPaidPatron(uint premiumGrade, IReadOnlyCollection<uint> membershipIds, uint paidFloorGradeId)
    {
        if (paidFloorGradeId > 0 && premiumGrade >= paidFloorGradeId)
            return true;
        return HasAncient(membershipIds) || HasAdvanced(membershipIds);
    }

    public static IEnumerable<uint> WithStacked(IEnumerable<uint> membershipIds, bool grantStacked)
    {
        var ids = membershipIds?.ToList() ?? [];
        if (!grantStacked)
            return ids;

        foreach (var membership in StackedMemberships)
        {
            if (!ids.Contains(membership))
                ids.Add(membership);
        }

        return ids;
    }

    /// <summary>
    /// Character buffs this Patron set should hold. The current grade buff wins inside
    /// the <c>premium_grades</c> family so a higher ArcheLife row does not also keep 7149.
    /// Membership buffs outside that family (生活会员 7150) stay.
    /// </summary>
    public static IReadOnlyList<uint> WantedBuffIds(
        uint gradeBuffId,
        IEnumerable<uint> membershipBuffIds,
        IEnumerable<uint> gradeFamilyBuffIds)
    {
        var family = (gradeFamilyBuffIds ?? []).Where(id => id > 0).ToHashSet();
        var wanted = new List<uint>();

        void Add(uint id)
        {
            if (id == 0 || wanted.Contains(id))
                return;
            wanted.Add(id);
        }

        Add(gradeBuffId);
        if (membershipBuffIds == null)
            return wanted;

        foreach (var id in membershipBuffIds)
        {
            if (family.Contains(id) && id != gradeBuffId)
                continue;
            Add(id);
        }

        return wanted;
    }

    public static IReadOnlyList<uint> KnownPatronBuffIds(
        IEnumerable<uint> gradeFamilyBuffIds,
        IEnumerable<uint> membershipBuffIds)
    {
        return (gradeFamilyBuffIds ?? [])
            .Concat(membershipBuffIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Auction charge/deposit cuts follow <c>account_buffs.use_auction_config</c>, not the
    /// AuctionPost listing grant that every client already receives.
    /// </summary>
    public static bool HasAuctionFeeDiscount(IEnumerable<uint> membershipIds, Func<uint, bool> usesAuctionConfig)
    {
        if (membershipIds == null || usesAuctionConfig == null)
            return false;
        return membershipIds.Any(usesAuctionConfig);
    }

    public static bool IsScheduleEligible(
        int kind,
        int kindValue,
        uint premiumGrade,
        IReadOnlyCollection<uint> membershipIds,
        uint paidFloorGradeId,
        bool isPcBang)
    {
        return (ScheduleItemKind)kind switch
        {
            ScheduleItemKind.Every => true,
            ScheduleItemKind.PcBang => isPcBang,
            ScheduleItemKind.Premium => IsPaidPatron(premiumGrade, membershipIds, paidFloorGradeId)
                                        && (kindValue <= 0 || kindValue == premiumGrade),
            ScheduleItemKind.Free => !IsPaidPatron(premiumGrade, membershipIds, paidFloorGradeId),
            ScheduleItemKind.AccountBuff => membershipIds != null && membershipIds.Contains((uint)kindValue),
            _ => false
        };
    }
}
