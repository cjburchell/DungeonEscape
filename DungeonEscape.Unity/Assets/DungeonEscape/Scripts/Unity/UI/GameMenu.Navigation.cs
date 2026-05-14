using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private void UpdateGamepadMenuNavigation()
        {
            if (!isOpen)
            {
                ResetMenuNavigationRepeat();
                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                MoveMenuSelection(moveY);
            }

            var moveX = currentScreen == MenuScreen.Settings && selectedRowIndex == 0
                ? GetSettingsTabMoveX()
                : GetMenuMoveX();
            if (moveX == 0)
            {
                return;
            }

            AdjustMenuSelection(moveX);
        }

        private void HandleMenuPageCommand(int delta)
        {
            if (currentScreen == MenuScreen.Settings)
            {
                CycleSettingsTab(delta);
                return;
            }

            if (IsPagedDetailScreen(currentScreen))
            {
                ChangeDetailPage(delta);
            }
        }

        private void CancelCurrentScreen()
        {
            if (currentFocus == MenuFocus.Detail)
            {
                currentFocus = MenuFocus.Primary;
                selectedDetailIndex = 0;
                selectedEquipmentItemIndex = 0;
                return;
            }

            if (currentFocus == MenuFocus.SubDetail)
            {
                currentFocus = MenuFocus.Detail;
                selectedEquipmentItemIndex = 0;
                return;
            }

            if (currentScreen != MenuScreen.Main)
            {
                if (previousScreen != MenuScreen.Main)
                {
                    currentScreen = previousScreen;
                    previousScreen = MenuScreen.Main;
                    selectedRowIndex = selectedPreviousScreenRowIndex;
                }
                else
                {
                    currentScreen = MenuScreen.Main;
                    selectedRowIndex = GetClampedMainActionIndex();
                }

                selectedDetailIndex = 0;
                selectedEquipmentItemIndex = 0;
                detailPageIndex = 0;
                return;
            }

            isOpen = false;
        }

        private void BlockMenuInteractUntilRelease()
        {
            acceptMenuInteractAfterFrame = Time.frameCount + 1;
            waitForMenuInteractRelease = true;
        }

        private bool CanAcceptMenuInteract()
        {
            if (Time.frameCount <= acceptMenuInteractAfterFrame)
            {
                return false;
            }

            return !waitForMenuInteractRelease;
        }

        private void UpdateMenuInteractRelease()
        {
            if (!waitForMenuInteractRelease || Time.frameCount <= acceptMenuInteractAfterFrame)
            {
                return;
            }

            if (!InputManager.GetCommand(InputCommand.Interact))
            {
                waitForMenuInteractRelease = false;
            }
        }

        private int GetSelectableRowCount()
        {
            var party = GetParty();
            switch (currentScreen)
            {
                case MenuScreen.Main:
                    return MainActionScreen.GetSelectableRowCount();
                case MenuScreen.Misc:
                    return MiscActionScreen.GetSelectableRowCount();
                case MenuScreen.Items:
                case MenuScreen.Spells:
                case MenuScreen.Equipment:
                case MenuScreen.Abilities:
                case MenuScreen.Status:
                case MenuScreen.Party:
                    return party == null ? 0 : GetMenuMembers(party).Count;
                case MenuScreen.Quests:
                    return QuestScreen.GetSelectableRowCount();
                case MenuScreen.Save:
                    return SaveScreen.GetSelectableRowCount();
                case MenuScreen.Load:
                    return LoadScreen.GetSelectableRowCount();
                case MenuScreen.Settings:
                    return SettingsScreen.GetSelectableRowCount();
                default:
                    return 0;
            }
        }

        private int GetSettingsSelectableRowCount()
        {
            switch (currentSettingsTab)
            {
                case SettingsTab.General:
                    return 8;
                case SettingsTab.Ui:
                    return 9;
                case SettingsTab.Input:
                    return InputManager.GetBindings().Length + 2;
                case SettingsTab.Debug:
                    return 6;
                default:
                    return 0;
            }
        }

        private void MoveSelectedRow(int delta)
        {
            var count = GetSelectableRowCount();
            if (count <= 0)
            {
                viewModel.MoveSelectedRowIndex(delta, count);
                return;
            }

            var previousIndex = selectedRowIndex;
            viewModel.MoveSelectedRowIndex(delta, count);
            if (selectedRowIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }

            ScrollActiveListToSelectedRow();
        }

        private void MoveMenuSelection(int delta)
        {
            if (currentFocus == MenuFocus.SubDetail)
            {
                MoveEquipmentCandidateSelection(delta);
                return;
            }

            if (currentFocus == MenuFocus.Detail)
            {
                MoveDetailSelection(delta);
                return;
            }

            MoveSelectedRow(delta);
        }

        private void MoveDetailSelection(int delta)
        {
            var count = GetCurrentDetailCount();
            if (count <= 0)
            {
                viewModel.MoveSelectedDetailIndex(delta, count, 10);
                return;
            }

            var previousIndex = selectedDetailIndex;
            viewModel.MoveSelectedDetailIndex(delta, count, 10);
            if (selectedDetailIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void MoveEquipmentCandidateSelection(int delta)
        {
            var hero = GetSelectedMenuHero();
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                selectedEquipmentItemIndex = 0;
                return;
            }

            var count = GetEquipmentCandidates(hero, slots[selectedDetailIndex]).Count;
            if (count <= 0)
            {
                viewModel.MoveSelectedEquipmentItemIndex(delta, count);
                return;
            }

            var previousIndex = selectedEquipmentItemIndex;
            viewModel.MoveSelectedEquipmentItemIndex(delta, count);
            if (selectedEquipmentItemIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void ScrollActiveListToSelectedRow()
        {
            var y = Mathf.Max(0f, selectedRowIndex * 54f * GetPixelScale());
            switch (currentTab)
            {
                case MenuTab.Party:
                    partyScrollPosition.y = y;
                    break;
                case MenuTab.Inventory:
                    inventoryScrollPosition.y = y;
                    break;
                case MenuTab.Quests:
                    questScrollPosition.y = y;
                    break;
                case MenuTab.Save:
                    saveScrollPosition.y = y;
                    break;
                case MenuTab.Settings:
                    settingsScrollPosition.y = y;
                    break;
            }
        }

        private void AdjustSelectedRow(int delta)
        {
            if (currentTab == MenuTab.Party)
            {
                CyclePartyDetailTab(delta);
                return;
            }

            if (currentTab == MenuTab.Inventory)
            {
                AdjustSelectedInventoryHero(delta);
                return;
            }

            if (currentTab == MenuTab.Settings)
            {
                AdjustSelectedSetting(delta);
            }
        }

        private void CyclePartyDetailTab(int delta)
        {
            var tabs = GetVisiblePartyDetailTabs(GetSelectedPartyHero());
            var currentIndex = tabs.IndexOf(currentPartyDetailTab);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var previousTab = currentPartyDetailTab;
            currentPartyDetailTab = tabs[(currentIndex + delta + tabs.Count) % tabs.Count];
            if (currentPartyDetailTab != previousTab)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void AdjustMenuSelection(int delta)
        {
            if (currentScreen == MenuScreen.Settings)
            {
                SettingsScreen.AdjustSelection(delta);
            }
        }

        private void ChangeDetailPage(int delta)
        {
            var count = GetCurrentDetailCount();
            if (count <= 0)
            {
                detailPageIndex = 0;
                selectedDetailIndex = 0;
                return;
            }

            var maxPage = Math.Max(0, (count - 1) / 10);
            var previousPage = detailPageIndex;
            detailPageIndex = Mathf.Clamp(detailPageIndex + delta, 0, maxPage);
            viewModel.SelectDetailPage(detailPageIndex, count, 10);
            if (detailPageIndex != previousPage)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void ActivateSelectedRow()
        {
            if (currentScreen == MenuScreen.Main)
            {
                MainActionScreen.ActivateSelectedRow();
                return;
            }

            if (currentScreen == MenuScreen.Misc)
            {
                MiscActionScreen.ActivateSelectedRow();
                return;
            }

            if (currentScreen == MenuScreen.Save)
            {
                SaveScreen.ActivateSelectedRow();
                return;
            }

            if (currentScreen == MenuScreen.Load)
            {
                LoadScreen.ActivateSelectedRow();
                return;
            }

            if (currentScreen == MenuScreen.Settings)
            {
                SettingsScreen.ActivateSelectedRow();
                return;
            }

            if (IsMemberDetailScreen(currentScreen))
            {
                ActivateCurrentMemberDetailScreen();
                return;
            }

            if (currentTab == MenuTab.Party)
            {
                ActivateSelectedPartyMember();
                return;
            }

            if (currentTab == MenuTab.Inventory)
            {
                ActivateSelectedInventoryItem();
                return;
            }

            if (currentTab == MenuTab.Settings)
            {
                ActivateSelectedSetting();
                return;
            }

            if (currentTab == MenuTab.Save)
            {
                ActivateSelectedSaveSlot();
            }
        }

        private void OpenMenuScreen(MenuScreen screen)
        {
            previousScreen = currentScreen == MenuScreen.Misc &&
                (screen == MenuScreen.Save || screen == MenuScreen.Load || screen == MenuScreen.Settings)
                ? currentScreen
                : MenuScreen.Main;
            selectedPreviousScreenRowIndex = previousScreen == MenuScreen.Misc ? selectedRowIndex : 0;
            currentScreen = screen;
            currentFocus = MenuFocus.Primary;
            selectedRowIndex = 0;
            selectedDetailIndex = 0;
            selectedEquipmentItemIndex = 0;
            detailPageIndex = 0;
            saveScrollPosition = Vector2.zero;
            settingsScrollPosition = Vector2.zero;
            BlockMenuInteractUntilRelease();
        }

        private int GetClampedMainActionIndex()
        {
            var actions = MainActionScreen.GetActions();
            if (actions.Count == 0)
            {
                selectedMainActionIndex = 0;
                return 0;
            }

            viewModel.ClampSelectedMainActionIndex(actions.Count);
            return selectedMainActionIndex;
        }

        private void ActivateCurrentMemberDetailScreen()
        {
            var hero = GetSelectedMenuHero();
            if (hero == null)
            {
                return;
            }

            if (currentFocus == MenuFocus.Primary)
            {
                if (currentScreen == MenuScreen.Status)
                {
                    return;
                }

                if (currentScreen == MenuScreen.Party)
                {
                    ShowPartyMemberActionModal(hero);
                    return;
                }

                currentFocus = MenuFocus.Detail;
                selectedDetailIndex = 0;
                detailPageIndex = 0;
                BlockMenuInteractUntilRelease();
                return;
            }

            switch (currentScreen)
            {
                case MenuScreen.Items:
                    var items = hero.Items == null ? new List<ItemInstance>() : hero.Items.ToList();
                    if (selectedDetailIndex >= 0 && selectedDetailIndex < items.Count)
                    {
                        ShowPartyItemActionModal(hero, items[selectedDetailIndex]);
                    }
                    break;
                case MenuScreen.Spells:
                    var spells = GetKnownSpells(hero);
                    if (selectedDetailIndex >= 0 && selectedDetailIndex < spells.Count)
                    {
                        ShowSpellTargetPicker(hero, spells[selectedDetailIndex]);
                    }
                    break;
                case MenuScreen.Abilities:
                    ShowPartyMessage("Abilities are shown for reference.");
                    break;
                case MenuScreen.Equipment:
                    if (currentFocus == MenuFocus.Detail)
                    {
                        SelectEquipmentSlot(hero);
                    }
                    else if (currentFocus == MenuFocus.SubDetail)
                    {
                        EquipSelectedEquipmentCandidate(hero);
                    }
                    break;
            }
        }

        private void ActivateSelectedSaveSlot()
        {
            if (gameState == null)
            {
                return;
            }

            if (selectedRowIndex < 0)
            {
                return;
            }

            ShowSaveActionModal(selectedRowIndex);
        }

        private int GetMenuMoveX()
        {
            var pressed = InputManager.GetMoveXDown();
            if (pressed != 0)
            {
                repeatingMenuMoveX = pressed;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = InputManager.GetMoveX();
            if (held == 0)
            {
                repeatingMenuMoveX = 0;
                nextMenuMoveXTime = 0f;
                return 0;
            }

            if (held != repeatingMenuMoveX)
            {
                repeatingMenuMoveX = held;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMenuMoveXTime)
            {
                return 0;
            }

            nextMenuMoveXTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private int GetSettingsTabMoveX()
        {
            var moveX = InputManager.GetMoveX();
            if (moveX == 0)
            {
                heldSettingsTabMoveX = 0;
                return 0;
            }

            if (heldSettingsTabMoveX == moveX)
            {
                return 0;
            }

            heldSettingsTabMoveX = moveX;
            return moveX;
        }

        private int GetMenuMoveY()
        {
            var pressed = InputManager.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMenuMoveY = pressed;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = InputManager.GetMoveY();
            if (held == 0)
            {
                repeatingMenuMoveY = 0;
                nextMenuMoveYTime = 0f;
                return 0;
            }

            if (held != repeatingMenuMoveY)
            {
                repeatingMenuMoveY = held;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMenuMoveYTime)
            {
                return 0;
            }

            nextMenuMoveYTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetMenuNavigationRepeat()
        {
            repeatingMenuMoveX = 0;
            nextMenuMoveXTime = 0f;
            repeatingMenuMoveY = 0;
            nextMenuMoveYTime = 0f;
            heldSettingsTabMoveX = 0;
        }
    }
}
