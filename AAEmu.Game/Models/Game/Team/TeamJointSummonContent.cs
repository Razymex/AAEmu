using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData;

namespace AAEmu.Game.Models.Game.Team;

/// <summary>
/// Catalog keys for the raid-team summon and joint follow-ups. Every numeric id and timing is
/// read from compact at load; none of them is written here.
/// </summary>
/// <remarks>
/// The shipped rows behind these keys (verified read-only, 10.0.2.13):
/// <list type="bullet">
/// <item><c>const_item_types</c> <c>team_summon</c> — the flame a raid leader spends. Its own
/// description says the flame is consumed once the summon succeeds.</item>
/// <item><c>const_skill_types</c> <c>team_summon</c> — the attempt skill: 1000 ms cooldown,
/// 3000 ms cast, 4 m range.</item>
/// <item><c>const_skill_types</c> <c>team_summon_channeling</c> — the channelled follow-through:
/// 60000 ms channel, channeling buff 23368.</item>
/// </list>
/// A missing row is not a zero: <see cref="TryResolve"/> refuses, and the caller must refuse too.
/// </remarks>
public static class TeamJointSummonContent
{
    /// <summary><c>const_item_types.name</c> — the summon flame.</summary>
    public const string SummonItemConstName = "team_summon";

    /// <summary><c>const_skill_types.name</c> — the attempt skill.</summary>
    public const string SummonSkillConstName = "team_summon";

    /// <summary><c>const_skill_types.name</c> — the channelled follow-through.</summary>
    public const string ChannelingSkillConstName = "team_summon_channeling";

    /// <summary>
    /// <c>ui_texts.key</c> holding the client's list of states that forbid a summon. The shipped row
    /// enumerates seven: dungeon entry, trial, imprisonment, siege participation, incapacitated,
    /// death, and a worn backpack. <see cref="TeamSummonRefusal"/> mirrors exactly this list.
    /// </summary>
    public const string RefusalNoticeTextKey = "team_summon_notice";

    /// <summary><c>ui_texts.key</c> — the heading above the refusal list.</summary>
    public const string RefusalCautionTextKey = "team_summon_caution";

    /// <summary><c>ui_texts.key</c> — the recipient-facing question ("will you move?").</summary>
    public const string MoveQuestionTextKey = "team_summon_move";

    /// <summary><c>ui_texts.key</c> — the summoner-facing question ("to your location?").</summary>
    public const string SummonerQuestionTextKey = "team_summon_question";

    /// <summary><c>ui_texts.key</c> — "the dice bidding method is reset after accepting".</summary>
    public const string DiceBidResetTextKey = "raid_joint_warning";

    /// <summary><c>ui_texts.key</c> — "loot acquisition and distribution are reset" on joint.</summary>
    public const string LootResetOnJointTextKey = "raid_jointed";

    /// <summary><c>ui_texts.key</c> — "loot acquisition and distribution are reset" on dissolve.</summary>
    public const string LootResetOnDissolveTextKey = "raid_joint_dismissed";

    /// <summary><c>ui_texts.key</c> — a jointed raid cannot enter an instance.</summary>
    public const string CannotEnterInstanceTextKey = "RAID_JOINTED_CANNOT_ENTER_INSTANCE";

    /// <summary><c>ui_texts.key</c> — the raid's state changed, so it can no longer joint.</summary>
    public const string StatusChangedTextKey = "RAID_JOINT_FAILED_STATUS_CHANGED";

    /// <summary><c>ui_texts.key</c> — the raid was already dissolved.</summary>
    public const string DismissedTextKey = "raid_joint_dismiss";

    public static uint SummonItemId => ItemManager.Instance.GetConstItemId(SummonItemConstName);

    public static uint SummonSkillId => SkillManager.Instance.GetConstSkillId(SummonSkillConstName);

    public static uint ChannelingSkillId => SkillManager.Instance.GetConstSkillId(ChannelingSkillConstName);

    /// <summary>Everything the summon flow needs from compact, resolved once.</summary>
    public sealed record SummonTemplate(
        uint ItemId,
        uint SkillId,
        uint ChannelingSkillId,
        uint ChannelingBuffId,
        int CooldownMilliseconds,
        int CastingMilliseconds,
        int ChannelingMilliseconds,
        int MaxRange);

    /// <summary>
    /// Reads the two skills and the item out of compact. A missing row is refused loudly rather
    /// than defaulted: a summon with no cost, no cooldown or no channel is a different feature.
    /// </summary>
    public static bool TryResolve(out SummonTemplate template, out string reason)
    {
        template = null;
        reason = null;

        var itemId = SummonItemId;
        if (itemId == 0)
        {
            reason = $"const_item_types '{SummonItemConstName}' is missing";
            return false;
        }

        var skillId = SummonSkillId;
        var skill = skillId == 0 ? null : SkillManager.Instance.GetSkillTemplate(skillId);
        if (skill == null)
        {
            reason = $"const_skill_types '{SummonSkillConstName}' is missing";
            return false;
        }

        var channelingId = ChannelingSkillId;
        var channeling = channelingId == 0 ? null : SkillManager.Instance.GetSkillTemplate(channelingId);
        if (channeling == null)
        {
            reason = $"const_skill_types '{ChannelingSkillConstName}' is missing";
            return false;
        }

        if (skill.CooldownTime <= 0)
        {
            reason = $"skill {skillId} has no cooldown";
            return false;
        }

        if (channeling.ChannelingTime <= 0)
        {
            reason = $"skill {channelingId} has no channeling time";
            return false;
        }

        if (channeling.ChannelingBuffId == 0)
        {
            reason = $"skill {channelingId} has no channeling buff";
            return false;
        }

        template = new SummonTemplate(
            itemId,
            skillId,
            channelingId,
            channeling.ChannelingBuffId,
            skill.CooldownTime,
            skill.CastingTime,
            channeling.ChannelingTime,
            skill.MaxRange);
        return true;
    }
}

/// <summary>
/// Reads the summon template on demand. The manager takes this as a seam so a test can decide
/// whether content is present without a container, which is the same reason the team access is an
/// interface.
/// </summary>
public interface ITeamSummonContent
{
    bool TryResolve(out TeamJointSummonContent.SummonTemplate template, out string reason);
}

/// <summary>The real reader, straight out of compact.</summary>
public sealed class CompactTeamSummonContent : ITeamSummonContent
{
    public bool TryResolve(out TeamJointSummonContent.SummonTemplate template, out string reason) =>
        TeamJointSummonContent.TryResolve(out template, out reason);
}
