using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// CS SkillCastExtra type 25. Payload is the Bless Uthstin page as one byte (0-based).
/// Echoed as <see cref="SkillObjectType.None"/> on SC — the page is already stashed from CSStartSkill.
/// </summary>
public class SkillObjectBlessUthstinPage : SkillObject
{
    public byte PageIndex { get; set; }

    public override void Read(PacketStream stream)
    {
        PageIndex = stream.ReadByte();
    }

    public override PacketStream Write(PacketStream stream)
    {
        base.Write(stream);
        stream.Write(PageIndex);
        return stream;
    }
}
