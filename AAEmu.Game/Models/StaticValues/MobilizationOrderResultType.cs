namespace AAEmu.Game.Models.StaticValues;

/// <summary>
/// mobilization_order.lua's MOBILIZATION_ORDER_RESULT table - the client's own response to the
/// Mobilization Order popup shown to non-issuing faction members (X2Faction:RequestMobilizationOrder).
/// </summary>
public enum MobilizationOrderResultType
{
    Accept = 1,
    Cancel = 2,
    TimeOver = 3,
}
