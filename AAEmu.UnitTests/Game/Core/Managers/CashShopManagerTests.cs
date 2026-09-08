using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.CashShop;

namespace AAEmu.UnitTests.Game.Core.Managers;

public class CashShopManagerTests
{
    [Test]
    public void DisableShop_CallsGetAllCharacters()
    {
        var mockWorld = Mock.Of<IWorldManager>();
        mockWorld.GetAllCharacters().Returns([]);
        var manager = new CashShopManager(mockWorld.Object, Mock.Of<IAccountManager>().Object, Mock.Of<ILocalizationManager>().Object, Mock.Of<IItemManager>().Object);
        manager.DisableShop();

        mockWorld.GetAllCharacters().WasCalled(Times.Once);
    }

    [Test]
    public async Task HasCatalog_NeedsMenusShopsAndSkus()
    {
        var manager = new CashShopManager(
            Mock.Of<IWorldManager>().Object,
            Mock.Of<IAccountManager>().Object,
            Mock.Of<ILocalizationManager>().Object,
            Mock.Of<IItemManager>().Object);

        await Assert.That(manager.HasCatalog).IsFalse();

        manager.MenuItems.Add(new IcsMenu());
        manager.ShopItems[1] = new IcsItem();
        manager.SKUs[1] = new IcsSku();

        await Assert.That(manager.HasCatalog).IsTrue();
    }
}