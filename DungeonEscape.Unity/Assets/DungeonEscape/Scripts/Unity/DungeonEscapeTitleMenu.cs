using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeTitleMenu : MonoBehaviour
    {
        private enum TitleMode
        {
            Main,
            Load
        }

        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static bool isOpen;

        private DungeonEscapeGameState gameState;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle panelStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private TitleMode mode;
        private int selectedIndex;
        private int repeatingMoveY;
        private float nextMoveYTime;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private void Start()
        {
            isOpen = true;
            EnsureReferences();
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureReferences();
            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                selectedIndex = Mathf.Clamp(selectedIndex + moveY, 0, Mathf.Max(GetOptionCount() - 1, 0));
            }

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                if (mode == TitleMode.Load)
                {
                    mode = TitleMode.Main;
                    selectedIndex = 0;
                    ResetNavigationRepeat();
                }
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                ActivateSelected();
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

            var previousDepth = GUI.depth;
            GUI.depth = -1000;
            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var height = mode == TitleMode.Load
                ? Mathf.Min(Screen.height - 32f * scale, 560f * scale)
                : Mathf.Min(Screen.height - 32f * scale, 420f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            GUI.Box(rect, GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            if (mode == TitleMode.Load)
            {
                DrawLoadMenu();
            }
            else
            {
                DrawMainMenu();
            }

            GUILayout.EndArea();
            GUI.depth = previousDepth;
        }

        private void DrawMainMenu()
        {
            GUILayout.Label("Dungeon Escape", titleStyle);
            GUILayout.Space(8f * GetPixelScale());
            var quickSave = gameState == null ? null : gameState.GetQuickSaveSlot();
            GUILayout.Label("Continue: " + DungeonEscapeGameState.GetGameSaveSummary(quickSave), smallStyle);
            GUILayout.Space(12f * GetPixelScale());

            var rows = GetMainRows().ToList();
            for (var i = 0; i < rows.Count; i++)
            {
                var enabled = rows[i].Enabled;
                var previousEnabled = GUI.enabled;
                GUI.enabled = enabled;
                if (DungeonEscapeUiControls.Button(rows[i].Label, selectedIndex == i, uiTheme, GUILayout.Height(40f * GetPixelScale())))
                {
                    selectedIndex = i;
                    if (enabled)
                    {
                        rows[i].Action();
                    }
                }

                GUI.enabled = previousEnabled;
                GUILayout.Space(6f * GetPixelScale());
            }
        }

        private void DrawLoadMenu()
        {
            GUILayout.Label("Load Game", titleStyle);
            GUILayout.Space(8f * GetPixelScale());
            var slots = gameState == null
                ? new List<GameSave>()
                : gameState.GetManualSaveSlots().ToList();

            for (var i = 0; i < slots.Count; i++)
            {
                var label = "Slot " + (i + 1) + ": " + DungeonEscapeGameState.GetGameSaveTitle(slots[i]) +
                            "\n" + DungeonEscapeGameState.GetGameSaveSummary(slots[i]);
                if (DungeonEscapeUiControls.Button(label, selectedIndex == i, uiTheme, GUILayout.Height(58f * GetPixelScale())))
                {
                    selectedIndex = i;
                    TryLoadSlot(i);
                }

                GUILayout.Space(6f * GetPixelScale());
            }

            if (DungeonEscapeUiControls.Button("Back", selectedIndex == slots.Count, uiTheme, GUILayout.Height(38f * GetPixelScale())))
            {
                mode = TitleMode.Main;
                selectedIndex = 0;
                ResetNavigationRepeat();
            }
        }

        private IEnumerable<TitleRow> GetMainRows()
        {
            yield return new TitleRow
            {
                Label = "Continue",
                Enabled = gameState != null && gameState.HasQuickSave(),
                Action = ContinueGame
            };
            yield return new TitleRow
            {
                Label = "New Game",
                Enabled = true,
                Action = NewGame
            };
            yield return new TitleRow
            {
                Label = "Load Game",
                Enabled = true,
                Action = ShowLoadMenu
            };
            yield return new TitleRow
            {
                Label = "Quit",
                Enabled = true,
                Action = Quit
            };
        }

        private int GetOptionCount()
        {
            if (mode == TitleMode.Load)
            {
                return (gameState == null ? 0 : gameState.GetManualSaveSlots().Count) + 1;
            }

            return GetMainRows().Count();
        }

        private void ActivateSelected()
        {
            if (mode == TitleMode.Load)
            {
                var slots = gameState == null ? new List<GameSave>() : gameState.GetManualSaveSlots().ToList();
                if (selectedIndex >= slots.Count)
                {
                    mode = TitleMode.Main;
                    selectedIndex = 0;
                    ResetNavigationRepeat();
                    return;
                }

                TryLoadSlot(selectedIndex);
                return;
            }

            var rows = GetMainRows().ToList();
            if (selectedIndex >= 0 && selectedIndex < rows.Count && rows[selectedIndex].Enabled)
            {
                rows[selectedIndex].Action();
            }
        }

        private void ContinueGame()
        {
            if (gameState != null && gameState.LoadQuick())
            {
                Close();
            }
        }

        private void NewGame()
        {
            if (gameState != null)
            {
                gameState.RestartNewGame();
            }

            Close();
        }

        private void ShowLoadMenu()
        {
            mode = TitleMode.Load;
            selectedIndex = 0;
            ResetNavigationRepeat();
        }

        private void TryLoadSlot(int slotIndex)
        {
            if (gameState != null && gameState.LoadManual(slotIndex))
            {
                Close();
            }
        }

        private static void Quit()
        {
            Application.Quit();
        }

        private static void Close()
        {
            isOpen = false;
        }

        private int GetMenuMoveY()
        {
            var pressed = DungeonEscapeInput.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMoveY = pressed;
                nextMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveY();
            if (held == 0)
            {
                repeatingMoveY = 0;
                nextMoveYTime = 0f;
                return 0;
            }

            if (held != repeatingMoveY)
            {
                repeatingMoveY = held;
                nextMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMoveYTime)
            {
                return 0;
            }

            nextMoveYTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetNavigationRepeat()
        {
            repeatingMoveY = 0;
            nextMoveYTime = 0f;
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
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (titleStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, System.StringComparison.Ordinal))
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
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }

        private sealed class TitleRow
        {
            public string Label { get; set; }
            public bool Enabled { get; set; }
            public System.Action Action { get; set; }
        }
    }
}
