using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class GameMenuViewModel
    {
        public const int ScreenItems = 1;
        public const int ScreenSpells = 2;
        public const int ScreenEquipment = 3;
        public const int ScreenAbilities = 4;
        public const int ScreenStatus = 5;
        public const int ScreenParty = 7;
        public const int SettingsGeneral = 0;
        public const int SettingsUi = 1;
        public const int SettingsInput = 2;
        public const int SettingsDebug = 3;

        public int CurrentScreen { get; private set; }
        public int PreviousScreen { get; private set; }
        public int CurrentFocus { get; private set; }
        public int CurrentTab { get; private set; }
        public int CurrentSettingsTab { get; private set; }
        public int CurrentPartyDetailTab { get; private set; }
        public int SelectedHeroIndex { get; private set; }
        public int SelectedPartyItemIndex { get; private set; }
        public int SelectedDetailIndex { get; private set; }
        public int SelectedEquipmentItemIndex { get; private set; }
        public int SelectedMainActionIndex { get; private set; }
        public int SelectedPreviousScreenRowIndex { get; private set; }
        public int DetailPageIndex { get; private set; }
        public int SelectedRowIndex { get; private set; }
        public int SelectedBindingSlotIndex { get; private set; }

        public void Reset()
        {
            CurrentScreen = 0;
            PreviousScreen = 0;
            CurrentFocus = 0;
            CurrentTab = 0;
            CurrentSettingsTab = 0;
            CurrentPartyDetailTab = 0;
            SelectedHeroIndex = 0;
            SelectedPartyItemIndex = 0;
            SelectedDetailIndex = 0;
            SelectedEquipmentItemIndex = 0;
            SelectedMainActionIndex = 0;
            SelectedPreviousScreenRowIndex = 0;
            DetailPageIndex = 0;
            SelectedRowIndex = 0;
            SelectedBindingSlotIndex = 0;
        }

        public int ClampSelectedRowIndex(int rowCount)
        {
            SelectedRowIndex = Clamp(SelectedRowIndex, 0, rowCount <= 0 ? 0 : rowCount - 1);
            return SelectedRowIndex;
        }

        public int ClampSelectedDetailIndex(int detailCount)
        {
            SelectedDetailIndex = Clamp(SelectedDetailIndex, 0, detailCount <= 0 ? 0 : detailCount - 1);
            return SelectedDetailIndex;
        }

        public int ClampSelectedEquipmentItemIndex(int itemCount)
        {
            SelectedEquipmentItemIndex = Clamp(SelectedEquipmentItemIndex, 0, itemCount <= 0 ? 0 : itemCount - 1);
            return SelectedEquipmentItemIndex;
        }

        public int ClampSelectedMainActionIndex(int actionCount)
        {
            SelectedMainActionIndex = Clamp(SelectedMainActionIndex, 0, actionCount <= 0 ? 0 : actionCount - 1);
            return SelectedMainActionIndex;
        }

        public int MoveSelectedRowIndex(int delta, int rowCount)
        {
            SelectedRowIndex = rowCount <= 0 ? 0 : Clamp(SelectedRowIndex + delta, 0, rowCount - 1);
            return SelectedRowIndex;
        }

        public int MoveSelectedDetailIndex(int delta, int detailCount, int pageSize)
        {
            SelectedDetailIndex = detailCount <= 0 ? 0 : Clamp(SelectedDetailIndex + delta, 0, detailCount - 1);
            DetailPageIndex = pageSize <= 0 ? 0 : SelectedDetailIndex / pageSize;
            return SelectedDetailIndex;
        }

        public int MoveSelectedEquipmentItemIndex(int delta, int itemCount)
        {
            SelectedEquipmentItemIndex = itemCount <= 0 ? 0 : Clamp(SelectedEquipmentItemIndex + delta, 0, itemCount - 1);
            return SelectedEquipmentItemIndex;
        }

        public int SelectDetailPage(int pageIndex, int detailCount, int pageSize)
        {
            DetailPageIndex = pageIndex < 0 ? 0 : pageIndex;
            SelectedDetailIndex = detailCount <= 0 ? 0 : Clamp(DetailPageIndex * pageSize, 0, detailCount - 1);
            return SelectedDetailIndex;
        }

        public int GetSettingsSelectableRowCount(int settingsTab, int bindingCount)
        {
            switch (settingsTab)
            {
                case SettingsGeneral:
                    return 8;
                case SettingsUi:
                    return 9;
                case SettingsInput:
                    return Math.Max(0, bindingCount) + 2;
                case SettingsDebug:
                    return 6;
                default:
                    return 0;
            }
        }

        public List<Hero> GetInventoryMembers(Party party)
        {
            return party == null
                ? new List<Hero>()
                : party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
        }

        public List<Hero> GetMenuMembers(
            Party party,
            int screen,
            Func<Hero, bool> canUseMapSpells,
            Func<Hero, bool> canUseMapSkills)
        {
            var members = GetInventoryMembers(party);
            switch (screen)
            {
                case ScreenSpells:
                    return members.Where(member => canUseMapSpells != null && canUseMapSpells(member)).ToList();
                case ScreenAbilities:
                    return members.Where(member => canUseMapSkills != null && canUseMapSkills(member)).ToList();
                default:
                    return members;
            }
        }

        public Hero GetSelectedMenuHero(IList<Hero> members)
        {
            return members != null && SelectedRowIndex >= 0 && SelectedRowIndex < members.Count
                ? members[SelectedRowIndex]
                : null;
        }

        public bool AnyMemberMatches(Party party, Func<Hero, bool> predicate)
        {
            return party != null && predicate != null && GetInventoryMembers(party).Any(predicate);
        }

        public bool CanManagePartyMembers(Party party)
        {
            return party != null && GetInventoryMembers(party).Count > 1;
        }

        public int GetCurrentDetailCount(int screen, Hero hero, int knownSpellCount, int knownSkillCount)
        {
            if (hero == null)
            {
                return 0;
            }

            switch (screen)
            {
                case ScreenItems:
                    return hero.Items == null ? 0 : hero.Items.Count;
                case ScreenSpells:
                    return Math.Max(0, knownSpellCount);
                case ScreenAbilities:
                    return Math.Max(0, knownSkillCount);
                case ScreenEquipment:
                    return GetEquipmentSlots().Count;
                default:
                    return 0;
            }
        }

        public List<Slot> GetEquipmentSlots()
        {
            return Enum.GetValues(typeof(Slot)).Cast<Slot>().ToList();
        }

        public ItemInstance GetEquippedItem(Hero hero, Slot slot)
        {
            if (hero == null || hero.Slots == null || hero.Items == null)
            {
                return null;
            }

            string itemId;
            if (!hero.Slots.TryGetValue(slot, out itemId) || string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return hero.Items.FirstOrDefault(item => item != null && item.Id == itemId);
        }

        public List<ItemInstance> GetEquipmentCandidates(Hero hero, Slot slot)
        {
            var candidates = new List<ItemInstance>();
            if (hero == null || hero.Items == null)
            {
                return candidates;
            }

            var equipped = GetEquippedItem(hero, slot);
            if (equipped != null)
            {
                candidates.Add(equipped);
            }

            candidates.AddRange(hero.Items.Where(item =>
                item != null &&
                !item.IsEquipped &&
                item.Slots != null &&
                item.Slots.Contains(slot) &&
                hero.CanEquipItem(item)));
            return candidates;
        }

        public List<string> GetMainActions(bool hasMapSpells, bool hasMapAbilities, bool canManageParty)
        {
            var actions = new List<string> { "Items" };
            if (hasMapSpells)
            {
                actions.Add("Spells");
            }

            actions.Add("Equipment");
            if (hasMapAbilities)
            {
                actions.Add("Abilities");
            }

            actions.Add("Status");
            actions.Add("Quests");
            if (canManageParty)
            {
                actions.Add("Party");
            }

            actions.Add("Misc.");
            return actions;
        }

        public int GetSaveSelectableRowCount(int manualSaveSlotCount)
        {
            return Math.Max(0, manualSaveSlotCount);
        }

        public int GetLoadSelectableRowCount(int manualSaveCount)
        {
            return Math.Max(0, manualSaveCount);
        }

        public void SetCurrentScreen(int value)
        {
            CurrentScreen = value;
        }

        public void SetPreviousScreen(int value)
        {
            PreviousScreen = value;
        }

        public void SetCurrentFocus(int value)
        {
            CurrentFocus = value;
        }

        public void SetCurrentTab(int value)
        {
            CurrentTab = value;
        }

        public void SetCurrentSettingsTab(int value)
        {
            CurrentSettingsTab = value;
        }

        public void SetCurrentPartyDetailTab(int value)
        {
            CurrentPartyDetailTab = value;
        }

        public void SetSelectedHeroIndex(int value)
        {
            SelectedHeroIndex = value;
        }

        public void SetSelectedPartyItemIndex(int value)
        {
            SelectedPartyItemIndex = value;
        }

        public void SetSelectedDetailIndex(int value)
        {
            SelectedDetailIndex = value;
        }

        public void SetSelectedEquipmentItemIndex(int value)
        {
            SelectedEquipmentItemIndex = value;
        }

        public void SetSelectedMainActionIndex(int value)
        {
            SelectedMainActionIndex = value;
        }

        public void SetSelectedPreviousScreenRowIndex(int value)
        {
            SelectedPreviousScreenRowIndex = value;
        }

        public void SetDetailPageIndex(int value)
        {
            DetailPageIndex = value;
        }

        public void SetSelectedRowIndex(int value)
        {
            SelectedRowIndex = value;
        }

        public void SetSelectedBindingSlotIndex(int value)
        {
            SelectedBindingSlotIndex = value;
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
