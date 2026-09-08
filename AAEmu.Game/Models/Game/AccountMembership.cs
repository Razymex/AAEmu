namespace AAEmu.Game.Models.Game;

/// <summary>
/// <c>account_buffs.id</c> values the client treats as paid memberships on an
/// <see cref="AccountAttributeKind.AccountBuff"/> attribute.
/// </summary>
public enum AccountMembership : uint
{
    /// <summary>上古会员 — housing/tax, extra labor, attendance extras.</summary>
    Ancient = 1001,

    /// <summary>生活会员 — vocation/craft and auction-fee column.</summary>
    Advanced = 1002
}
