using System;
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
            Settings
        }

        private const float MinUiScale = 0.5f;
        private const float MaxUiScale = 3f;
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static bool isOpen;

        private DungeonEscapeGameState gameState;
        private DungeonEscapeUiSettings uiSettings;
        private TiledMapView mapView;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tabStyle;
        private GUIStyle selectedTabStyle;
        private float lastPixelScale;
        private MenuTab currentTab = MenuTab.Party;
        private Vector2 scrollPosition;
        private int selectedHeroIndex;
        private InputBinding rebindingInput;
        private string rebindingSlot;
        private int repeatingMenuMoveX;
        private float nextMenuMoveXTime;
        private int repeatingMenuMoveY;
        private float nextMenuMoveYTime;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private void Update()
        {
            if (rebindingInput != null)
            {
                string keyCode;
                if (DungeonEscapeInput.TryCaptureBinding(out keyCode))
                {
                    DungeonEscapeInput.SetBinding(rebindingInput, rebindingSlot, keyCode);
                    DungeonEscapeSettingsCache.Save();
                    rebindingInput = null;
                    rebindingSlot = null;
                }

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
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuParty))
            {
                Toggle(MenuTab.Party);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuInventory))
            {
                Toggle(MenuTab.Inventory);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuQuests))
            {
                Toggle(MenuTab.Quests);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuSettings))
            {
                Toggle(MenuTab.Settings);
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

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 980f * scale);
            var height = Mathf.Min(Screen.height - 32f * scale, 680f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, rect.height - 28f * scale));
            DrawHeader();
            DrawTabs();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            DrawCurrentTab();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Toggle(MenuTab tab)
        {
            if (isOpen && currentTab == tab)
            {
                isOpen = false;
                return;
            }

            currentTab = tab;
            scrollPosition = Vector2.zero;
            isOpen = true;
            ResetMenuNavigationRepeat();
        }

        private void CycleTab(int delta)
        {
            var tabCount = Enum.GetValues(typeof(MenuTab)).Length;
            var next = ((int)currentTab + delta + tabCount) % tabCount;
            currentTab = (MenuTab)next;
            scrollPosition = Vector2.zero;
            ResetMenuNavigationRepeat();
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
                scrollPosition.y = Mathf.Max(0f, scrollPosition.y + moveY * 42f * GetPixelScale());
            }

            if (currentTab != MenuTab.Inventory)
            {
                return;
            }

            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var memberCount = party.Members.Count;
            if (memberCount <= 1)
            {
                return;
            }

            var moveX = GetMenuMoveX();
            if (moveX == 0)
            {
                return;
            }

            selectedHeroIndex = (selectedHeroIndex + moveX + memberCount) % memberCount;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dungeon Escape", titleStyle);
            GUILayout.FlexibleSpace();
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
            DrawTab(MenuTab.Inventory, "Inventory");
            DrawTab(MenuTab.Quests, "Quests");
            DrawTab(MenuTab.Settings, "Settings");
            GUILayout.EndHorizontal();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawTab(MenuTab tab, string label)
        {
            var style = currentTab == tab ? selectedTabStyle : tabStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(34f * GetPixelScale())))
            {
                currentTab = tab;
                scrollPosition = Vector2.zero;
            }
        }

        private void DrawCurrentTab()
        {
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
            GUILayout.Label("Active Party (" + activeMembers.Count + "/" + GetMaxPartyMembers() + ")", titleStyle);
            for (var i = 0; i < activeMembers.Count; i++)
            {
                DrawHeroStatus(activeMembers[i], true);
                DrawActivePartyControls(activeMembers[i], i, activeMembers.Count);
            }

            var inactive = party.InactiveMembers.ToList();
            if (inactive.Count > 0)
            {
                GUILayout.Space(10f * GetPixelScale());
                GUILayout.Label("Reserve", titleStyle);
                foreach (var hero in inactive)
                {
                    DrawHeroStatus(hero, false);
                    DrawReservePartyControls(hero, activeMembers.Count);
                }
            }
        }

        private void DrawActivePartyControls(Hero hero, int index, int activeCount)
        {
            GUILayout.BeginHorizontal();
            GUI.enabled = index > 0;
            if (GUILayout.Button("Move Up", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberUp(hero));
            }

            GUI.enabled = index < activeCount - 1;
            if (GUILayout.Button("Move Down", buttonStyle, GUILayout.Width(128f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberDown(hero));
            }

            GUI.enabled = activeCount > 1;
            if (GUILayout.Button("Reserve", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.DeactivatePartyMember(hero));
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawReservePartyControls(Hero hero, int activeCount)
        {
            GUI.enabled = activeCount < GetMaxPartyMembers();
            if (GUILayout.Button("Add to Party", buttonStyle, GUILayout.Width(144f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.ActivatePartyMember(hero));
            }

            GUI.enabled = true;
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawHeroStatus(Hero hero, bool active)
        {
            GUILayout.BeginVertical(GUI.skin.box);
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
        }

        private void ApplyPartyChange(Func<bool> action)
        {
            EnsureReferences();
            if (action == null || !action())
            {
                return;
            }

            var player = FindObjectOfType<PlayerGridController>();
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

            var members = party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
            if (members.Count == 0)
            {
                GUILayout.Label("No party members.", labelStyle);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                if (GUILayout.Toggle(selectedHeroIndex == i, members[i].Name, buttonStyle))
                {
                    selectedHeroIndex = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            GUILayout.Label(hero.Name + "'s Inventory (" + hero.Items.Count + "/" + Party.MaxItems + ")", titleStyle);
            if (hero.Items.Count == 0)
            {
                GUILayout.Label("No items.", labelStyle);
                return;
            }

            foreach (var item in hero.Items)
            {
                var equipped = item.IsEquipped ? " [E]" : "";
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.NameWithStats + equipped + "    " + item.Type + "    " + item.Gold + "g", labelStyle);
                if (item.IsEquipped)
                {
                    if (GUILayout.Button("Unequip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                    }
                }
                else
                {
                    GUI.enabled = hero.CanEquipItem(item);
                    if (GUILayout.Button("Equip", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                    }

                    GUI.enabled = true;
                }

                GUILayout.EndHorizontal();
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

            foreach (var activeQuest in party.ActiveQuests)
            {
                Quest quest;
                if (DungeonEscapeGameDataCache.Current == null ||
                    !DungeonEscapeGameDataCache.Current.TryGetQuest(activeQuest.Id, out quest))
                {
                    GUILayout.Label(activeQuest.Id + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                    continue;
                }

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(quest.Name + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                if (!string.IsNullOrEmpty(quest.Description))
                {
                    GUILayout.Label(quest.Description, smallStyle);
                }

                var currentStage = quest.Stages == null
                    ? null
                    : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                {
                    GUILayout.Label(currentStage.Description, smallStyle);
                }

                GUILayout.EndVertical();
            }
        }

        private void DrawSettings()
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                GUILayout.Label("Settings are not loaded.", labelStyle);
                return;
            }

            GUI.changed = false;
            GUILayout.Label("UI Scale: " + settings.UiScale.ToString("0.00"), labelStyle);
            settings.UiScale = GUILayout.HorizontalSlider(settings.UiScale <= 0f ? 1f : settings.UiScale, MinUiScale, MaxUiScale);
            settings.MapDebugInfo = GUILayout.Toggle(settings.MapDebugInfo, "Map debug info", labelStyle);
            settings.ShowHiddenObjects = GUILayout.Toggle(settings.ShowHiddenObjects, "Show hidden map objects", labelStyle);
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Sprint Boost: " + settings.SprintBoost.ToString("0.00"), labelStyle);
            settings.SprintBoost = GUILayout.HorizontalSlider(settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost, 1f, 3f);

            if (GUI.changed)
            {
                ApplySettings(settings);
            }

            GUILayout.Space(14f * GetPixelScale());
            DrawInputBindings();
        }

        private void DrawInputBindings()
        {
            var bindings = DungeonEscapeInput.GetBindings();
            GUILayout.Label("Input Bindings", titleStyle);
            if (rebindingInput != null)
            {
                GUILayout.Label("Press a key or gamepad button for " + rebindingInput.Command + " " + rebindingSlot + ".", labelStyle);
                if (GUILayout.Button("Cancel Rebind", buttonStyle, GUILayout.Width(160f * GetPixelScale())))
                {
                    rebindingInput = null;
                    rebindingSlot = null;
                }
            }

            foreach (var binding in bindings.OrderBy(item => item.Command))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(binding.Command + ": " + DungeonEscapeInput.GetBindingText(binding), labelStyle);
                GUILayout.BeginHorizontal();
                DrawBindingButton(binding, "Primary", binding.Primary);
                DrawBindingButton(binding, "Secondary", binding.Secondary);
                DrawBindingButton(binding, "Gamepad", binding.Gamepad);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Reset Input Bindings", buttonStyle, GUILayout.Width(220f * GetPixelScale())))
            {
                DungeonEscapeInput.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
            }
        }

        private void DrawBindingButton(InputBinding binding, string slot, string currentValue)
        {
            var label = slot + ": " + (string.IsNullOrEmpty(currentValue) || currentValue == KeyCode.None.ToString() ? "-" : currentValue);
            if (GUILayout.Button(label, buttonStyle, GUILayout.Width(190f * GetPixelScale())))
            {
                rebindingInput = binding;
                rebindingSlot = slot;
            }
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
        }

        private void ApplySettings(Settings settings)
        {
            DungeonEscapeSettingsCache.Set(settings);
            DungeonEscapeSettingsCache.Save();
            DungeonEscapeUiSettings.GetOrCreate().ApplySettings(settings);
            if (mapView == null)
            {
                mapView = FindObjectOfType<TiledMapView>();
            }

            if (mapView != null)
            {
                mapView.RefreshRender();
            }
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

            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            if (mapView == null)
            {
                mapView = FindObjectOfType<TiledMapView>();
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            if (titleStyle != null && Mathf.Approximately(lastPixelScale, scale))
            {
                return;
            }

            lastPixelScale = scale;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                wordWrap = true
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                normal = { textColor = Color.white },
                wordWrap = true
            };
            smallStyle = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.RoundToInt(14f * scale)
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15f * scale)
            };
            tabStyle = new GUIStyle(buttonStyle);
            selectedTabStyle = new GUIStyle(buttonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
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
