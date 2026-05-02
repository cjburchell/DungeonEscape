using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGameMenu : MonoBehaviour
    {
        private enum MenuTab
        {
            Party,
            Inventory,
            Quests,
            Save,
            Settings
        }

        private enum SettingsTab
        {
            General,
            Ui,
            Input,
            Debug
        }

        private enum PartyDetailTab
        {
            Status,
            Equipment,
            Items,
            Skills,
            Spells
        }

        private const float MinUiScale = 0.5f;
        private const float MaxUiScale = 3f;
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static bool isOpen;

        private DungeonEscapeGameState gameState;
        private DungeonEscapeMessageBox messageBox;
        private DungeonEscapeUiSettings uiSettings;
        private TiledMapView mapView;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private DungeonEscapeUiTheme uiTheme;
        private float lastPixelScale;
        private string lastThemeSignature;
        private MenuTab currentTab = MenuTab.Party;
        private SettingsTab currentSettingsTab = SettingsTab.General;
        private PartyDetailTab currentPartyDetailTab = PartyDetailTab.Status;
        private Vector2 partyScrollPosition;
        private Vector2 inventoryScrollPosition;
        private Vector2 partyDetailScrollPosition;
        private Vector2 questScrollPosition;
        private Vector2 saveScrollPosition;
        private Vector2 settingsScrollPosition;
        private float menuBodyHeight;
        private int selectedHeroIndex;
        private int selectedPartyItemIndex;
        private int selectedRowIndex;
        private int selectedBindingSlotIndex;
        private int drawingRowIndex;
        private InputBinding rebindingInput;
        private string rebindingSlot;
        private int rebindingStartFrame;
        private string menuModalTitle;
        private string menuModalMessage;
        private List<string> menuModalChoices;
        private Action<int> menuModalSelected;
        private int menuModalSelectedIndex;
        private int repeatingMenuMoveX;
        private float nextMenuMoveXTime;
        private int repeatingMenuMoveY;
        private float nextMenuMoveYTime;
        private int heldSettingsTabMoveX;
        private bool menuControlsBlocked;
        private bool uiAssetsPrewarmed;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            PrewarmUiAssets();
        }

        private void Update()
        {
            if (DungeonEscapeStoreWindow.IsOpen)
            {
                return;
            }

            if (DungeonEscapeTitleMenu.IsOpen)
            {
                return;
            }

            if (rebindingInput != null)
            {
                if (Time.frameCount <= rebindingStartFrame)
                {
                    return;
                }

                string keyCode;
                if (DungeonEscapeInput.TryCaptureBinding(rebindingSlot, out keyCode))
                {
                    DungeonEscapeInput.SetBinding(rebindingInput, rebindingSlot, keyCode);
                    DungeonEscapeSettingsCache.Save();
                    rebindingInput = null;
                    rebindingSlot = null;
                }

                return;
            }

            if (IsMenuModalVisible())
            {
                UpdateMenuModal();
                return;
            }

            if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuPreviousTab))
            {
                CycleTab(-1);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuNextTab))
            {
                CycleTab(1);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                isOpen = false;
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Menu))
            {
                Toggle(MenuTab.Party);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                ActivateSelectedRow();
            }

            UpdateGamepadMenuNavigation();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            if (DungeonEscapeMessageBox.IsAnyVisible)
            {
                return;
            }

            var previousDepth = GUI.depth;
            GUI.depth = 1000;
            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 980f * scale);
            var height = Mathf.Min(Screen.height - 32f * scale, 680f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            GUI.Box(rect, GUIContent.none, panelStyle);
            var areaHeight = rect.height - 28f * scale;
            menuBodyHeight = Mathf.Max(160f * scale, areaHeight - 90f * scale);
            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, areaHeight));
            var previousEnabled = GUI.enabled;
            var previousMenuControlsBlocked = menuControlsBlocked;
            menuControlsBlocked = rebindingInput != null || IsMenuModalVisible();
            SetMenuGuiEnabled(previousEnabled);
            DrawHeader();
            DrawTabs();
            var previousVerticalThumb = GUI.skin.verticalScrollbarThumb;
            if (uiTheme != null)
            {
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
            }

            DrawCurrentTab();
            GUI.skin.verticalScrollbarThumb = previousVerticalThumb;
            GUI.enabled = previousEnabled;
            menuControlsBlocked = previousMenuControlsBlocked;
            GUILayout.EndArea();
            DrawMenuModalOverlay();
            DrawRebindingOverlay();
            GUI.depth = previousDepth;
        }

        private void SetMenuGuiEnabled(bool enabled)
        {
            GUI.enabled = enabled && !menuControlsBlocked;
        }

        private void Toggle(MenuTab tab)
        {
            PrewarmUiAssets();
            if (isOpen && currentTab == tab)
            {
                isOpen = false;
                return;
            }

            currentTab = tab;
            isOpen = true;
            selectedRowIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private void CycleTab(int delta)
        {
            var tabs = GetVisibleTabs();
            var currentIndex = tabs.IndexOf(currentTab);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            currentTab = tabs[(currentIndex + delta + tabs.Count) % tabs.Count];
            selectedRowIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private static List<MenuTab> GetVisibleTabs()
        {
            return new List<MenuTab>
            {
                MenuTab.Party,
                MenuTab.Quests,
                MenuTab.Save,
                MenuTab.Settings
            };
        }

        private void CycleSettingsTab(int delta)
        {
            var tabCount = Enum.GetValues(typeof(SettingsTab)).Length;
            var next = ((int)currentSettingsTab + delta + tabCount) % tabCount;
            currentSettingsTab = (SettingsTab)next;
            settingsScrollPosition = Vector2.zero;
            selectedRowIndex = 0;
        }

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
                MoveSelectedRow(moveY);
            }

            var moveX = currentTab == MenuTab.Settings && selectedRowIndex == 0
                ? GetSettingsTabMoveX()
                : GetMenuMoveX();
            if (moveX == 0)
            {
                return;
            }

            AdjustSelectedRow(moveX);
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dungeon Escape", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Width(120f * GetPixelScale())))
            {
                ConfirmReturnToMainMenu();
            }

            if (GUILayout.Button("Quit", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                ConfirmQuitGame();
            }

            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                isOpen = false;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            DrawTab(MenuTab.Party, "Party");
            DrawTab(MenuTab.Quests, "Quests");
            DrawTab(MenuTab.Save, "Save");
            DrawTab(MenuTab.Settings, "Settings");
            GUILayout.EndHorizontal();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawTab(MenuTab tab, string label)
        {
            if (DungeonEscapeUiControls.TabButton(label, currentTab == tab, uiTheme, 34f * GetPixelScale()))
            {
                currentTab = tab;
                selectedRowIndex = 0;
            }
        }

        private void DrawCurrentTab()
        {
            drawingRowIndex = 0;
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Mathf.Max(GetSelectableRowCount() - 1, 0));
            switch (currentTab)
            {
                case MenuTab.Party:
                    DrawParty();
                    break;
                case MenuTab.Inventory:
                    DrawInventory();
                    break;
                case MenuTab.Quests:
                    DrawQuests();
                    break;
                case MenuTab.Save:
                    DrawSave();
                    break;
                case MenuTab.Settings:
                    DrawSettings();
                    break;
            }
        }

        private void DrawParty()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            GUILayout.Label("Gold: " + party.Gold + "    Map: " + party.CurrentMapId + "    Steps: " + party.StepCount, labelStyle);
            GUILayout.Space(8f * GetPixelScale());
            var activeMembers = party.ActiveMembers.ToList();
            var inactive = party.InactiveMembers.ToList();
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Mathf.Max(activeMembers.Count + inactive.Count - 1, 0));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(340f * GetPixelScale()), GUILayout.Height(menuBodyHeight));
            partyScrollPosition = BeginThemedScroll(
                partyScrollPosition,
                Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 28f * GetPixelScale()));
            GUILayout.Label("Active Party (" + activeMembers.Count + "/" + GetMaxPartyMembers() + ")", titleStyle);
            for (var i = 0; i < activeMembers.Count; i++)
            {
                BeginSelectableRow();
                DrawHeroStatus(activeMembers[i], true);
                DrawActivePartyControls(activeMembers[i], i, activeMembers.Count);
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            if (inactive.Count > 0)
            {
                GUILayout.Space(10f * GetPixelScale());
                GUILayout.Label("Reserve", titleStyle);
                for (var i = 0; i < inactive.Count; i++)
                {
                    BeginSelectableRow();
                    DrawHeroStatus(inactive[i], false);
                    DrawReservePartyControls(inactive[i], activeMembers.Count);
                    EndSelectableRow();
                    SelectRowOnMouseClick(activeMembers.Count + i);
                }
            }
            EndThemedScroll();
            GUILayout.EndVertical();

            var selectedHero = GetSelectedPartyHero(activeMembers, inactive);
            if (selectedHero != null)
            {
                GUILayout.Space(10f * GetPixelScale());
                DrawPartyDetail(selectedHero);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawActivePartyControls(Hero hero, int index, int activeCount)
        {
            GUILayout.BeginHorizontal();
            SetMenuGuiEnabled(index > 0);
            if (GUILayout.Button("Up", buttonStyle, GUILayout.Width(72f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberUp(hero));
            }

            SetMenuGuiEnabled(index < activeCount - 1);
            if (GUILayout.Button("Down", buttonStyle, GUILayout.Width(86f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberDown(hero));
            }

            SetMenuGuiEnabled(activeCount > 1);
            if (GUILayout.Button("Reserve", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.DeactivatePartyMember(hero));
            }

            SetMenuGuiEnabled(true);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawReservePartyControls(Hero hero, int activeCount)
        {
            SetMenuGuiEnabled(activeCount < GetMaxPartyMembers());
            if (GUILayout.Button("Add to Party", buttonStyle, GUILayout.Width(144f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.ActivatePartyMember(hero));
            }

            SetMenuGuiEnabled(true);
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawHeroStatus(Hero hero, bool active)
        {
            GUILayout.BeginHorizontal();
            Sprite sprite;
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null,
                48f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(hero.Name + "  L" + hero.Level + " " + hero.Class + (active ? "" : "  Reserve"), labelStyle);
            GUILayout.Label(
                "HP " + hero.Health + "/" + hero.MaxHealth +
                "   MP " + hero.Magic + "/" + hero.MaxMagic +
                "   XP " + hero.Xp + "/" + hero.NextLevel,
                smallStyle);
            GUILayout.Label(
                "ATK " + hero.Attack +
                "   DEF " + hero.Defence +
                "   MDEF " + hero.MagicDefence +
                "   AGI " + hero.Agility,
                smallStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawPartyDetail(Hero hero)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(420f * GetPixelScale()));
            GUILayout.Label("Status", titleStyle);
            GUILayout.BeginHorizontal();
            Sprite sprite;
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null,
                72f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(hero.Name + "  Level " + hero.Level + " " + hero.Class + "  " + hero.Gender, labelStyle);
            DrawDetailValue("HP", hero.Health + " / " + hero.MaxHealth);
            DrawDetailValue("MP", hero.Magic + " / " + hero.MaxMagic);
            DrawDetailValue("XP", hero.Xp + " / " + hero.NextLevel + " (" + GetXpToNextLevel(hero) + " to next)");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Attributes", labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawDetailValue("Attack", hero.Attack.ToString());
            DrawDetailValue("Defence", hero.Defence.ToString());
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            DrawDetailValue("Magic Defence", hero.MagicDefence.ToString());
            DrawDetailValue("Agility", hero.Agility.ToString());
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Equipment", labelStyle);
            foreach (Slot slot in Enum.GetValues(typeof(Slot)))
            {
                DrawEquipmentSlot(hero, slot);
            }

            DrawStatusEffects(hero);
            DrawKnownSkills(hero);
            DrawKnownSpells(hero);

            GUILayout.EndVertical();
        }

        private void DrawDetailValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", smallStyle, GUILayout.Width(112f * GetPixelScale()));
            GUILayout.Label(value, smallStyle);
            GUILayout.EndHorizontal();
        }

        private static ulong GetXpToNextLevel(Hero hero)
        {
            return hero == null || hero.Xp >= hero.NextLevel ? 0 : hero.NextLevel - hero.Xp;
        }

        private void DrawEquipmentSlot(Hero hero, Slot slot)
        {
            var item = GetEquippedItem(hero, slot);
            GUILayout.BeginHorizontal();
            Sprite sprite;
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                32f * GetPixelScale(),
                uiTheme);
            GUILayout.Label(slot + ":", smallStyle, GUILayout.Width(98f * GetPixelScale()));
            GUILayout.Label(item == null ? "Empty" : item.NameWithStats, GetRarityStyle(item, smallStyle));
            GUILayout.EndHorizontal();
        }

        private void DrawStatusEffects(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Effects", labelStyle);
            if (hero.Status == null || hero.Status.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var effect in hero.Status)
            {
                var duration = effect.Duration <= 0 ? "" : "  " + effect.Duration + " " + effect.DurationType;
                var stat = effect.StatType == StatType.None ? "" : "  " + effect.StatType + " " + effect.StatValue;
                GUILayout.Label(effect.Name + "  " + effect.Type + stat + duration, smallStyle);
            }
        }

        private void DrawKnownSkills(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Skills", labelStyle);
            var skills = DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.Skills == null
                ? new List<Skill>()
                : hero.GetSkills(DungeonEscapeGameDataCache.Current.Skills).ToList();
            if (skills.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var skill in skills)
            {
                GUILayout.Label(skill.Name + "  " + skill.Type + "  " + skill.Targets, smallStyle);
            }
        }

        private void DrawKnownSpells(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Spells", labelStyle);
            var spells = DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.Spells == null
                ? new List<Spell>()
                : hero.GetSpells(DungeonEscapeGameDataCache.Current.Spells).ToList();
            if (spells.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var spell in spells)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(spell.Name + "  MP " + spell.Cost + "  " + spell.Type + "  " + spell.Targets, smallStyle);
                SetMenuGuiEnabled(gameState != null && CanCastSpellFromPartyMenu(hero, spell));
                if (GUILayout.Button("Cast", buttonStyle, GUILayout.Width(86f * GetPixelScale())))
                {
                    ShowSpellTargetPicker(hero, spell);
                }

                SetMenuGuiEnabled(true);
                GUILayout.EndHorizontal();
            }
        }

        private bool CanCastSpellFromPartyMenu(Hero caster, Spell spell)
        {
            if (gameState == null || !gameState.CanCastHeroSpell(caster, spell))
            {
                return false;
            }

            if (spell.Type == SkillType.Outside)
            {
                return gameState.CanCastOutside();
            }

            if (spell.Type == SkillType.Return)
            {
                return gameState.CanCastReturn();
            }

            return true;
        }

        private void ShowSpellTargetPicker(Hero caster, Spell spell)
        {
            EnsureReferences();
            if (gameState == null || caster == null || spell == null)
            {
                return;
            }

            if (spell.Type == SkillType.Outside)
            {
                ShowPartyMessage(gameState.CastOutsideSpell(caster, spell));
                return;
            }

            if (spell.Type == SkillType.Return)
            {
                ShowReturnLocationPicker(caster, spell);
                return;
            }

            if (spell.Type == SkillType.Open)
            {
                ShowPartyMessage(CastSpellOnFacingObject(caster, spell));
                return;
            }

            switch (spell.Targets)
            {
                case Target.Group:
                    ShowPartyMessage(gameState.CastHeroSpellOnParty(caster, spell));
                    return;
                case Target.None:
                    ShowPartyMessage(gameState.CastHeroSpellWithoutTarget(caster, spell));
                    return;
                case Target.Single:
                    ShowSingleSpellTargetPicker(caster, spell);
                    return;
                case Target.Object:
                    ShowPartyMessage(CastSpellOnFacingObject(caster, spell));
                    return;
                default:
                    ShowPartyMessage("That spell cannot be cast from the party menu.");
                    return;
            }
        }

        private void ShowReturnLocationPicker(Hero caster, Spell spell)
        {
            var locations = gameState.GetReturnLocations().ToList();
            if (locations.Count == 0)
            {
                ShowPartyMessage(gameState.Party != null && gameState.Party.CurrentMapIsOverWorld
                    ? "You have not visited any places to return to."
                    : "Return can only be used outside.");
                return;
            }

            var labels = locations
                .Select(location => string.IsNullOrEmpty(location.DisplayName) ? location.MapId : location.DisplayName)
                .ToList();
            labels.Add("Cancel");
            ShowMenuModal("Return", "Choose a place to return to.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= locations.Count)
                {
                    return;
                }

                ShowPartyMessage(gameState.CastReturnSpell(caster, spell, locations[selectedIndex]));
            });
        }

        private void ShowSingleSpellTargetPicker(Hero caster, Spell spell)
        {
            var party = GetParty();
            if (party == null)
            {
                ShowPartyMessage("No party loaded.");
                return;
            }

            var targets = party.ActiveMembers.ToList();
            if (targets.Count == 0)
            {
                ShowPartyMessage("No party members can be targeted.");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            ShowMenuModal("Cast " + spell.Name, "Choose a target.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                ShowPartyMessage(gameState.CastHeroSpell(caster, spell, targets[selectedIndex]));
            });
        }

        private Hero GetSelectedPartyHero(IList<Hero> activeMembers, IList<Hero> inactiveMembers)
        {
            if (selectedRowIndex < activeMembers.Count)
            {
                return activeMembers[selectedRowIndex];
            }

            var reserveIndex = selectedRowIndex - activeMembers.Count;
            return reserveIndex >= 0 && reserveIndex < inactiveMembers.Count ? inactiveMembers[reserveIndex] : null;
        }

        private static ItemInstance GetEquippedItem(Hero hero, Slot slot)
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

        private void ApplyPartyChange(Func<bool> action)
        {
            EnsureReferences();
            if (action == null || !action())
            {
                return;
            }

            var player = FindAnyObjectByType<PlayerGridController>();
            if (player != null)
            {
                player.RefreshPartyFollowers();
            }
        }

        private void DrawInventory()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            var members = GetInventoryMembers(party);
            if (members.Count == 0)
            {
                GUILayout.Label("No party members.", labelStyle);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                if (DungeonEscapeUiControls.Button(members[i].Name, selectedHeroIndex == i, uiTheme))
                {
                    selectedHeroIndex = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            GUILayout.BeginHorizontal();
            GUILayout.Label(hero.Name + "'s Inventory (" + hero.Items.Count + "/" + Party.MaxItems + ")", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gold: " + party.Gold, labelStyle);
            GUILayout.EndHorizontal();
            if (hero.Items.Count == 0)
            {
                GUILayout.Label("No items.", labelStyle);
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, hero.Items.Count - 1);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MinWidth(420f * GetPixelScale()));
            inventoryScrollPosition = BeginThemedScroll(inventoryScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 82f * GetPixelScale()));
            for (var i = 0; i < hero.Items.Count; i++)
            {
                var item = hero.Items[i];
                var equipped = item.IsEquipped ? " [E]" : "";
                BeginSelectableRow();
                GUILayout.BeginHorizontal();
                Sprite sprite;
                DungeonEscapeUiControls.SpriteIcon(
                    DungeonEscapeUiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                    36f * GetPixelScale(),
                    uiTheme);
                GUILayout.Label(item.NameWithStats + equipped, GetRarityStyle(item, labelStyle));
                if (item.IsEquipped)
                {
                    if (GUILayout.Button("Unequip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                    }
                }
                else if (hero.CanEquipItem(item))
                {
                    if (GUILayout.Button("Equip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                    }
                }

                GUILayout.EndHorizontal();
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            EndThemedScroll();
            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());
            DrawInventoryItemDetail(hero, hero.Items[selectedRowIndex]);
            GUILayout.EndHorizontal();
        }

        private void DrawInventoryItemDetail(Hero hero, ItemInstance item)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(320f * GetPixelScale()));
            if (item == null)
            {
                GUILayout.Label("No item selected.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            Sprite sprite;
            GUILayout.BeginHorizontal();
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                64f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(item.Name, GetRarityStyle(item, labelStyle));
            GUILayout.Label(GetItemDetailSubtitle(item), smallStyle);
            if (!string.IsNullOrEmpty(item.Item.StatString))
            {
                GUILayout.Label("Stats: " + item.Item.StatString, smallStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * GetPixelScale());
            if (item.MaxCharges > 0)
            {
                GUILayout.Label("Charges: " + item.Charges + "/" + item.MaxCharges, smallStyle);
            }

            GUILayout.Space(8f * GetPixelScale());
            if (gameState != null && CanUseItemFromInventory(hero, item))
            {
                if (GUILayout.Button("Use", buttonStyle))
                {
                    ShowUseItemTargetPicker(hero, item);
                }
            }

            if (item.IsEquipped)
            {
                if (GUILayout.Button("Unequip", buttonStyle))
                {
                    ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                }
            }
            else if (hero.CanEquipItem(item))
            {
                if (GUILayout.Button("Equip", buttonStyle))
                {
                    ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                }
            }

            if (HasTransferTarget(hero))
            {
                if (GUILayout.Button("Transfer", buttonStyle))
                {
                    ShowTransferItemTargetPicker(hero, item);
                }
            }

            if (item.Type != ItemType.Quest)
            {
                if (GUILayout.Button("Drop", buttonStyle))
                {
                    ShowDropItemConfirmation(hero, item);
                }
            }

            GUILayout.EndVertical();
        }

        private static string GetItemDetailSubtitle(ItemInstance item)
        {
            if (item == null)
            {
                return "";
            }

            var subtitle = item.Type == ItemType.Quest
                ? item.Type.ToString()
                : item.Rarity + " " + item.Type;
            return item.IsEquipped ? subtitle + "  Equipped" : subtitle;
        }

        private bool HasTransferTarget(Hero source)
        {
            var party = GetParty();
            return party != null &&
                   source != null &&
                   GetInventoryMembers(party).Any(member => !ReferenceEquals(member, source) && member.Items.Count < Party.MaxItems);
        }

        private void ShowTransferItemTargetPicker(Hero source, ItemInstance item)
        {
            var party = GetParty();
            if (party == null || source == null || item == null)
            {
                return;
            }

            var targets = GetInventoryMembers(party)
                .Where(member => !ReferenceEquals(member, source) && member.Items.Count < Party.MaxItems)
                .ToList();
            if (targets.Count == 0)
            {
                ShowInventoryMessage("No party member has room for " + item.Name + ".");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            ShowMenuModal("Transfer " + item.Name, "Choose who should carry this item.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                var target = targets[selectedIndex];
                if (gameState.TransferHeroItem(source, target, item))
                {
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(source.Items.Count - 1, 0));
                    ShowInventoryMessage(source.Name + " gave " + item.Name + " to " + target.Name + ".");
                }
                else
                {
                    ShowInventoryMessage("Could not transfer " + item.Name + ".");
                }
            });
        }

        private void ShowDropItemConfirmation(Hero hero, ItemInstance item)
        {
            if (hero == null || item == null)
            {
                return;
            }

            if (item.Type == ItemType.Quest)
            {
                ShowInventoryMessage("Quest items cannot be dropped.");
                return;
            }

            ShowMenuModal("Drop " + item.Name, "Discard this item?", new[] { "Drop", "Cancel" }, selectedIndex =>
            {
                if (selectedIndex != 0)
                {
                    return;
                }

                if (gameState.DropHeroItem(hero, item))
                {
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                    ShowInventoryMessage(item.Name + " was dropped.");
                }
                else
                {
                    ShowInventoryMessage("Could not drop " + item.Name + ".");
                }
            });
        }

        private void ShowUseItemTargetPicker(Hero hero, ItemInstance item)
        {
            EnsureReferences();
            if (gameState == null || hero == null || item == null)
            {
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Outside)
            {
                ShowInventoryMessage(gameState.UseOutsideItem(hero, item));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Return)
            {
                ShowReturnLocationPicker(hero, item);
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Open)
            {
                ShowInventoryMessage(UseItemOnFacingObject(hero, item));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                return;
            }

            switch (item.Target)
            {
                case Target.Group:
                    ShowInventoryMessage(gameState.UseHeroItemOnParty(hero, item));
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                    return;
                case Target.None:
                    ShowInventoryMessage(gameState.UseHeroItem(hero, item, hero));
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                    return;
                case Target.Single:
                    ShowSingleItemTargetPicker(hero, item);
                    return;
                case Target.Object:
                    ShowInventoryMessage(UseItemOnFacingObject(hero, item));
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
                    return;
                default:
                    ShowInventoryMessage("That item cannot be used from inventory.");
                    return;
            }
        }

        private static string UseItemOnFacingObject(Hero hero, ItemInstance item)
        {
            var player = FindAnyObjectByType<PlayerGridController>();
            return player == null
                ? "There is no map object target."
                : player.UseItemOnFacingObject(hero, item);
        }

        private static string CastSpellOnFacingObject(Hero hero, Spell spell)
        {
            var player = FindAnyObjectByType<PlayerGridController>();
            return player == null
                ? "There is no map object target."
                : player.CastSpellOnFacingObject(hero, spell);
        }

        private void ShowSingleItemTargetPicker(Hero hero, ItemInstance item)
        {
            var party = GetParty();
            if (party == null)
            {
                ShowInventoryMessage("No party loaded.");
                return;
            }

            var targets = party.ActiveMembers.ToList();
            if (targets.Count == 0)
            {
                ShowInventoryMessage("No party members can be targeted.");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            ShowMenuModal("Use " + item.Name, "Choose a target.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                ShowInventoryMessage(gameState.UseHeroItem(hero, item, targets[selectedIndex]));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
            });
        }

        private bool CanUseItemFromInventory(Hero hero, ItemInstance item)
        {
            if (gameState == null || !gameState.CanUseHeroItem(hero, item))
            {
                return false;
            }

            if (item != null && item.Item != null && item.Item.Skill != null)
            {
                if (item.Item.Skill.Type == SkillType.Outside)
                {
                    return gameState.CanCastOutside();
                }

                if (item.Item.Skill.Type == SkillType.Return)
                {
                    return gameState.CanCastReturn();
                }
            }

            return true;
        }

        private void ShowReturnLocationPicker(Hero hero, ItemInstance item)
        {
            var locations = gameState.GetReturnLocations().ToList();
            if (locations.Count == 0)
            {
                ShowInventoryMessage(gameState.Party != null && gameState.Party.CurrentMapIsOverWorld
                    ? "You have not visited any places to return to."
                    : "Return can only be used outside.");
                return;
            }

            var labels = locations
                .Select(location => string.IsNullOrEmpty(location.DisplayName) ? location.MapId : location.DisplayName)
                .ToList();
            labels.Add("Cancel");
            ShowMenuModal("Return", "Choose a place to return to.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= locations.Count)
                {
                    return;
                }

                ShowInventoryMessage(gameState.UseReturnItem(hero, item, locations[selectedIndex]));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(hero.Items.Count - 1, 0));
            });
        }

        private void ShowInventoryMessage(string message)
        {
            ShowMenuModal("Inventory", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private void ShowPartyMessage(string message)
        {
            ShowMenuModal("Party", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private void ShowMenuModal(string title, string message, IEnumerable<string> choices, Action<int> selected)
        {
            menuModalTitle = title;
            menuModalMessage = message;
            menuModalChoices = choices == null ? null : choices.ToList();
            menuModalSelected = selected;
            menuModalSelectedIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private bool IsMenuModalVisible()
        {
            return !string.IsNullOrEmpty(menuModalMessage);
        }

        private bool MenuModalHasChoices()
        {
            return menuModalChoices != null && menuModalChoices.Count > 0;
        }

        private void HideMenuModal()
        {
            menuModalTitle = null;
            menuModalMessage = null;
            menuModalChoices = null;
            menuModalSelected = null;
            menuModalSelectedIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private void UpdateMenuModal()
        {
            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                HideMenuModal();
                return;
            }

            if (!MenuModalHasChoices())
            {
                if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
                {
                    HideMenuModal();
                }

                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY < 0)
            {
                menuModalSelectedIndex = Mathf.Max(0, menuModalSelectedIndex - 1);
            }
            else if (moveY > 0)
            {
                menuModalSelectedIndex = Mathf.Min(menuModalChoices.Count - 1, menuModalSelectedIndex + 1);
            }

            if (menuModalChoices.Count <= 2)
            {
                var moveX = DungeonEscapeInput.GetMoveXDown();
                if (moveX < 0)
                {
                    menuModalSelectedIndex = Mathf.Max(0, menuModalSelectedIndex - 1);
                }
                else if (moveX > 0)
                {
                    menuModalSelectedIndex = Mathf.Min(menuModalChoices.Count - 1, menuModalSelectedIndex + 1);
                }
            }

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                SelectMenuModalChoice(menuModalSelectedIndex);
            }
        }

        private void SelectMenuModalChoice(int index)
        {
            if (!MenuModalHasChoices() || index < 0 || index >= menuModalChoices.Count)
            {
                return;
            }

            var selected = menuModalSelected;
            HideMenuModal();
            if (selected != null)
            {
                selected(index);
            }
        }

        private void ApplyInventoryChange(Func<bool> action)
        {
            EnsureReferences();
            if (action != null)
            {
                action();
            }
        }

        private void DrawQuests()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            if (party.ActiveQuests == null || party.ActiveQuests.Count == 0)
            {
                GUILayout.Label("No active quests.", labelStyle);
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, party.ActiveQuests.Count - 1);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MinWidth(420f * GetPixelScale()));
            questScrollPosition = BeginThemedScroll(questScrollPosition, menuBodyHeight);
            for (var i = 0; i < party.ActiveQuests.Count; i++)
            {
                var activeQuest = party.ActiveQuests[i];
                Quest quest;
                if (DungeonEscapeGameDataCache.Current == null ||
                    !DungeonEscapeGameDataCache.Current.TryGetQuest(activeQuest.Id, out quest))
                {
                    BeginSelectableRow();
                    GUILayout.Label(activeQuest.Id + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                    EndSelectableRow();
                    SelectRowOnMouseClick(i);
                    continue;
                }

                BeginSelectableRow();
                GUILayout.BeginVertical();
                GUILayout.Label(quest.Name + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                var currentStage = quest.Stages == null
                    ? null
                    : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                {
                    GUILayout.Label(currentStage.Description, smallStyle);
                }

                GUILayout.EndVertical();
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }
            EndThemedScroll();
            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());
            DrawQuestDetail(party.ActiveQuests[selectedRowIndex]);
            GUILayout.EndHorizontal();
        }

        private void DrawQuestDetail(ActiveQuest activeQuest)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(360f * GetPixelScale()));
            if (activeQuest == null)
            {
                GUILayout.Label("No quest selected.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            Quest quest = null;
            if (DungeonEscapeGameDataCache.Current != null)
            {
                DungeonEscapeGameDataCache.Current.TryGetQuest(activeQuest.Id, out quest);
            }

            GUILayout.Label(quest == null ? activeQuest.Id : quest.Name, titleStyle);
            GUILayout.Label(activeQuest.Completed ? "Completed" : "Active", labelStyle);
            GUILayout.Label("Current Stage: " + activeQuest.CurrentStage, smallStyle);

            if (quest == null)
            {
                GUILayout.Label("Quest data was not found.", smallStyle);
                GUILayout.EndVertical();
                return;
            }

            if (!string.IsNullOrEmpty(quest.Description))
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label(quest.Description, smallStyle);
            }

            var currentStage = quest.Stages == null
                ? null
                : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
            if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label("Stage", labelStyle);
                GUILayout.Label(currentStage.Description, smallStyle);
            }

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Rewards", labelStyle);
            GUILayout.Label("XP: " + quest.Xp + "    Gold: " + quest.Gold, smallStyle);
            if (quest.Items != null && quest.Items.Count > 0)
            {
                GUILayout.Label("Items: " + string.Join(", ", quest.Items.ToArray()), smallStyle);
            }

            if (quest.Stages != null && quest.Stages.Count > 0)
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label("Stages", labelStyle);
                foreach (var stage in quest.Stages.OrderBy(stage => stage.Number))
                {
                    var marker = stage.Number == activeQuest.CurrentStage ? "> " : "  ";
                    GUILayout.Label(marker + stage.Number + ": " + stage.Description, smallStyle);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSave()
        {
            EnsureReferences();
            if (gameState == null)
            {
                GUILayout.Label("Game state is not loaded.", labelStyle);
                return;
            }

            var saves = gameState.GetManualSaveSlots();
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, saves.Count);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MinWidth(420f * GetPixelScale()));
            saveScrollPosition = BeginThemedScroll(saveScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight));
            for (var i = 0; i < saves.Count; i++)
            {
                BeginSelectableRow();
                DrawSaveRow(saves[i]);
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            BeginSelectableRow();
            DrawNewSaveRow();
            EndSelectableRow();
            SelectRowOnMouseClick(saves.Count);

            EndThemedScroll();
            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());
            DrawSaveDetail(selectedRowIndex < saves.Count ? saves[selectedRowIndex] : null);
            GUILayout.EndHorizontal();
        }

        private void DrawSaveRow(GameSave save)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(GetSaveTitle(save), labelStyle);
            GUILayout.Label(GetSaveSummary(save), smallStyle);
            GUILayout.EndVertical();
        }

        private void DrawNewSaveRow()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("New Save", labelStyle);
            GUILayout.Label("Save the current quest.", smallStyle);
            GUILayout.EndVertical();
        }

        private void DrawSaveDetail(GameSave save)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(380f * GetPixelScale()));
            if (IsUsableSave(save))
            {
                GUILayout.Label(GetSaveTitle(save), titleStyle);
                GUILayout.Label(GetSaveSummary(save), smallStyle);
            }
            else
            {
                GUILayout.Label("New Save", titleStyle);
                GUILayout.Label("Create a new manual save from the current quest.", smallStyle);
            }

            GUILayout.Space(12f * GetPixelScale());
            if (GUILayout.Button(IsUsableSave(save) ? "Save Over" : "Save", buttonStyle, GUILayout.Height(32f * GetPixelScale())))
            {
                ConfirmSaveManual(selectedRowIndex, save);
            }

            if (IsUsableSave(save))
            {
                if (GUILayout.Button("Load", buttonStyle, GUILayout.Height(32f * GetPixelScale())))
                {
                    ConfirmLoadManual(selectedRowIndex);
                }

                if (GUILayout.Button("Delete", buttonStyle, GUILayout.Height(32f * GetPixelScale())))
                {
                    ConfirmDeleteManual(selectedRowIndex);
                }
            }

            GUILayout.EndVertical();
        }

        private void ConfirmSaveManual(int slotIndex, GameSave save)
        {
            if (IsUsableSave(save))
            {
                ShowMenuModal(
                    "Save",
                    "Save over this quest?",
                    new[] { "Save", "Cancel" },
                    index =>
                    {
                        if (index == 0)
                        {
                            SaveManual(slotIndex);
                        }
                    });
                return;
            }

            SaveManual(slotIndex);
        }

        private void SaveManual(int slotIndex)
        {
            ShowSaveMessage(gameState != null && gameState.SaveManual(slotIndex)
                ? "Saved quest."
                : "Could not save quest.");
        }

        private void ConfirmLoadManual(int slotIndex)
        {
            ShowMenuModal(
                "Load",
                "Load this quest? Unsaved progress will be lost.",
                new[] { "Load", "Cancel" },
                index =>
                {
                    if (index == 0)
                    {
                        if (gameState != null && gameState.LoadManual(slotIndex))
                        {
                            isOpen = false;
                        }
                        else
                        {
                            ShowSaveMessage("Could not load quest.");
                        }
                    }
                });
        }

        private void ConfirmDeleteManual(int slotIndex)
        {
            ShowMenuModal(
                "Delete",
                "Delete this quest?",
                new[] { "Delete", "Cancel" },
                index =>
                {
                    if (index == 0)
                    {
                        ShowSaveMessage(gameState != null && gameState.DeleteManual(slotIndex)
                            ? "Deleted quest."
                            : "Could not delete quest.");
                    }
                });
        }

        private void ConfirmRestartNewGame()
        {
            ShowMenuModal(
                "New Game",
                "Start a new game? Unsaved progress will be lost.",
                new[] { "Start", "Cancel" },
                index =>
                {
                    if (index == 0 && gameState != null)
                    {
                        gameState.RestartNewGame();
                        isOpen = false;
                    }
                });
        }

        private void ConfirmReturnToMainMenu()
        {
            ShowMenuModal(
                "Main Menu",
                "Return to the main menu? Unsaved progress will be lost.",
                new[] { "OK", "Back" },
                index =>
                {
                    if (index == 0)
                    {
                        isOpen = false;
                        DungeonEscapeTitleMenu.OpenMainMenu();
                    }
                });
        }

        private void ConfirmQuitGame()
        {
            ShowMenuModal(
                "Quit",
                "Quit the game? Unsaved progress will be lost.",
                new[] { "Quit", "Back" },
                index =>
                {
                    if (index == 0)
                    {
                        Application.Quit();
                    }
                });
        }

        private void ShowSaveMessage(string message)
        {
            ShowMenuModal("Save", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private static string GetSaveTitle(GameSave save)
        {
            return DungeonEscapeGameState.GetGameSaveTitle(save);
        }

        private static string GetSaveSummary(GameSave save)
        {
            return DungeonEscapeGameState.GetGameSaveSummary(save);
        }

        private static bool IsUsableSave(GameSave save)
        {
            return DungeonEscapeGameState.IsUsableGameSave(save);
        }

        private void DrawSettings()
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                GUILayout.Label("Settings are not loaded.", labelStyle);
                return;
            }

            DrawSettingsTabs();

            settingsScrollPosition = BeginThemedScroll(settingsScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 50f * GetPixelScale()));
            switch (currentSettingsTab)
            {
                case SettingsTab.General:
                    DrawGeneralSettings(settings);
                    break;
                case SettingsTab.Ui:
                    DrawUiSettings(settings);
                    break;
                case SettingsTab.Input:
                    DrawInputBindings();
                    break;
                case SettingsTab.Debug:
                    DrawDebugSettings(settings);
                    break;
            }
            EndThemedScroll();
        }

        private void DrawSettingsTabs()
        {
            BeginSelectableRow();
            GUILayout.BeginHorizontal();
            DrawSettingsTab(SettingsTab.General, "General");
            DrawSettingsTab(SettingsTab.Ui, "UI");
            DrawSettingsTab(SettingsTab.Input, "Input Bindings");
            DrawSettingsTab(SettingsTab.Debug, "Debug");
            GUILayout.EndHorizontal();
            EndSelectableRow();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawSettingsTab(SettingsTab tab, string label)
        {
            if (DungeonEscapeUiControls.TabButton(label, currentSettingsTab == tab, uiTheme, 32f * GetPixelScale()))
            {
                currentSettingsTab = tab;
                settingsScrollPosition = Vector2.zero;
                selectedRowIndex = 0;
            }
        }

        private void DrawGeneralSettings(Settings settings)
        {
            GUI.changed = false;
            BeginSelectableRow();
            settings.UiScale = DrawSliderRow("UI Scale: " + settings.UiScale.ToString("0.00"), settings.UiScale <= 0f ? 1f : settings.UiScale, MinUiScale, MaxUiScale);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.IsFullScreen = DrawCheckboxRow(settings.IsFullScreen, "Fullscreen");
            EndSelectableRow();
            BeginSelectableRow();
            settings.SprintBoost = DrawSliderRow("Sprint Boost: " + settings.SprintBoost.ToString("0.00"), settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost, 1f, 3f);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.TurnMoveDelaySeconds = DrawSliderRow("Turn Delay: " + GetTurnMoveDelay(settings).ToString("0.00") + " seconds", GetTurnMoveDelay(settings), 0f, 0.3f);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.AutoSaveEnabled = DrawCheckboxRow(settings.AutoSaveEnabled, "Autosave enabled");
            EndSelectableRow();
            BeginSelectableRow();
            SetMenuGuiEnabled(settings.AutoSaveEnabled);
            settings.AutoSaveIntervalSeconds = DrawSliderRow("Autosave Period: " + GetAutoSaveInterval(settings).ToString("0") + " seconds", GetAutoSaveInterval(settings), 5f, 300f);
            SetMenuGuiEnabled(true);
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
            }
        }

        private void DrawUiSettings(Settings settings)
        {
            GUI.changed = false;
            BeginSelectableRow();
            settings.UiBackgroundColor = DrawTextFieldRow("Background Colour", settings.UiBackgroundColor, "#000000");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBackgroundAlpha = DrawSliderRow("Background Transparency: " + settings.UiBackgroundAlpha.ToString("0.00"), Mathf.Clamp01(settings.UiBackgroundAlpha), 0f, 1f);
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiHoverColor = DrawTextFieldRow("Hover Colour", settings.UiHoverColor, "#808080");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiActiveColor = DrawTextFieldRow("Pressed Colour", settings.UiActiveColor, "#D3D3D3");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBorderColor = DrawTextFieldRow("Border Colour", settings.UiBorderColor, "#FFFFFF");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBorderThickness = Mathf.RoundToInt(DrawSliderRow("Border Thickness: " + GetBorderThickness(settings), GetBorderThickness(settings), 2f, 12f));
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiTextColor = DrawTextFieldRow("Text Colour", settings.UiTextColor, "#FFFFFF");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiHighlightColor = DrawTextFieldRow("Highlighted Text/Border Colour", settings.UiHighlightColor, "#FFFF00");
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
            }
        }

        private void DrawDebugSettings(Settings settings)
        {
            GUI.changed = false;
            BeginSelectableRow();
            settings.MapDebugInfo = DrawCheckboxRow(settings.MapDebugInfo, "Map debug info");
            EndSelectableRow();
            BeginSelectableRow();
            settings.ShowHiddenObjects = DrawCheckboxRow(settings.ShowHiddenObjects, "Show hidden map objects");
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
            }
        }

        private static float GetAutoSaveInterval(Settings settings)
        {
            return settings.AutoSaveIntervalSeconds <= 0f ? 5f : settings.AutoSaveIntervalSeconds;
        }

        private static float GetTurnMoveDelay(Settings settings)
        {
            return Mathf.Clamp(settings.TurnMoveDelaySeconds, 0f, 0.3f);
        }

        private void DrawInputBindings()
        {
            var bindings = DungeonEscapeInput.GetBindings();
            var headerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUILayout.BeginHorizontal();
            GUILayout.Label("Action", headerStyle, GUILayout.Width(220f * GetPixelScale()));
            GUILayout.Label("Key", headerStyle, GUILayout.Width(190f * GetPixelScale()));
            GUILayout.Label("Gamepad", headerStyle, GUILayout.Width(190f * GetPixelScale()));
            GUILayout.EndHorizontal();

            foreach (var binding in bindings.OrderBy(item => item.Command))
            {
                BeginSelectableRow();
                GUILayout.BeginHorizontal();
                GUILayout.Label(binding.Command, labelStyle, GUILayout.Width(220f * GetPixelScale()));
                DrawBindingCell(binding, "Primary", binding.Primary, 0);
                DrawBindingCell(binding, "Gamepad", binding.Gamepad, 1);
                GUILayout.EndHorizontal();
                EndSelectableRow();
            }

            BeginSelectableRow();
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(120f * GetPixelScale())))
            {
                DungeonEscapeInput.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
            }
            EndSelectableRow();
        }

        private void DrawBindingCell(InputBinding binding, string slot, string currentValue, int slotIndex)
        {
            var label = string.IsNullOrEmpty(currentValue) || currentValue == "None" ? "-" : currentValue;
            var selected = currentSettingsTab == SettingsTab.Input &&
                           drawingRowIndex - 1 == selectedRowIndex &&
                           selectedRowIndex > 0 &&
                           selectedBindingSlotIndex == slotIndex;
            if (DungeonEscapeUiControls.Button(label, selected, uiTheme, GUILayout.Width(190f * GetPixelScale())))
            {
                StartRebinding(binding, slot);
                selectedBindingSlotIndex = slotIndex;
            }
        }

        private static string GetBindingSlotLabel(string slot)
        {
            return slot == "Primary" ? "Key" : slot;
        }

        private string GetRebindingPrompt()
        {
            if (rebindingInput == null)
            {
                return "";
            }

            return rebindingSlot == "Gamepad"
                ? "Press a gamepad button to change the binding for " + rebindingInput.Command + "."
                : "Press a key to change the binding for " + rebindingInput.Command + ".";
        }

        private void StartRebinding(InputBinding binding, string slot)
        {
            EnsureReferences();
            rebindingInput = binding;
            rebindingSlot = slot;
            rebindingStartFrame = Time.frameCount;
        }

        private void DrawMenuModalOverlay()
        {
            if (!IsMenuModalVisible() || rebindingInput != null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var hasChoices = MenuModalHasChoices();
            var choiceCount = hasChoices ? menuModalChoices.Count : 1;
            var compactDialog = !hasChoices || choiceCount <= 2;
            var height = compactDialog
                ? 170f * scale
                : Mathf.Min(Screen.height - 48f * scale, (150f + choiceCount * 42f) * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(menuModalTitle, titleStyle);
            GUILayout.Label(menuModalMessage, labelStyle);
            GUILayout.Space(10f * scale);

            if (hasChoices)
            {
                if (menuModalChoices.Count <= 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (var i = 0; i < menuModalChoices.Count; i++)
                    {
                        if (DungeonEscapeUiControls.Button(menuModalChoices[i], i == menuModalSelectedIndex, uiTheme, GUILayout.Width(120f * scale), GUILayout.Height(34f * scale)))
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }

                        if (i < menuModalChoices.Count - 1)
                        {
                            GUILayout.Space(10f * scale);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    for (var i = 0; i < menuModalChoices.Count; i++)
                    {
                        if (DungeonEscapeUiControls.Button(menuModalChoices[i], i == menuModalSelectedIndex, uiTheme))
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }
                    }
                }
            }
            else if (GUILayout.Button("OK", buttonStyle, GUILayout.Width(120f * scale)))
            {
                HideMenuModal();
            }

            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndArea();
        }

        private void DrawRebindingOverlay()
        {
            if (rebindingInput == null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var height = 150f * scale;
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            GUILayout.Label("Input Bindings", titleStyle);
            GUILayout.Label(GetRebindingPrompt(), labelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", buttonStyle, GUILayout.Width(120f * scale)))
            {
                rebindingInput = null;
                rebindingSlot = null;
            }

            GUILayout.EndArea();
        }

        private static void DrawModalBackdrop()
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private Vector2 BeginThemedScroll(Vector2 position, float height)
        {
            return GUILayout.BeginScrollView(
                position,
                false,
                false,
                GUIStyle.none,
                uiTheme == null ? GUI.skin.verticalScrollbar : uiTheme.VerticalScrollbarStyle,
                GUILayout.Height(height));
        }

        private static void EndThemedScroll()
        {
            GUILayout.EndScrollView();
        }

        private void BeginSelectableRow()
        {
            var rowIndex = drawingRowIndex++;
            DungeonEscapeUiControls.BeginSelectableRow(rowIndex, selectedRowIndex, uiTheme);
        }

        private static void EndSelectableRow()
        {
            DungeonEscapeUiControls.EndSelectableRow();
        }

        private void SelectRowOnMouseClick(int rowIndex)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedRowIndex = rowIndex;
        }

        private bool DrawCheckboxRow(bool value, string label)
        {
            return DungeonEscapeUiControls.CheckboxRow(value, label, uiTheme, GetPixelScale());
        }

        private float DrawSliderRow(string label, float value, float leftValue, float rightValue)
        {
            return DungeonEscapeUiControls.SliderRow(label, value, leftValue, rightValue, uiTheme);
        }

        private string DrawTextFieldRow(string label, string value, string fallback)
        {
            return DungeonEscapeUiControls.TextFieldRow(label, value, fallback, uiTheme);
        }

        private int GetSelectableRowCount()
        {
            var party = GetParty();
            switch (currentTab)
            {
                case MenuTab.Party:
                    return party == null ? 0 : party.Members.Count;
                case MenuTab.Inventory:
                    return GetSelectedInventoryHero() == null ? 0 : GetSelectedInventoryHero().Items.Count;
                case MenuTab.Quests:
                    return party == null || party.ActiveQuests == null ? 0 : party.ActiveQuests.Count;
                case MenuTab.Save:
                    return gameState == null ? 0 : gameState.ManualSaveSlotCount;
                case MenuTab.Settings:
                    return GetSettingsSelectableRowCount();
                default:
                    return 0;
            }
        }

        private int GetSettingsSelectableRowCount()
        {
            switch (currentSettingsTab)
            {
                case SettingsTab.General:
                    return 7;
                case SettingsTab.Ui:
                    return 9;
                case SettingsTab.Input:
                    return DungeonEscapeInput.GetBindings().Length + 2;
                case SettingsTab.Debug:
                    return 3;
                default:
                    return 0;
            }
        }

        private void MoveSelectedRow(int delta)
        {
            var count = GetSelectableRowCount();
            if (count <= 0)
            {
                selectedRowIndex = 0;
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex + delta, 0, count - 1);
            ScrollActiveListToSelectedRow();
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
                AdjustSelectedPartyMember(delta);
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

        private void ActivateSelectedRow()
        {
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

        private void ActivateSelectedSaveSlot()
        {
            if (gameState == null)
            {
                return;
            }

            var slots = gameState.GetManualSaveSlots();
            if (selectedRowIndex < 0)
            {
                return;
            }

            if (selectedRowIndex >= slots.Count)
            {
                SaveManual(selectedRowIndex);
                return;
            }

            if (IsUsableSave(slots[selectedRowIndex]))
            {
                ConfirmLoadManual(selectedRowIndex);
            }
            else
            {
                SaveManual(selectedRowIndex);
            }
        }

        private void AdjustSelectedPartyMember(int delta)
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var activeMembers = party.ActiveMembers.ToList();
            if (selectedRowIndex < 0 || selectedRowIndex >= activeMembers.Count)
            {
                return;
            }

            if (delta < 0)
            {
                ApplyPartyChange(() => gameState.MovePartyMemberUp(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Max(0, selectedRowIndex - 1);
            }
            else if (delta > 0)
            {
                ApplyPartyChange(() => gameState.MovePartyMemberDown(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Min(activeMembers.Count - 1, selectedRowIndex + 1);
            }
        }

        private void ActivateSelectedPartyMember()
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var activeMembers = party.ActiveMembers.ToList();
            if (selectedRowIndex < activeMembers.Count)
            {
                ApplyPartyChange(() => gameState.DeactivatePartyMember(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(GetSelectableRowCount() - 1, 0));
                return;
            }

            var inactiveMembers = party.InactiveMembers.ToList();
            var inactiveIndex = selectedRowIndex - activeMembers.Count;
            if (inactiveIndex >= 0 && inactiveIndex < inactiveMembers.Count)
            {
                ApplyPartyChange(() => gameState.ActivatePartyMember(inactiveMembers[inactiveIndex]));
            }
        }

        private Hero GetSelectedInventoryHero()
        {
            var party = GetParty();
            if (party == null)
            {
                return null;
            }

            var members = GetInventoryMembers(party);
            if (members.Count == 0)
            {
                return null;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            return members[selectedHeroIndex];
        }

        private void AdjustSelectedInventoryHero(int delta)
        {
            var party = GetParty();
            if (party == null || party.Members.Count <= 1)
            {
                return;
            }

            var members = GetInventoryMembers(party);
            selectedHeroIndex = (selectedHeroIndex + delta + members.Count) % members.Count;
            selectedRowIndex = 0;
        }

        private static List<Hero> GetInventoryMembers(Party party)
        {
            return party == null
                ? new List<Hero>()
                : party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
        }

        private void ActivateSelectedInventoryItem()
        {
            var hero = GetSelectedInventoryHero();
            if (hero == null || selectedRowIndex < 0 || selectedRowIndex >= hero.Items.Count)
            {
                return;
            }

            var item = hero.Items[selectedRowIndex];
            if (gameState != null && gameState.CanUseHeroItem(hero, item))
            {
                ShowUseItemTargetPicker(hero, item);
            }
            else if (item.IsEquipped)
            {
                ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
            }
            else if (hero.CanEquipItem(item))
            {
                ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
            }
        }

        private void AdjustSelectedSetting(int delta)
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            if (selectedRowIndex == 0)
            {
                CycleSettingsTab(delta);
                return;
            }

            if (currentSettingsTab == SettingsTab.General)
            {
                switch (selectedRowIndex - 1)
                {
                    case 0:
                        settings.UiScale = Mathf.Clamp((settings.UiScale <= 0f ? 1f : settings.UiScale) + 0.05f * delta, MinUiScale, MaxUiScale);
                        ApplySettings(settings);
                        break;
                    case 2:
                        settings.SprintBoost = Mathf.Clamp((settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost) + 0.05f * delta, 1f, 3f);
                        ApplySettings(settings);
                        break;
                    case 3:
                        settings.TurnMoveDelaySeconds = Mathf.Clamp(GetTurnMoveDelay(settings) + 0.01f * delta, 0f, 0.3f);
                        ApplySettings(settings);
                        break;
                    case 5:
                        settings.AutoSaveIntervalSeconds = Mathf.Clamp(GetAutoSaveInterval(settings) + 5f * delta, 5f, 300f);
                        ApplySettings(settings);
                        break;
                }
            }
            else if (currentSettingsTab == SettingsTab.Ui)
            {
                switch (selectedRowIndex - 1)
                {
                    case 1:
                        settings.UiBackgroundAlpha = Mathf.Clamp01(settings.UiBackgroundAlpha + 0.05f * delta);
                        ApplySettings(settings);
                        break;
                    case 5:
                        settings.UiBorderThickness = Mathf.Clamp(settings.UiBorderThickness + delta, 2, 12);
                        ApplySettings(settings);
                        break;
                }
            }
            else if (currentSettingsTab == SettingsTab.Input)
            {
                selectedBindingSlotIndex = (selectedBindingSlotIndex + delta + 2) % 2;
            }
        }

        private void ActivateSelectedSetting()
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            if (selectedRowIndex == 0)
            {
                return;
            }

            var settingsRowIndex = selectedRowIndex - 1;
            if (currentSettingsTab == SettingsTab.General && settingsRowIndex == 1)
            {
                settings.IsFullScreen = !settings.IsFullScreen;
                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.General && settingsRowIndex == 4)
            {
                settings.AutoSaveEnabled = !settings.AutoSaveEnabled;
                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.Debug)
            {
                if (settingsRowIndex == 0)
                {
                    settings.MapDebugInfo = !settings.MapDebugInfo;
                }
                else if (settingsRowIndex == 1)
                {
                    settings.ShowHiddenObjects = !settings.ShowHiddenObjects;
                }

                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.Input)
            {
                ActivateSelectedInputBinding();
            }
        }

        private void ActivateSelectedInputBinding()
        {
            var bindings = DungeonEscapeInput.GetBindings().OrderBy(item => item.Command).ToList();
            var bindingIndex = selectedRowIndex - 1;
            if (bindingIndex >= bindings.Count)
            {
                DungeonEscapeInput.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
                return;
            }

            if (bindingIndex < 0)
            {
                return;
            }

            StartRebinding(bindings[bindingIndex], selectedBindingSlotIndex == 0 ? "Primary" : "Gamepad");
        }

        private int GetMenuMoveX()
        {
            var pressed = DungeonEscapeInput.GetMoveXDown();
            if (pressed != 0)
            {
                repeatingMenuMoveX = pressed;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveX();
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
            var moveX = DungeonEscapeInput.GetMoveX();
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
            var pressed = DungeonEscapeInput.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMenuMoveY = pressed;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveY();
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

        private void ApplySettings(Settings settings)
        {
            DungeonEscapeSettingsCache.Set(settings);
            DungeonEscapeSettingsCache.Save();
            DungeonEscapeDisplaySettings.Apply(settings);
            DungeonEscapeUiSettings.GetOrCreate().ApplySettings(settings);
            lastThemeSignature = null;
            if (mapView == null)
            {
                mapView = FindAnyObjectByType<TiledMapView>();
            }

            if (mapView != null)
            {
                mapView.RefreshRender();
            }
        }

        private static string GetThemeValue(string value, string fallback)
        {
            return DungeonEscapeUiTheme.GetThemeValue(value, fallback);
        }

        private static GUIStyle GetRarityStyle(ItemInstance item, GUIStyle baseStyle)
        {
            var style = new GUIStyle(baseStyle);
            if (item == null)
            {
                return style;
            }

            style.normal.textColor = GetRarityColor(item.Rarity, baseStyle.normal.textColor);
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;
            return style;
        }

        private static Color GetRarityColor(Rarity rarity, Color commonColor)
        {
            switch (rarity)
            {
                case Rarity.Uncommon:
                    return Color.green;
                case Rarity.Rare:
                    return Color.blue;
                case Rarity.Epic:
                    return new Color(0.55f, 0f, 0.75f);
                case Rarity.Legendary:
                    return new Color(1f, 0.5f, 0f);
                case Rarity.Common:
                default:
                    return commonColor;
            }
        }

        private static int GetBorderThickness(Settings settings)
        {
            return DungeonEscapeUiTheme.GetBorderThickness(settings);
        }

        private static int GetMaxPartyMembers()
        {
            return DungeonEscapeSettingsCache.Current == null || DungeonEscapeSettingsCache.Current.MaxPartyMembers <= 0
                ? 4
                : DungeonEscapeSettingsCache.Current.MaxPartyMembers;
        }

        private Party GetParty()
        {
            EnsureReferences();
            return gameState == null ? null : gameState.Party;
        }

        private void EnsureReferences()
        {
            if (gameState == null)
            {
                gameState = DungeonEscapeGameState.GetOrCreate();
            }

            if (messageBox == null)
            {
                messageBox = FindAnyObjectByType<DungeonEscapeMessageBox>();
                if (messageBox == null)
                {
                    messageBox = new GameObject("DungeonEscapeMessageBox").AddComponent<DungeonEscapeMessageBox>();
                }
            }

            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            if (mapView == null)
            {
                mapView = FindAnyObjectByType<TiledMapView>();
            }
        }

        private void PrewarmUiAssets()
        {
            if (uiAssetsPrewarmed)
            {
                return;
            }

            EnsureReferences();
            DungeonEscapeUiAssetResolver.Preload(gameState == null ? null : gameState.Party);
            uiAssetsPrewarmed = true;
        }

        private DungeonEscapeMessageBox GetMessageBox()
        {
            EnsureReferences();
            return messageBox;
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (titleStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = DungeonEscapeUiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            titleStyle = uiTheme.TitleStyle;
            labelStyle = uiTheme.LabelStyle;
            smallStyle = uiTheme.SmallStyle;
            buttonStyle = uiTheme.ButtonStyle;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}
