using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
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
        public string ModalTitle { get; private set; }
        public string ModalMessage { get; private set; }
        public List<string> ModalChoices { get; private set; }
        public List<Hero> ModalChoiceHeroes { get; private set; }
        public int ModalSelectedIndex { get; private set; }
        public bool ModalWaitingForConfirmRelease { get; private set; }

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
            HideModal();
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

        public List<string> GetPartyItemActionLabels(bool canUse, ItemInstance item, bool canEquip, bool hasTransferTarget)
        {
            var choices = new List<string>();
            if (canUse)
            {
                choices.Add("Use");
            }

            if (item != null && item.IsEquipped)
            {
                choices.Add("Unequip");
            }
            else if (canEquip)
            {
                choices.Add("Equip");
            }

            if (hasTransferTarget)
            {
                choices.Add("Transfer");
            }

            if (item != null && item.Type != ItemType.Quest)
            {
                choices.Add("Drop");
            }

            choices.Add("Cancel");
            return choices;
        }

        public GameMenuUseAction GetUseItemAction(ItemInstance item)
        {
            if (item == null)
            {
                return GameMenuUseAction.CannotUse;
            }

            if (item.Item != null && item.Item.Skill != null)
            {
                switch (item.Item.Skill.Type)
                {
                    case SkillType.Outside:
                        return GameMenuUseAction.Outside;
                    case SkillType.Return:
                        return GameMenuUseAction.Return;
                    case SkillType.Open:
                        return GameMenuUseAction.Open;
                }
            }

            return GetTargetUseAction(item.Target);
        }

        public GameMenuUseAction GetCastSpellAction(Spell spell)
        {
            if (spell == null)
            {
                return GameMenuUseAction.CannotUse;
            }

            switch (spell.Type)
            {
                case SkillType.Outside:
                    return GameMenuUseAction.Outside;
                case SkillType.Return:
                    return GameMenuUseAction.Return;
                case SkillType.Open:
                    return GameMenuUseAction.Open;
                default:
                    return GetTargetUseAction(spell.Targets);
            }
        }

        public bool CanCastSpellFromPartyMenu(bool canCastHeroSpell, SkillType type, bool canCastOutside, bool canCastReturn)
        {
            if (!canCastHeroSpell)
            {
                return false;
            }

            if (type == SkillType.Outside)
            {
                return canCastOutside;
            }

            if (type == SkillType.Return)
            {
                return canCastReturn;
            }

            return true;
        }

        public GameMenuSettingsEffect AdjustSelectedSetting(Settings settings, int settingsTab, int selectedRowIndex, int delta)
        {
            if (settings == null)
            {
                return GameMenuSettingsEffect.None;
            }

            if (selectedRowIndex == 0)
            {
                return GameMenuSettingsEffect.CycleTab;
            }

            var rowIndex = selectedRowIndex - 1;
            if (settingsTab == SettingsGeneral)
            {
                switch (rowIndex)
                {
                    case 0:
                        settings.UiScale = ClampFloat((settings.UiScale <= 0f ? 1f : settings.UiScale) + 0.05f * delta, 0.5f, 3f);
                        return GameMenuSettingsEffect.ApplySettings;
                    case 1:
                        settings.DialogTextCharactersPerSecond = ClampFloat(settings.DialogTextCharactersPerSecond + 5f * delta, 0f, 120f);
                        return GameMenuSettingsEffect.ApplySettings;
                    case 3:
                        settings.MusicVolume = ClampFloat(settings.MusicVolume + 0.01f * delta, 0f, 1f);
                        return GameMenuSettingsEffect.ApplyAudioSettings;
                    case 4:
                        settings.SoundEffectsVolume = ClampFloat(settings.SoundEffectsVolume + 0.01f * delta, 0f, 1f);
                        return GameMenuSettingsEffect.ApplyAudioSettings;
                    case 6:
                        settings.AutoSaveIntervalSeconds = ClampFloat((settings.AutoSaveIntervalSeconds <= 0f ? 5f : settings.AutoSaveIntervalSeconds) + 5f * delta, 5f, 300f);
                        return GameMenuSettingsEffect.ApplySettings;
                }
            }
            else if (settingsTab == SettingsUi)
            {
                switch (rowIndex)
                {
                    case 1:
                        settings.UiBackgroundAlpha = ClampFloat(settings.UiBackgroundAlpha + 0.05f * delta, 0f, 1f);
                        return GameMenuSettingsEffect.ApplySettings;
                    case 5:
                        settings.UiBorderThickness = Clamp(settings.UiBorderThickness + delta, 2, 12);
                        return GameMenuSettingsEffect.ApplySettings;
                }
            }
            else if (settingsTab == SettingsInput)
            {
                SelectedBindingSlotIndex = (SelectedBindingSlotIndex + delta + 2) % 2;
            }
            else if (settingsTab == SettingsDebug)
            {
                switch (rowIndex)
                {
                    case 3:
                        settings.SprintBoost = ClampFloat((settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost) + 0.05f * delta, 1f, 3f);
                        return GameMenuSettingsEffect.ApplySettings;
                    case 4:
                        settings.TurnMoveDelaySeconds = ClampFloat(settings.TurnMoveDelaySeconds + 0.01f * delta, 0f, 0.3f);
                        return GameMenuSettingsEffect.ApplySettings;
                }
            }

            return GameMenuSettingsEffect.None;
        }

        public GameMenuSettingsEffect ActivateSelectedSetting(Settings settings, int settingsTab, int selectedRowIndex, int bindingCount)
        {
            if (settings == null || selectedRowIndex == 0)
            {
                return GameMenuSettingsEffect.None;
            }

            var settingsRowIndex = selectedRowIndex - 1;
            if (settingsTab == SettingsGeneral && settingsRowIndex == 2)
            {
                settings.IsFullScreen = !settings.IsFullScreen;
                return GameMenuSettingsEffect.ApplySettings;
            }

            if (settingsTab == SettingsGeneral && settingsRowIndex == 5)
            {
                settings.AutoSaveEnabled = !settings.AutoSaveEnabled;
                return GameMenuSettingsEffect.ApplySettings;
            }

            if (settingsTab == SettingsDebug)
            {
                if (settingsRowIndex == 0)
                {
                    settings.MapDebugInfo = !settings.MapDebugInfo;
                    return GameMenuSettingsEffect.ApplySettings;
                }

                if (settingsRowIndex == 1)
                {
                    settings.ShowHiddenObjects = !settings.ShowHiddenObjects;
                    return GameMenuSettingsEffect.ApplySettingsAndRefreshVisibility;
                }

                if (settingsRowIndex == 2)
                {
                    settings.NoMonsters = !settings.NoMonsters;
                    return GameMenuSettingsEffect.ApplySettings;
                }
            }

            if (settingsTab == SettingsInput)
            {
                var bindingIndex = GetSelectedInputBindingIndex(selectedRowIndex);
                return bindingIndex >= bindingCount
                    ? GameMenuSettingsEffect.ResetBindings
                    : bindingIndex < 0 ? GameMenuSettingsEffect.None : GameMenuSettingsEffect.StartRebinding;
            }

            return GameMenuSettingsEffect.None;
        }

        public GameMenuSettingsEffect GetGeneralSettingsChangeEffect(
            Settings oldSettings,
            float uiScale,
            float dialogTextCharactersPerSecond,
            bool isFullScreen,
            float musicVolume,
            float soundEffectsVolume,
            bool autoSaveEnabled,
            float autoSaveIntervalSeconds)
        {
            if (oldSettings == null)
            {
                return GameMenuSettingsEffect.None;
            }

            var audioChanged =
                !NearlyEqual(oldSettings.MusicVolume, musicVolume) ||
                !NearlyEqual(oldSettings.SoundEffectsVolume, soundEffectsVolume);
            var otherChanged =
                !NearlyEqual(oldSettings.UiScale, uiScale) ||
                !NearlyEqual(oldSettings.DialogTextCharactersPerSecond, dialogTextCharactersPerSecond) ||
                oldSettings.IsFullScreen != isFullScreen ||
                oldSettings.AutoSaveEnabled != autoSaveEnabled ||
                !NearlyEqual(oldSettings.AutoSaveIntervalSeconds, autoSaveIntervalSeconds);

            return otherChanged
                ? GameMenuSettingsEffect.ApplySettings
                : audioChanged ? GameMenuSettingsEffect.ApplyAudioSettings : GameMenuSettingsEffect.None;
        }

        public GameMenuSettingsEffect GetUiSettingsChangeEffect(Settings oldSettings, Settings newSettings)
        {
            if (oldSettings == null || newSettings == null)
            {
                return GameMenuSettingsEffect.None;
            }

            return oldSettings.UiBackgroundColor != newSettings.UiBackgroundColor ||
                   !NearlyEqual(oldSettings.UiBackgroundAlpha, newSettings.UiBackgroundAlpha) ||
                   oldSettings.UiHoverColor != newSettings.UiHoverColor ||
                   oldSettings.UiActiveColor != newSettings.UiActiveColor ||
                   oldSettings.UiBorderColor != newSettings.UiBorderColor ||
                   oldSettings.UiBorderThickness != newSettings.UiBorderThickness ||
                   oldSettings.UiTextColor != newSettings.UiTextColor ||
                   oldSettings.UiHighlightColor != newSettings.UiHighlightColor
                ? GameMenuSettingsEffect.ApplySettings
                : GameMenuSettingsEffect.None;
        }

        public GameMenuSettingsEffect GetDebugSettingsChangeEffect(
            Settings oldSettings,
            bool mapDebugInfo,
            bool showHiddenObjects,
            bool noMonsters,
            float sprintBoost,
            float turnMoveDelaySeconds)
        {
            if (oldSettings == null)
            {
                return GameMenuSettingsEffect.None;
            }

            if (oldSettings.ShowHiddenObjects != showHiddenObjects)
            {
                return GameMenuSettingsEffect.ApplySettingsAndRefreshVisibility;
            }

            return oldSettings.MapDebugInfo != mapDebugInfo ||
                   oldSettings.NoMonsters != noMonsters ||
                   !NearlyEqual(oldSettings.SprintBoost, sprintBoost) ||
                   !NearlyEqual(oldSettings.TurnMoveDelaySeconds, turnMoveDelaySeconds)
                ? GameMenuSettingsEffect.ApplySettings
                : GameMenuSettingsEffect.None;
        }

        public int GetSelectedInputBindingIndex(int selectedRowIndex)
        {
            return selectedRowIndex - 1;
        }

        public string GetSelectedBindingSlotName()
        {
            return SelectedBindingSlotIndex == 0 ? "Primary" : "Gamepad";
        }

        public void ShowModal(string title, string message, IEnumerable<string> choices, IEnumerable<Hero> choiceHeroes, bool waitingForConfirmRelease)
        {
            ModalTitle = title;
            ModalMessage = message;
            ModalChoices = choices == null ? null : choices.ToList();
            ModalChoiceHeroes = choiceHeroes == null ? null : choiceHeroes.ToList();
            ModalSelectedIndex = 0;
            ModalWaitingForConfirmRelease = waitingForConfirmRelease;
        }

        public bool IsModalVisible()
        {
            return !string.IsNullOrEmpty(ModalMessage);
        }

        public bool ModalHasChoices()
        {
            return ModalChoices != null && ModalChoices.Count > 0;
        }

        public void HideModal()
        {
            ModalTitle = null;
            ModalMessage = null;
            ModalChoices = null;
            ModalChoiceHeroes = null;
            ModalSelectedIndex = 0;
            ModalWaitingForConfirmRelease = false;
        }

        public void ReleaseModalConfirmWait()
        {
            ModalWaitingForConfirmRelease = false;
        }

        public int MoveModalSelection(int delta)
        {
            if (!ModalHasChoices() || delta == 0)
            {
                return ModalSelectedIndex;
            }

            ModalSelectedIndex = Clamp(ModalSelectedIndex + delta, 0, ModalChoices.Count - 1);
            return ModalSelectedIndex;
        }

        public bool TrySelectModalChoice(int index, out int selectedIndex)
        {
            selectedIndex = -1;
            if (!ModalHasChoices() || index < 0 || index >= ModalChoices.Count)
            {
                return false;
            }

            selectedIndex = index;
            HideModal();
            return true;
        }

        public Hero GetModalChoiceHero(int index)
        {
            return ModalChoiceHeroes != null && index >= 0 && index < ModalChoiceHeroes.Count
                ? ModalChoiceHeroes[index]
                : null;
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

        private static float ClampFloat(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) < 0.0001f;
        }

        private static GameMenuUseAction GetTargetUseAction(Target target)
        {
            switch (target)
            {
                case Target.Group:
                    return GameMenuUseAction.Group;
                case Target.None:
                    return GameMenuUseAction.NoTarget;
                case Target.Single:
                    return GameMenuUseAction.Single;
                case Target.Object:
                    return GameMenuUseAction.Object;
                default:
                    return GameMenuUseAction.CannotUse;
            }
        }
    }
}
