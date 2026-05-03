using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Tools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class TitleMenu : MonoBehaviour
    {
        private enum TitleMode
        {
            Main,
            Load,
            Create
        }

        private enum CreateDropdown
        {
            None,
            Gender,
            Class
        }

        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;
        private const int TitleGuiDepth = -3000;
        private const int CreateNameIndex = 0;
        private const int CreateGenerateNameIndex = 1;
        private const int CreateGenderIndex = 2;
        private const int CreateClassIndex = 3;
        private const int CreateRerollIndex = 4;
        private const int CreateStartIndex = 5;
        private const int CreateBackIndex = 6;
        private const string MainMenuBackgroundAssetPath = "Assets/DungeonEscape/Images/ui/mainmenue.png";
        private const string SecondaryMenuBackgroundAssetPath = "Assets/DungeonEscape/Images/ui/menu2.png";

        private static bool isOpen;

        private GameState gameState;
        private UiSettings uiSettings;
        private UiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle mainMenuButtonStyle;
        private GUIStyle selectedMainMenuButtonStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle panelStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private TitleMode mode;
        private int selectedIndex;
        private int repeatingMoveY;
        private float nextMoveYTime;
        private string createPlayerName = "Player";
        private bool createPlayerNameInitialized;
        private Class createPlayerClass = Class.Hero;
        private Gender createPlayerGender = Gender.Male;
        private CreateDropdown activeCreateDropdown;
        private Rect createMenuAreaOffset;
        private Rect genderDropdownAnchor;
        private Rect classDropdownAnchor;
        private Vector2 genderDropdownScrollPosition;
        private Vector2 classDropdownScrollPosition;
        private Vector2 loadQuestScrollPosition;
        private Hero createPreviewHero;
        private int selectedDropdownIndex;
        private bool focusCreateNameNextGui;
        private Texture2D mainMenuBackground;
        private Texture2D secondaryMenuBackground;
        private bool ownsMainMenuBackground;
        private bool ownsSecondaryMenuBackground;
        private bool waitingForConfirmRelease;
        private bool titleActionPending;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        public static void OpenMainMenu()
        {
            var menu = FindAnyObjectByType<TitleMenu>();
            if (menu == null)
            {
                menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            }

            menu.mode = TitleMode.Main;
            menu.selectedIndex = 0;
            menu.WaitForConfirmRelease();
            menu.ResetNavigationRepeat();
            Audio.GetOrCreate().PrewarmSoundEffects("confirm", "select");
            isOpen = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            isOpen = false;
        }

        private void Awake()
        {
            OpenAtStartup();
        }

        private void Start()
        {
            OpenAtStartup();
            EnsureReferences();
        }

        private void OpenAtStartup()
        {
            if (SettingsCache.Current.SkipSplashAndLoadQuickSave)
            {
                return;
            }

            if (isOpen)
            {
                return;
            }

            isOpen = true;
            mode = TitleMode.Main;
            Audio.GetOrCreate().PlayMusic("first-story");
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (SplashScreen.IsVisible)
            {
                return;
            }

            EnsureReferences();
            if (waitingForConfirmRelease)
            {
                if (GetConfirmHeld() || Input.GetMouseButton(0))
                {
                    return;
                }

                waitingForConfirmRelease = false;
            }

            if (activeCreateDropdown != CreateDropdown.None)
            {
                HandleCreateDropdownInput();
                return;
            }

            HandleNavigation();

            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
                if (mode == TitleMode.Load)
                {
                    mode = TitleMode.Main;
                    selectedIndex = 0;
                    WaitForConfirmRelease();
                    ResetNavigationRepeat();
                }
            }
            else if (GetConfirmDown())
            {
                UiControls.PlayConfirmSound();
                ActivateSelected();
            }
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            if (SplashScreen.IsVisible)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = TitleGuiDepth;
            DrawBackgroundForCurrentMode();

            var scale = GetPixelScale();
            if (mode == TitleMode.Main)
            {
                DrawMainMenuStandalone(scale);
                GUI.depth = previousDepth;
                GUI.color = previousColor;
                return;
            }

            if (mode == TitleMode.Create)
            {
                DrawCreateMenuStandalone(scale);
                DrawCreateDropdownOverlay();
                GUI.depth = previousDepth;
                GUI.color = previousColor;
                return;
            }

            if (mode == TitleMode.Load)
            {
                DrawLoadMenuStandalone(scale);
                GUI.depth = previousDepth;
                GUI.color = previousColor;
                return;
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }

        private void OnDestroy()
        {
            if (mainMenuBackground != null && ownsMainMenuBackground)
            {
                Destroy(mainMenuBackground);
            }

            if (secondaryMenuBackground != null && ownsSecondaryMenuBackground)
            {
                Destroy(secondaryMenuBackground);
            }

            mainMenuBackground = null;
            secondaryMenuBackground = null;
            ownsMainMenuBackground = false;
            ownsSecondaryMenuBackground = false;
        }

        private void DrawBackgroundForCurrentMode()
        {
            var background = mode == TitleMode.Main ? GetMainMenuBackground() : GetSecondaryMenuBackground();
            GUI.color = Color.white;
            if (background != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
                return;
            }

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private Texture2D GetMainMenuBackground()
        {
            if (mainMenuBackground == null)
            {
                mainMenuBackground = LoadTexture(MainMenuBackgroundAssetPath, "DungeonEscapeMainMenuBackground", out ownsMainMenuBackground);
            }

            return mainMenuBackground;
        }

        private Texture2D GetSecondaryMenuBackground()
        {
            if (secondaryMenuBackground == null)
            {
                secondaryMenuBackground = LoadTexture(SecondaryMenuBackgroundAssetPath, "DungeonEscapeSecondaryMenuBackground", out ownsSecondaryMenuBackground);
            }

            return secondaryMenuBackground;
        }

        private static Texture2D LoadTexture(string assetPath, string textureName, out bool ownsTexture)
        {
            ownsTexture = false;

#if UNITY_EDITOR
            var editorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (editorTexture != null)
            {
                return editorTexture;
            }
#endif

            var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Title menu background image not found: " + assetPath);
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning("Could not load title menu background image: " + assetPath);
                return null;
            }

            texture.name = textureName;
            ownsTexture = true;
            return texture;
        }

        private void DrawMainMenuStandalone(float scale)
        {
            var rows = GetMainRows().ToList();
            var buttonWidth = Mathf.Min(250f * scale, Screen.width - 48f * scale);
            var buttonHeight = 32f * scale;
            var buttonGap = 18f * scale;
            var totalHeight = rows.Count * buttonHeight + Mathf.Max(0, rows.Count - 1) * buttonGap;
            var bottomHalfY = Screen.height / 2f;
            var startY = bottomHalfY + Mathf.Max(0f, (Screen.height - bottomHalfY - totalHeight) / 2f);
            var x = (Screen.width - buttonWidth) / 2f;
            for (var i = 0; i < rows.Count; i++)
            {
                var rect = new Rect(x, startY + i * (buttonHeight + buttonGap), buttonWidth, buttonHeight);
                var enabled = rows[i].Enabled;
                var previousEnabled = GUI.enabled;
                GUI.enabled = enabled;
                if (UiControls.Button(rect, rows[i].Label, selectedIndex == i ? selectedMainMenuButtonStyle : mainMenuButtonStyle) &&
                    !waitingForConfirmRelease)
                {
                    selectedIndex = i;
                    if (enabled)
                    {
                        ActivateTitleAction(rows[i].Action);
                    }
                }

                GUI.enabled = previousEnabled;
            }
        }

        private void DrawLoadMenuStandalone(float scale)
        {
            var loadTitleStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(32f * scale),
                fontStyle = FontStyle.Normal
            };

            var slots = gameState == null
                ? new List<GameSave>()
                : gameState.GetManualSaveSlots().ToList();

            selectedIndex = Mathf.Clamp(selectedIndex, 0, GetLoadBackIndex(slots.Count));
            var width = Mathf.Min(640f * scale, Screen.width - 32f * scale);
            var height = 330f * scale;
            var titleHeight = 44f * scale;
            var titleGap = 14f * scale;
            var backWidth = 82f * scale;
            var backHeight = 32f * scale;
            var backGap = 10f * scale;
            var totalHeight = titleHeight + titleGap + height + backGap + backHeight;
            var titleY = Mathf.Max(16f * scale, (Screen.height - totalHeight) / 2f);
            var panelPadding = 16f * scale;
            var panelRect = new Rect(
                (Screen.width - width) / 2f - panelPadding,
                titleY - panelPadding,
                width + panelPadding * 2f,
                totalHeight + panelPadding * 2f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(0f, titleY, Screen.width, titleHeight), "Load Quest", loadTitleStyle);

            var area = new Rect((Screen.width - width) / 2f, titleY + titleHeight + titleGap, width, height);
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical();
            var visibleSlotCount = Mathf.Min(slots.Count, 5);
            var listHeight = visibleSlotCount * 48f * scale + Mathf.Max(visibleSlotCount - 1, 0) * 8f * scale + 4f * scale;
            if (slots.Count > 5)
            {
                var previousThumb = GUI.skin.verticalScrollbarThumb;
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
                loadQuestScrollPosition = GUILayout.BeginScrollView(
                    loadQuestScrollPosition,
                    false,
                    true,
                    GUIStyle.none,
                    uiTheme.VerticalScrollbarStyle,
                    GUILayout.Height(listHeight));
                DrawLoadSlotButtons(slots, scale);
                GUILayout.EndScrollView();
                GUI.skin.verticalScrollbarThumb = previousThumb;
            }
            else
            {
                DrawLoadSlotButtons(slots, scale);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            var backRect = new Rect((Screen.width - backWidth) / 2f, area.yMax + backGap, backWidth, backHeight);
            if (UiControls.Button(backRect, "Back", selectedIndex == GetLoadBackIndex(slots.Count) ? selectedMainMenuButtonStyle : mainMenuButtonStyle) &&
                !waitingForConfirmRelease)
            {
                ShowMainMenu();
            }
        }

        private void DrawLoadSlotButtons(IList<GameSave> slots, float scale)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (UiControls.Button(
                    GameState.GetGameSaveTitle(slots[i]) + "\n" +
                    GameState.GetGameSaveSummary(slots[i]),
                    selectedIndex == GetLoadSaveIndex(i),
                    uiTheme,
                    GUILayout.Height(48f * scale),
                    GUILayout.Width(500f * scale)) &&
                    !waitingForConfirmRelease)
                {
                    selectedIndex = GetLoadSaveIndex(i);
                    TryLoadSlot(i);
                }

                if (UiControls.Button("Delete", selectedIndex == GetLoadDeleteIndex(i), uiTheme, GUILayout.Height(48f * scale), GUILayout.Width(92f * scale)) &&
                    !waitingForConfirmRelease)
                {
                    selectedIndex = GetLoadDeleteIndex(i);
                    DeleteSlot(i);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(8f * scale);
            }
        }

        private void DrawCreateMenuStandalone(float scale)
        {
            var newQuestTitleStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(32f * scale),
                fontStyle = FontStyle.Normal
            };
            EnsureCreatePreviewHero();

            var width = Mathf.Min(660f * scale, Screen.width - 32f * scale);
            var height = 230f * scale;
            var titleHeight = 44f * scale;
            var titleGap = 8f * scale;
            var totalHeight = titleHeight + titleGap + height;
            var titleY = Mathf.Max(16f * scale, (Screen.height - totalHeight) / 2f);
            var panelPadding = 16f * scale;
            var panelRect = new Rect(
                (Screen.width - width) / 2f - panelPadding,
                titleY - panelPadding,
                width + panelPadding * 2f,
                totalHeight + panelPadding * 2f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(0f, titleY, Screen.width, titleHeight), "New Quest", newQuestTitleStyle);

            createMenuAreaOffset = new Rect((Screen.width - width) / 2f, titleY + titleHeight + titleGap, width, height);
            GUILayout.BeginArea(createMenuAreaOffset);

            var previousEnabled = GUI.enabled;
            var dropdownOpen = activeCreateDropdown != CreateDropdown.None;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(370f * scale));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", labelStyle, GUILayout.Width(74f * scale), GUILayout.Height(32f * scale));
            GUI.enabled = previousEnabled && !dropdownOpen;
            GUI.SetNextControlName("CreatePlayerName");
            createPlayerName = GUILayout.TextField(createPlayerName, 24, selectedIndex == CreateNameIndex ? uiTheme.SelectedTabStyle : GetTextFieldStyle(), GUILayout.Width(136f * scale), GUILayout.Height(32f * scale));
            if (focusCreateNameNextGui)
            {
                GUI.FocusControl("CreatePlayerName");
                focusCreateNameNextGui = false;
            }

            if (UiControls.Button("Generate Name", selectedIndex == CreateGenerateNameIndex, uiTheme, GUILayout.Width(152f * scale), GUILayout.Height(32f * scale)) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateGenerateNameIndex;
                GenerateRandomPlayerName();
            }

            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Gender:", labelStyle, GUILayout.Width(74f * scale), GUILayout.Height(32f * scale));
            DrawGenderDropdown(selectedIndex == CreateGenderIndex);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Class:", labelStyle, GUILayout.Width(74f * scale), GUILayout.Height(32f * scale));
            DrawClassDropdown(selectedIndex == CreateClassIndex);
            GUILayout.Space(34f * scale);
            GUI.enabled = previousEnabled && !dropdownOpen;
            if (UiControls.Button("Re-roll", selectedIndex == CreateRerollIndex, uiTheme, GUILayout.Width(92f * scale), GUILayout.Height(32f * scale)) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateRerollIndex;
                RerollCreatePreviewHero();
            }

            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8f * scale);
            DrawCreateStatsPanel(scale);
            GUILayout.EndHorizontal();

            GUILayout.Space(12f * scale);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = previousEnabled && !dropdownOpen;
            if (UiControls.Button("Start", selectedIndex == CreateStartIndex, uiTheme, GUILayout.Width(82f * scale), GUILayout.Height(32f * scale)) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateStartIndex;
                StartCreatedGame();
            }

            GUILayout.Space(10f * scale);
            if (UiControls.Button("Back", selectedIndex == CreateBackIndex, uiTheme, GUILayout.Width(82f * scale), GUILayout.Height(32f * scale)) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateBackIndex;
                ShowMainMenu();
            }

            GUI.enabled = previousEnabled;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawGenderDropdown(bool selected)
        {
            if (UiControls.Button(createPlayerGender.ToString(), selected || activeCreateDropdown == CreateDropdown.Gender, uiTheme, GUILayout.Width(136f * GetPixelScale()), GUILayout.Height(32f * GetPixelScale())) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateGenderIndex;
                activeCreateDropdown = activeCreateDropdown == CreateDropdown.Gender ? CreateDropdown.None : CreateDropdown.Gender;
                selectedDropdownIndex = (int)createPlayerGender;
                WaitForConfirmRelease();
            }

            genderDropdownAnchor = ToScreenRect(GUILayoutUtility.GetLastRect());
        }

        private void DrawClassDropdown(bool selected)
        {
            if (UiControls.Button(createPlayerClass.ToString(), selected || activeCreateDropdown == CreateDropdown.Class, uiTheme, GUILayout.Width(136f * GetPixelScale()), GUILayout.Height(32f * GetPixelScale())) &&
                !waitingForConfirmRelease)
            {
                selectedIndex = CreateClassIndex;
                activeCreateDropdown = activeCreateDropdown == CreateDropdown.Class ? CreateDropdown.None : CreateDropdown.Class;
                selectedDropdownIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(Class)), createPlayerClass);
                WaitForConfirmRelease();
            }

            classDropdownAnchor = ToScreenRect(GUILayoutUtility.GetLastRect());
        }

        private void DrawCreateDropdownOverlay()
        {
            if (mode != TitleMode.Create || activeCreateDropdown == CreateDropdown.None)
            {
                return;
            }

            if (activeCreateDropdown == CreateDropdown.Gender)
            {
                DrawDropdownOverlay(
                    genderDropdownAnchor,
                    new[] { Gender.Male, Gender.Female },
                    createPlayerGender,
                    value =>
                    {
                        createPlayerGender = value;
                        RerollCreatePreviewHero();
                        activeCreateDropdown = CreateDropdown.None;
                    },
                    ref genderDropdownScrollPosition,
                    selectedDropdownIndex);
                return;
            }

            var classes = System.Enum.GetValues(typeof(Class)).Cast<Class>().ToArray();
            DrawDropdownOverlay(
                classDropdownAnchor,
                classes,
                createPlayerClass,
                value =>
                {
                    createPlayerClass = value;
                    RerollCreatePreviewHero();
                    activeCreateDropdown = CreateDropdown.None;
                },
                ref classDropdownScrollPosition,
                selectedDropdownIndex);
        }

        private void DrawDropdownOverlay<T>(Rect anchor, IList<T> values, T selectedValue, System.Action<T> selected, ref Vector2 scrollPosition, int keyboardSelectedIndex)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            var scale = GetPixelScale();
            var rowHeight = 32f * scale;
            var visibleRows = Mathf.Min(values.Count, 6);
            var needsScrollbar = values.Count > visibleRows;
            var contentWidth = GetDropdownContentWidth(values, anchor.width);
            var width = contentWidth + uiTheme.BorderThickness * 2f + (needsScrollbar ? uiTheme.VerticalScrollbarStyle.fixedWidth : 0f);
            var height = visibleRows * rowHeight + uiTheme.BorderThickness * 2f;
            var rect = new Rect(anchor.x, anchor.yMax + 2f * scale, width, height);
            rect.x = Mathf.Clamp(rect.x, 8f * scale, Mathf.Max(8f * scale, Screen.width - rect.width - 8f * scale));
            rect.y = Mathf.Min(rect.y, Screen.height - rect.height - 8f * scale);

            GUI.Box(rect, GUIContent.none, panelStyle);
            var contentRect = new Rect(
                rect.x + uiTheme.BorderThickness,
                rect.y + uiTheme.BorderThickness,
                rect.width - uiTheme.BorderThickness * 2f,
                rect.height - uiTheme.BorderThickness * 2f);
            var previousVerticalThumb = GUI.skin.verticalScrollbarThumb;
            var previousVerticalScrollbar = GUI.skin.verticalScrollbar;
            var previousColor = GUI.color;
            GUI.skin.verticalScrollbar = uiTheme.VerticalScrollbarStyle;
            if (needsScrollbar)
            {
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
                var scrollbarWidth = uiTheme.VerticalScrollbarStyle.fixedWidth;
                var listRect = new Rect(contentRect.x, contentRect.y, contentRect.width - scrollbarWidth, contentRect.height);
                var scrollbarRect = new Rect(listRect.xMax, contentRect.y, scrollbarWidth, contentRect.height);
                var maxScroll = Mathf.Max(0f, values.Count * rowHeight - listRect.height);
                scrollPosition.y = GUI.VerticalScrollbar(
                    scrollbarRect,
                    Mathf.Clamp(scrollPosition.y, 0f, maxScroll),
                    listRect.height,
                    0f,
                    maxScroll + listRect.height);

                GUI.BeginGroup(listRect);
                DrawDropdownButtons(values, selectedValue, selected, rowHeight, listRect.width, -scrollPosition.y, listRect.height, keyboardSelectedIndex);
                GUI.EndGroup();
            }
            else
            {
                GUI.BeginGroup(contentRect);
                DrawDropdownButtons(values, selectedValue, selected, rowHeight, contentRect.width, 0f, contentRect.height, keyboardSelectedIndex);
                GUI.EndGroup();
            }

            GUI.skin.verticalScrollbarThumb = previousVerticalThumb;
            GUI.skin.verticalScrollbar = previousVerticalScrollbar;
            GUI.color = previousColor;
        }

        private void DrawDropdownButtons<T>(
            IList<T> values,
            T selectedValue,
            System.Action<T> selected,
            float rowHeight,
            float width,
            float yOffset,
            float visibleHeight,
            int keyboardSelectedIndex)
        {
            for (var i = 0; i < values.Count; i++)
            {
                var y = i * rowHeight + yOffset;
                if (y + rowHeight < 0f || y > visibleHeight)
                {
                    continue;
                }

                var value = values[i];
                var selectedRow = EqualityComparer<T>.Default.Equals(value, selectedValue);
                var style = selectedRow || i == keyboardSelectedIndex ? uiTheme.SelectedTabStyle : uiTheme.ButtonStyle;
                if (UiControls.Button(new Rect(0f, y, width, rowHeight), value.ToString(), style) &&
                    !waitingForConfirmRelease)
                {
                    selected(value);
                    Event.current.Use();
                }
            }
        }

        private float GetDropdownContentWidth<T>(IEnumerable<T> values, float minimumWidth)
        {
            var width = minimumWidth;
            var extraPadding = 24f * GetPixelScale();
            foreach (var value in values)
            {
                var text = value == null ? string.Empty : value.ToString();
                var size = uiTheme.ButtonStyle.CalcSize(new GUIContent(text));
                width = Mathf.Max(width, size.x + uiTheme.ButtonStyle.padding.horizontal + uiTheme.BorderThickness * 2f + extraPadding);
            }

            return width;
        }

        private Rect ToScreenRect(Rect localRect)
        {
            return new Rect(
                createMenuAreaOffset.x + localRect.x,
                createMenuAreaOffset.y + localRect.y,
                localRect.width,
                localRect.height);
        }

        private void DrawCreateStatsPanel(float scale)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(268f * scale), GUILayout.Height(132f * scale));
            GUILayout.BeginHorizontal();
            Sprite sprite;
            if (UiAssetResolver.TryGetHeroSprite(createPlayerClass, createPlayerGender, out sprite))
            {
                var rect = GUILayoutUtility.GetRect(42f * scale, 88f * scale, GUILayout.Width(42f * scale));
                DrawSprite(rect, sprite);
            }
            else
            {
                GUILayout.Space(42f * scale);
            }

            GUILayout.BeginVertical();
            DrawStatRow("Health:", createPreviewHero == null ? 0 : createPreviewHero.MaxHealth);
            DrawStatRow("Magic:", createPreviewHero == null ? 0 : createPreviewHero.MaxMagic);
            DrawStatRow("Attack:", createPreviewHero == null ? 0 : createPreviewHero.Attack);
            DrawStatRow("Defence:", createPreviewHero == null ? 0 : createPreviewHero.Defence);
            DrawStatRow("Magic Defence:", createPreviewHero == null ? 0 : createPreviewHero.MagicDefence);
            DrawStatRow("Agility:", createPreviewHero == null ? 0 : createPreviewHero.Agility);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawStatRow(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150f * GetPixelScale()));
            GUILayout.Label(value.ToString(), labelStyle, GUILayout.Width(44f * GetPixelScale()));
            GUILayout.EndHorizontal();
        }

        private GUIStyle GetTextFieldStyle()
        {
            return uiTheme.TextFieldStyle;
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var aspect = textureRect.width / textureRect.height;
            var drawWidth = rect.width;
            var drawHeight = drawWidth / aspect;
            if (drawHeight > rect.height)
            {
                drawHeight = rect.height;
                drawWidth = drawHeight * aspect;
            }

            var drawRect = new Rect(
                rect.x + (rect.width - drawWidth) / 2f,
                rect.y + (rect.height - drawHeight) / 2f,
                drawWidth,
                drawHeight);
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords, true);
        }

        private void GenerateRandomPlayerName()
        {
            var names = GameDataCache.Current == null ? null : GameDataCache.Current.Names;
            if (names == null)
            {
                return;
            }

            var generator = new NameGenerator(names);
            var name = generator.Generate(createPlayerGender);
            if (!string.IsNullOrEmpty(name))
            {
                createPlayerName = name;
            }
        }

        private void EnsureCreatePlayerName()
        {
            if (createPlayerNameInitialized)
            {
                return;
            }

            createPlayerNameInitialized = true;
            GenerateRandomPlayerName();
            RerollCreatePreviewHero();
        }

        private void EnsureCreatePreviewHero()
        {
            if (createPreviewHero == null)
            {
                RerollCreatePreviewHero();
            }
        }

        private void RerollCreatePreviewHero()
        {
            createPreviewHero = gameState == null
                ? null
                : gameState.CreatePlayerPreviewHero(createPlayerName, createPlayerClass, createPlayerGender);
        }

        private IEnumerable<TitleRow> GetMainRows()
        {
            if (gameState != null && gameState.HasQuickSave())
            {
                yield return new TitleRow
                {
                    Label = "Continue",
                    Enabled = true,
                    Action = ContinueGame
                };
            }

            yield return new TitleRow
            {
                Label = "New Quest",
                Enabled = true,
                Action = ShowCreateMenu
            };
            if (gameState != null && gameState.GetManualSaveSlots().Count > 0)
            {
                yield return new TitleRow
                {
                    Label = "Load Quest",
                    Enabled = true,
                    Action = ShowLoadMenu
                };
            }

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
                return (gameState == null ? 0 : gameState.GetManualSaveSlots().Count) * 2 + 1;
            }

            if (mode == TitleMode.Create)
            {
                return 7;
            }

            return GetMainRows().Count();
        }

        private void HandleNavigation()
        {
            if (mode == TitleMode.Load)
            {
                HandleLoadNavigation();
                return;
            }

            if (mode == TitleMode.Create)
            {
                HandleCreateNavigation();
                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                SetSelectedIndex(Mathf.Clamp(selectedIndex + moveY, 0, Mathf.Max(GetOptionCount() - 1, 0)));
            }
        }

        private void HandleCreateNavigation()
        {
            var moveY = GetMenuMoveY();
            if (moveY < 0)
            {
                SetSelectedIndex(GetCreateUpIndex(selectedIndex));
            }
            else if (moveY > 0)
            {
                SetSelectedIndex(GetCreateDownIndex(selectedIndex));
            }

            var moveX = InputManager.GetMoveXDown();
            if (moveX < 0)
            {
                SetSelectedIndex(GetCreateLeftIndex(selectedIndex));
            }
            else if (moveX > 0)
            {
                SetSelectedIndex(GetCreateRightIndex(selectedIndex));
            }
        }

        private static int GetCreateUpIndex(int index)
        {
            switch (index)
            {
                case CreateGenderIndex:
                    return CreateNameIndex;
                case CreateClassIndex:
                    return CreateGenderIndex;
                case CreateStartIndex:
                    return CreateClassIndex;
                case CreateRerollIndex:
                    return CreateGenerateNameIndex;
                case CreateBackIndex:
                    return CreateRerollIndex;
                default:
                    return index;
            }
        }

        private static int GetCreateDownIndex(int index)
        {
            switch (index)
            {
                case CreateNameIndex:
                    return CreateGenderIndex;
                case CreateGenderIndex:
                    return CreateClassIndex;
                case CreateClassIndex:
                    return CreateStartIndex;
                case CreateGenerateNameIndex:
                    return CreateRerollIndex;
                case CreateRerollIndex:
                    return CreateBackIndex;
                default:
                    return index;
            }
        }

        private static int GetCreateLeftIndex(int index)
        {
            switch (index)
            {
                case CreateGenerateNameIndex:
                    return CreateNameIndex;
                case CreateRerollIndex:
                    return CreateClassIndex;
                case CreateBackIndex:
                    return CreateStartIndex;
                default:
                    return index;
            }
        }

        private static int GetCreateRightIndex(int index)
        {
            switch (index)
            {
                case CreateNameIndex:
                    return CreateGenerateNameIndex;
                case CreateClassIndex:
                    return CreateRerollIndex;
                case CreateStartIndex:
                    return CreateBackIndex;
                default:
                    return index;
            }
        }

        private void HandleLoadNavigation()
        {
            var saveCount = gameState == null ? 0 : gameState.GetManualSaveSlots().Count;
            var backIndex = GetLoadBackIndex(saveCount);
            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                if (saveCount == 0)
                {
                    SetSelectedIndex(backIndex);
                    return;
                }

                if (selectedIndex >= backIndex)
                {
                    SetSelectedIndex(moveY < 0 ? GetLoadSaveIndex(saveCount - 1) : backIndex);
                    return;
                }

                var row = selectedIndex / 2;
                if (moveY < 0)
                {
                    SetSelectedIndex(row <= 0 ? GetLoadSaveIndex(0) : GetLoadSaveIndex(row - 1));
                }
                else
                {
                    SetSelectedIndex(row >= saveCount - 1 ? backIndex : GetLoadSaveIndex(row + 1));
                }
            }

            var moveX = InputManager.GetMoveXDown();
            if (moveX == 0 || selectedIndex >= backIndex)
            {
                return;
            }

            var saveIndex = selectedIndex / 2;
            if (moveX > 0)
            {
                SetSelectedIndex(GetLoadDeleteIndex(saveIndex));
            }
            else
            {
                SetSelectedIndex(GetLoadSaveIndex(saveIndex));
            }
        }

        private void SetSelectedIndex(int index)
        {
            if (selectedIndex == index)
            {
                return;
            }

            selectedIndex = index;
            UiControls.PlaySelectSound();
        }

        private void ActivateSelected()
        {
            if (mode == TitleMode.Load)
            {
                var slots = gameState == null ? new List<GameSave>() : gameState.GetManualSaveSlots().ToList();
                var backIndex = GetLoadBackIndex(slots.Count);
                if (selectedIndex >= backIndex)
                {
                    mode = TitleMode.Main;
                    selectedIndex = 0;
                    WaitForConfirmRelease();
                    ResetNavigationRepeat();
                    return;
                }

                var slotIndex = selectedIndex / 2;
                if (selectedIndex % 2 == 0)
                {
                    ActivateTitleAction(() => TryLoadSlot(slotIndex));
                }
                else
                {
                    DeleteSlot(slotIndex);
                }

                return;
            }

            if (mode == TitleMode.Create)
            {
                ActivateSelectedCreateControl();
                return;
            }

            var rows = GetMainRows().ToList();
            if (selectedIndex >= 0 && selectedIndex < rows.Count && rows[selectedIndex].Enabled)
            {
                ActivateTitleAction(rows[selectedIndex].Action);
            }
        }

        private void ActivateSelectedCreateControl()
        {
            switch (selectedIndex)
            {
                case CreateNameIndex:
                    focusCreateNameNextGui = true;
                    break;
                case CreateGenerateNameIndex:
                    GenerateRandomPlayerName();
                    RerollCreatePreviewHero();
                    break;
                case CreateGenderIndex:
                    activeCreateDropdown = CreateDropdown.Gender;
                    selectedDropdownIndex = (int)createPlayerGender;
                    WaitForConfirmRelease();
                    break;
                case CreateClassIndex:
                    activeCreateDropdown = CreateDropdown.Class;
                    selectedDropdownIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(Class)), createPlayerClass);
                    WaitForConfirmRelease();
                    break;
                case CreateRerollIndex:
                    RerollCreatePreviewHero();
                    break;
                case CreateStartIndex:
                    ActivateTitleAction(StartCreatedGame);
                    break;
                case CreateBackIndex:
                default:
                    ShowMainMenu();
                    break;
            }
        }

        private void ActivateTitleAction(Action action)
        {
            if (titleActionPending || action == null)
            {
                return;
            }

            UiControls.PlayConfirmSound();
            StartCoroutine(RunTitleActionAfterConfirmFrame(action));
        }

        private IEnumerator RunTitleActionAfterConfirmFrame(Action action)
        {
            titleActionPending = true;
            yield return null;
            titleActionPending = false;
            action();
        }

        private void HandleCreateDropdownInput()
        {
            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                activeCreateDropdown = CreateDropdown.None;
                ResetNavigationRepeat();
                return;
            }

            var count = activeCreateDropdown == CreateDropdown.Gender
                ? System.Enum.GetValues(typeof(Gender)).Length
                : System.Enum.GetValues(typeof(Class)).Length;
            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                selectedDropdownIndex = Mathf.Clamp(selectedDropdownIndex + moveY, 0, Mathf.Max(count - 1, 0));
            }

            if (!GetConfirmDown())
            {
                return;
            }

            if (activeCreateDropdown == CreateDropdown.Gender)
            {
                var values = System.Enum.GetValues(typeof(Gender));
                createPlayerGender = (Gender)values.GetValue(Mathf.Clamp(selectedDropdownIndex, 0, values.Length - 1));
            }
            else
            {
                var values = System.Enum.GetValues(typeof(Class));
                createPlayerClass = (Class)values.GetValue(Mathf.Clamp(selectedDropdownIndex, 0, values.Length - 1));
            }

            RerollCreatePreviewHero();
            activeCreateDropdown = CreateDropdown.None;
            WaitForConfirmRelease();
            ResetNavigationRepeat();
        }

        private static bool GetConfirmDown()
        {
            return InputManager.GetCommandDown(InputCommand.Interact) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        private static bool GetConfirmHeld()
        {
            return InputManager.GetCommand(InputCommand.Interact) ||
                   Input.GetKey(KeyCode.Return) ||
                   Input.GetKey(KeyCode.KeypadEnter);
        }

        private void WaitForConfirmRelease()
        {
            waitingForConfirmRelease = GetConfirmHeld() || Input.GetMouseButton(0);
        }

        private void ContinueGame()
        {
            if (gameState != null && gameState.LoadQuick())
            {
                Close();
            }
        }

        private void ShowLoadMenu()
        {
            mode = TitleMode.Load;
            selectedIndex = 0;
            WaitForConfirmRelease();
            ResetNavigationRepeat();
        }

        private void ShowCreateMenu()
        {
            mode = TitleMode.Create;
            selectedIndex = 0;
            EnsureCreatePlayerName();
            WaitForConfirmRelease();
            ResetNavigationRepeat();
        }

        private void ShowMainMenu()
        {
            mode = TitleMode.Main;
            selectedIndex = 0;
            WaitForConfirmRelease();
            ResetNavigationRepeat();
        }

        private void StartCreatedGame()
        {
            if (gameState != null)
            {
                gameState.RestartNewGame(createPlayerName, createPlayerClass, createPlayerGender);
            }

            Close();
        }

        private void TryLoadSlot(int slotIndex)
        {
            if (gameState != null && gameState.LoadManual(slotIndex))
            {
                Close();
            }
        }

        private void DeleteSlot(int slotIndex)
        {
            if (gameState != null)
            {
                gameState.DeleteManual(slotIndex);
                selectedIndex = Mathf.Clamp(selectedIndex, 0, GetOptionCount() - 1);
            }
        }

        private static int GetLoadSaveIndex(int saveIndex)
        {
            return saveIndex * 2;
        }

        private static int GetLoadDeleteIndex(int saveIndex)
        {
            return saveIndex * 2 + 1;
        }

        private static int GetLoadBackIndex(int saveCount)
        {
            return saveCount * 2;
        }

        private static void Quit()
        {
            Application.Quit();
        }

        private static void Close()
        {
            isOpen = false;
            var mapView = FindAnyObjectByType<View>();
            if (mapView != null)
            {
                mapView.PlayCurrentMapMusic();
            }
        }

        private int GetMenuMoveY()
        {
            var pressed = InputManager.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMoveY = pressed;
                nextMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = InputManager.GetMoveY();
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
                gameState = GameState.GetOrCreate();
            }

            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (titleStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, System.StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            titleStyle = uiTheme.TitleStyle;
            mainMenuButtonStyle = new GUIStyle(uiTheme.ButtonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Normal
            };
            selectedMainMenuButtonStyle = new GUIStyle(uiTheme.SelectedTabStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Normal
            };
            labelStyle = uiTheme.LabelStyle;
            smallStyle = uiTheme.SmallStyle;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
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
