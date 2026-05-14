using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Rules;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class StoreViewModel
    {
        public StoreTab CurrentTab { get; private set; }
        public StoreFocus CurrentFocus { get; private set; }
        public int SelectedHeroIndex { get; private set; }
        public int SelectedBuyIndex { get; private set; }
        public int SelectedSellIndex { get; private set; }

        public void Reset()
        {
            CurrentTab = StoreTab.Buy;
            CurrentFocus = StoreFocus.Items;
            SelectedHeroIndex = 0;
            SelectedBuyIndex = 0;
            SelectedSellIndex = 0;
        }

        public string GetStoreName(TiledObjectInfo storeObject)
        {
            return StoreRules.GetStoreName(storeObject);
        }

        public string GetStoreText(TiledObjectInfo storeObject)
        {
            return StoreRules.GetStoreText(storeObject);
        }

        public bool StoreWillBuyItems(TiledObjectInfo storeObject)
        {
            return StoreRules.StoreWillBuyItems(storeObject);
        }

        public bool CanBuy(Party party, Item item)
        {
            return StoreRules.CanBuy(party, item);
        }

        public List<Hero> GetBuyRecipients(Party party)
        {
            return StoreRules.GetBuyRecipients(party);
        }

        public List<Hero> GetPartyMembers(Party party)
        {
            return party == null ? new List<Hero>() : party.Members.ToList();
        }

        public List<ItemInstance> GetSellableItems(Hero hero)
        {
            return StoreRules.GetSellableItems(hero).ToList();
        }

        public int GetSalePrice(ItemInstance item)
        {
            return StoreRules.GetSalePrice(item);
        }

        public void ClampBuySelection(IList<Item> inventory)
        {
            SelectedBuyIndex = Clamp(SelectedBuyIndex, 0, Math.Max((inventory == null ? 0 : inventory.Count) - 1, 0));
        }

        public void ClampHeroSelection(IList<Hero> members)
        {
            SelectedHeroIndex = Clamp(SelectedHeroIndex, 0, Math.Max((members == null ? 0 : members.Count) - 1, 0));
        }

        public void ClampSellSelection(IList<ItemInstance> items)
        {
            SelectedSellIndex = Clamp(SelectedSellIndex, 0, Math.Max((items == null ? 0 : items.Count) - 1, 0));
        }

        public bool SetCurrentTab(StoreTab tab, bool storeWillBuyItems)
        {
            if (tab == StoreTab.Sell && !storeWillBuyItems)
            {
                return false;
            }

            if (CurrentTab == tab)
            {
                return false;
            }

            CurrentTab = tab;
            return true;
        }

        public bool SetCurrentFocus(StoreFocus focus)
        {
            if (CurrentFocus == focus)
            {
                return false;
            }

            CurrentFocus = focus;
            return true;
        }

        public bool SetSelectedHeroIndex(int index)
        {
            if (SelectedHeroIndex == index)
            {
                return false;
            }

            SelectedHeroIndex = index;
            SelectedSellIndex = 0;
            return true;
        }

        public bool SetSelectedBuyIndex(int index)
        {
            if (SelectedBuyIndex == index)
            {
                return false;
            }

            SelectedBuyIndex = index;
            return true;
        }

        public bool SetSelectedSellIndex(int index)
        {
            if (SelectedSellIndex == index)
            {
                return false;
            }

            SelectedSellIndex = index;
            return true;
        }

        public Item GetSelectedBuyItem(IList<Item> inventory)
        {
            if (inventory == null || inventory.Count == 0)
            {
                return null;
            }

            ClampBuySelection(inventory);
            return inventory[SelectedBuyIndex];
        }

        public Hero GetSelectedHero(IList<Hero> members)
        {
            if (members == null || members.Count == 0)
            {
                return null;
            }

            ClampHeroSelection(members);
            return members[SelectedHeroIndex];
        }

        public ItemInstance GetSelectedSellItem(IList<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            ClampSellSelection(items);
            return items[SelectedSellIndex];
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
