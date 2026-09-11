using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class NpcInteractionRulesTests
{
    [Test]
    public async Task QuestTalkNpc_UsesNpcTalk()
    {
        await Assert.That(NpcInteractionRules.PrimarySkill(new NpcTemplate { Id = 7816 }, questTalk: true))
            .IsEqualTo(SkillsEnum.NpcTalk);
        await Assert.That(NpcInteractionRules.PrimarySkill(null)).IsEqualTo(0u);
        await Assert.That(NpcInteractionRules.PrimarySkill(new NpcTemplate { Id = 7816 })).IsEqualTo(0u);
    }

    [Test]
    public async Task Merchant_UsesStore()
    {
        await Assert.That(NpcInteractionRules.PrimarySkill(new NpcTemplate { Merchant = true }))
            .IsEqualTo(SkillsEnum.UseStore);
    }

    [Test]
    public async Task Banker_UsesWarehouse()
    {
        await Assert.That(NpcInteractionRules.PrimarySkill(new NpcTemplate { Banker = true }))
            .IsEqualTo(SkillsEnum.UseWarehouse);
    }

    [Test]
    public async Task MerchantQuestTalk_UsesNpcTalk()
    {
        await Assert.That(NpcInteractionRules.PrimarySkill(new NpcTemplate { Merchant = true }, questTalk: true))
            .IsEqualTo(SkillsEnum.NpcTalk);
    }
}
