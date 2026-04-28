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

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Toggle(MenuTab.Party);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                Toggle(MenuTab.Inventory);
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                Toggle(MenuTab.Quests);
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                Toggle(MenuTab.Settings);
            }
            else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                isOpen = false;
            }
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
            foreach (var hero in party.ActiveMembers)
            {
                DrawHeroStatus(hero, true);
            }

            var inactive = party.InactiveMembers.ToList();
            if (inactive.Count > 0)
            {
                GUILayout.Space(10f * GetPixelScale());
                GUILayout.Label("Reserve", titleStyle);
                foreach (var hero in inactive)
                {
                    DrawHeroStatus(hero, false);
                }
            }
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
                GUILayout.Label(item.NameWithStats + equipped + "    " + item.Type + "    " + item.Gold + "g", labelStyle);
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
