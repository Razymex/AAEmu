namespace AAEmu.Game.Models.Game.Taxations;

public class Taxation
{
    public uint Id { get; set; }
    public uint Tax { get; set; }
    public bool Show { get; set; }
    /// <summary>10.x: appraisal seals per sale, from taxations.seal_count.</summary>
    public uint SealCount { get; set; }
}
