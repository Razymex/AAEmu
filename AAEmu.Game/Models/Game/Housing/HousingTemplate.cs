using AAEmu.Game.Models.Game.Taxations;

namespace AAEmu.Game.Models.Game.Housing;

public class HousingTemplate
{
    public uint Id { get; init; }
    public string Name { get; set; }
    public uint CategoryId { get; set; }
    public uint MainModelId { get; init; }
    public uint DoorModelId { get; init; }
    public uint StairModelId { get; init; }
    public bool AutoZ { get; init; }
    public bool GateExists { get; init; }
    public int Hp { get; init; }
    public uint RepairCost { get; init; }
    public string Family { get; init; }
    public Taxation Taxation { get; set; }
    /// <summary>Use Taxation instead of TaxationId for functions in the server</summary>
    internal uint TaxationId { get; init; }
    public uint GuardTowerSettingId { get; init; }
    public float CinemaRadius { get; init; }
    public float AutoZOffsetX { get; init; }
    public float AutoZOffsetY { get; init; }
    public float AutoZOffsetZ { get; init; }
    public float Alley { get; init; }
    public float ExtraHeightAbove { get; init; }
    public float ExtraHeightBelow { get; init; }
    public uint DecoLimit { get; init; }
    public uint AbsoluteDecoLimit { get; init; }
    public uint HousingDecoLimitId { get; init; }
    public bool IsSellable { get; init; }
    public bool HeavyTax { get; init; }
    public bool AlwaysPublic { get; init; }
    public uint RotateItemId { get; init; }
    public uint RotateItemCount { get; init; }
    /// <summary>10.x plot radius from housing_sizes via housing_size_id (garden_radius left the housings table).</summary>
    public float GardenRadius { get; set; }
    internal uint HousingSizeId { get; init; }

    public Dictionary<int, HousingBuildStep> BuildSteps { get; } = [];
    public HousingBindingDoodad[] HousingBindingDoodad { get; set; }
}
