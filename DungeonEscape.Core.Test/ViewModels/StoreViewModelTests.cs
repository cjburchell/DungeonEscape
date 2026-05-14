using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.ViewModels;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class StoreViewModelTests
    {
        [Fact]
        public void ResetSelectsBuyItemsAndFirstRows()
        {
            var viewModel = new StoreViewModel();
            viewModel.SetCurrentTab(StoreTab.Sell, true);
            viewModel.SetCurrentFocus(StoreFocus.SellMembers);
            viewModel.SetSelectedHeroIndex(2);
            viewModel.SetSelectedBuyIndex(3);
            viewModel.SetSelectedSellIndex(4);

            viewModel.Reset();

            Assert.Equal(StoreTab.Buy, viewModel.CurrentTab);
            Assert.Equal(StoreFocus.Items, viewModel.CurrentFocus);
            Assert.Equal(0, viewModel.SelectedHeroIndex);
            Assert.Equal(0, viewModel.SelectedBuyIndex);
            Assert.Equal(0, viewModel.SelectedSellIndex);
        }

        [Fact]
        public void SetCurrentTabRejectsSellWhenStoreWillNotBuy()
        {
            var viewModel = new StoreViewModel();

            var changed = viewModel.SetCurrentTab(StoreTab.Sell, false);

            Assert.False(changed);
            Assert.Equal(StoreTab.Buy, viewModel.CurrentTab);
        }

        [Fact]
        public void SetSelectedHeroIndexResetsSellSelection()
        {
            var viewModel = new StoreViewModel();
            viewModel.SetSelectedSellIndex(5);

            var changed = viewModel.SetSelectedHeroIndex(1);

            Assert.True(changed);
            Assert.Equal(1, viewModel.SelectedHeroIndex);
            Assert.Equal(0, viewModel.SelectedSellIndex);
        }

        [Fact]
        public void ClampSelectionsKeepIndexesInRange()
        {
            var viewModel = new StoreViewModel();
            viewModel.SetSelectedBuyIndex(5);
            viewModel.SetSelectedHeroIndex(5);
            viewModel.SetSelectedSellIndex(5);

            viewModel.ClampBuySelection(new List<Item> { new Item(), new Item() });
            viewModel.ClampHeroSelection(new List<Hero> { new Hero() });
            viewModel.ClampSellSelection(new List<ItemInstance>());

            Assert.Equal(1, viewModel.SelectedBuyIndex);
            Assert.Equal(0, viewModel.SelectedHeroIndex);
            Assert.Equal(0, viewModel.SelectedSellIndex);
        }

        [Fact]
        public void GetSelectedItemsReturnClampedSelection()
        {
            var viewModel = new StoreViewModel();
            var potion = new Item { Name = "Potion" };
            var sword = new Item { Name = "Sword" };
            viewModel.SetSelectedBuyIndex(10);

            var selected = viewModel.GetSelectedBuyItem(new List<Item> { potion, sword });

            Assert.Same(sword, selected);
            Assert.Equal(1, viewModel.SelectedBuyIndex);
        }

        [Fact]
        public void DelegatesStoreMetadataAndSellableFiltering()
        {
            var viewModel = new StoreViewModel();
            var store = new TiledObjectInfo { Name = "#Random#" };
            var hero = new Hero { Items = new List<ItemInstance>() };
            var sellable = new ItemInstance(new Item { Name = "Sword", Type = ItemType.Weapon, Cost = 100 });
            hero.Items.Add(sellable);
            hero.Items.Add(new ItemInstance(new Item { Name = "Gold", Type = ItemType.Gold, Cost = 1 }));

            Assert.Equal("Store", viewModel.GetStoreName(store));
            Assert.True(viewModel.StoreWillBuyItems(store));
            Assert.Equal(new[] { sellable }, viewModel.GetSellableItems(hero));
            Assert.Equal(75, viewModel.GetSalePrice(sellable));
        }
    }
}
