using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Tools;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeTitleMenu : MonoBehaviour
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

        private static bool isOpen;

        private DungeonEscapeGameState gameState;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle mainTitleStyle;
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

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        public static void OpenMainMenu()
        {
            var menu = FindAnyObjectByType<DungeonEscapeTitleMenu>();
            if (menu == null)
            {
                menu = new GameObject("DungeonEscapeTitleMenu").AddComponent<DungeonEscapeTitleMenu>();
            }

            menu.mode = TitleMode.Main;
            menu.selectedIndex = 0;
            menu.ResetNavigationRepeat();
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
            if (DungeonEscapeSettingsCache.Current.SkipSplashAndLoadQuickSave)
            {
                return;
            }

            if (isOpen)
            {
                return;
            }

            isOpen = true;
            mode = TitleMode.Main;
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (DungeonEscapeSplashScreen.IsVisible)
            {
                return;
            }

            EnsureReferences();
            if (activeCreateDropdown != CreateDropdown.None)
            {
                HandleCreateDropdownInput();
                return;
            }

            HandleNavigation();

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                if (mode == TitleMode.Load)
                {
                    mode = TitleMode.Main;
                    selectedIndex = 0;
                    ResetNavigationRepeat();
                }
            }
            else if (GetConfirmDown())
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

            if (DungeonEscapeSplashScreen.IsVisible)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = TitleGuiDepth;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            var scale = GetPixelScale();
            if (mode == TitleMode.Main)
            {
                DrawMainMenuStandalone(scale);
                GUI.depth = previousDepth;
                return;
            }

            if (mode == TitleMode.Create)
            {
                DrawCreateMenuStandalone(scale);
                DrawCreateDropdownOverlay();
                GUI.depth = previousDepth;
                return;
            }

            if (mode == TitleMode.Load)
            {
                DrawLoadMenuStandalone(scale);
                GUI.depth = previousDepth;
                return;
            }

            GUI.depth = previousDepth;
        }

        private void DrawMainMenuStandalone(float scale)
        {
            var rows = GetMainRows().ToList();
            var titleRect = new Rect(0f, 36f * scale, Screen.width, 88f * scale);
            GUI.Label(titleRect, "Dungeon Escape", mainTitleStyle);

            var buttonWidth = Mathf.Min(250f * scale, Screen.width - 48f * scale);
            var buttonHeight = 32f * scale;
            var buttonGap = 18f * scale;
            var startY = titleRect.yMax + 4f * scale;
            var x = (Screen.width - buttonWidth) / 2f;
            for (var i = 0; i < rows.Count; i++)
            {
                var rect = new Rect(x, startY + i * (buttonHeight + buttonGap), buttonWidth, buttonHeight);
                var enabled = rows[i].Enabled;
                var previousEnabled = GUI.enabled;
                GUI.enabled = enabled;
                if (GUI.Button(rect, rows[i].Label, selectedIndex == i ? selectedMainMenuButtonStyle : mainMenuButtonStyle))
                {
                    selectedIndex = i;
                    if (enabled)
                    {
                        rows[i].Action();
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
            GUI.Label(new Rect(0f, 28f * scale, Screen.width, 44f * scale), "Load Quest", loadTitleStyle);

            var slots = gameState == null
                ? new List<GameSave>()
                : gameState.GetManualSaveSlots().ToList();

            selectedIndex = Mathf.Clamp(selectedIndex, 0, GetLoadBackIndex(slots.Count));
            var width = Mathf.Min(640f * scale, Screen.width - 32f * scale);
            var height = 330f * scale;
            var area = new Rect((Screen.width - width) / 2f, 86f * scale, width, height);
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

            var backWidth = 82f * scale;
            var backHeight = 32f * scale;
            var backRect = new Rect((Screen.width - backWidth) / 2f, area.yMax + 10f * scale, backWidth, backHeight);
            if (GUI.Button(backRect, "Back", selectedIndex == GetLoadBackIndex(slots.Count) ? selectedMainMenuButtonStyle : mainMenuButtonStyle))
            {
                ShowMainMenu();
            }
        }

        private void DrawLoadSlotButtons(IList<GameSave> slots, float scale)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (DungeonEscapeUiControls.Button(
                    DungeonEscapeGameState.GetGameSaveTitle(slots[i]) + "\n" +
                    DungeonEscapeGameState.GetGameSaveSummary(slots[i]),
                    selectedIndex == GetLoadSaveIndex(i),
                    uiTheme,
                    GUILayout.Height(48f * scale),
                    GUILayout.Width(500f * scale)))
                {
                    selectedIndex = GetLoadSaveIndex(i);
                    TryLoadSlot(i);
                }

                if (DungeonEscapeUiControls.Button("Delete", selectedIndex == GetLoadDeleteIndex(i), uiTheme, GUILayout.Height(48f * scale), GUILayout.Width(92f * scale)))
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
            GUI.Label(new Rect(0f, 28f * scale, Screen.width, 44f * scale), "New Quest", newQuestTitleStyle);
            EnsureCreatePreviewHero();

            var width = Mathf.Min(660f * scale, Screen.width - 32f * scale);
            var height = 230f * scale;
            createMenuAreaOffset = new Rect((Screen.width - width) / 2f, 80f * scale, width, height);
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

            if (DungeonEscapeUiControls.Button("Generate Name", selectedIndex == CreateGenerateNameIndex, uiTheme, GUILayout.Width(152f * scale), GUILayout.Height(32f * scale)))
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
            if (DungeonEscapeUiControls.Button("Re-roll", selectedIndex == CreateRerollIndex, uiTheme, GUILayout.Width(92f * scale), GUILayout.Height(32f * scale)))
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
            if (DungeonEscapeUiControls.Button("Start", selectedIndex == CreateStartIndex, uiTheme, GUILayout.Width(82f * scale), GUILayout.Height(32f * scale)))
            {
                selectedIndex = CreateStartIndex;
                StartCreatedGame();
            }

            GUILayout.Space(10f * scale);
            if (DungeonEscapeUiControls.Button("Back", selectedIndex == CreateBackIndex, uiTheme, GUILayout.Width(82f * scale), GUILayout.Height(32f * scale)))
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
            if (DungeonEscapeUiControls.Button(createPlayerGender.ToString(), selected || activeCreateDropdown == CreateDropdown.Gender, uiTheme, GUILayout.Width(136f * GetPixelScale()), GUILayout.Height(32f * GetPixelScale())))
            {
                selectedIndex = CreateGenderIndex;
                activeCreateDropdown = activeCreateDropdown == CreateDropdown.Gender ? CreateDropdown.None : CreateDropdown.Gender;
                selectedDropdownIndex = (int)createPlayerGender;
            }

            genderDropdownAnchor = ToScreenRect(GUILayoutUtility.GetLastRect());
        }

        private void DrawClassDropdown(bool selected)
        {
            if (DungeonEscapeUiControls.Button(createPlayerClass.ToString(), selected || activeCreateDropdown == CreateDropdown.Class, uiTheme, GUILayout.Width(136f * GetPixelScale()), GUILayout.Height(32f * GetPixelScale())))
            {
                selectedIndex = CreateClassIndex;
                activeCreateDropdown = activeCreateDropdown == CreateDropdown.Class ? CreateDropdown.None : CreateDropdown.Class;
                selectedDropdownIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(Class)), createPlayerClass);
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
                if (GUI.Button(new Rect(0f, y, width, rowHeight), value.ToString(), style))
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
            if (DungeonEscapeUiAssetResolver.TryGetHeroSprite(createPlayerClass, createPlayerGender, out sprite))
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
            var names = DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.Names;
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
                selectedIndex = Mathf.Clamp(selectedIndex + moveY, 0, Mathf.Max(GetOptionCount() - 1, 0));
            }
        }

        private void HandleCreateNavigation()
        {
            var moveY = GetMenuMoveY();
            if (moveY < 0)
            {
                selectedIndex = GetCreateUpIndex(selectedIndex);
            }
            else if (moveY > 0)
            {
                selectedIndex = GetCreateDownIndex(selectedIndex);
            }

            var moveX = DungeonEscapeInput.GetMoveXDown();
            if (moveX < 0)
            {
                selectedIndex = GetCreateLeftIndex(selectedIndex);
            }
            else if (moveX > 0)
            {
                selectedIndex = GetCreateRightIndex(selectedIndex);
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
                    selectedIndex = backIndex;
                    return;
                }

                if (selectedIndex >= backIndex)
                {
                    selectedIndex = moveY < 0 ? GetLoadSaveIndex(saveCount - 1) : backIndex;
                    return;
                }

                var row = selectedIndex / 2;
                if (moveY < 0)
                {
                    selectedIndex = row <= 0 ? GetLoadSaveIndex(0) : GetLoadSaveIndex(row - 1);
                }
                else
                {
                    selectedIndex = row >= saveCount - 1 ? backIndex : GetLoadSaveIndex(row + 1);
                }
            }

            var moveX = DungeonEscapeInput.GetMoveXDown();
            if (moveX == 0 || selectedIndex >= backIndex)
            {
                return;
            }

            var saveIndex = selectedIndex / 2;
            if (moveX > 0)
            {
                selectedIndex = GetLoadDeleteIndex(saveIndex);
            }
            else
            {
                selectedIndex = GetLoadSaveIndex(saveIndex);
            }
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
                    ResetNavigationRepeat();
                    return;
                }

                var slotIndex = selectedIndex / 2;
                if (selectedIndex % 2 == 0)
                {
                    TryLoadSlot(slotIndex);
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
                rows[selectedIndex].Action();
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
                    break;
                case CreateClassIndex:
                    activeCreateDropdown = CreateDropdown.Class;
                    selectedDropdownIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(Class)), createPlayerClass);
                    break;
                case CreateRerollIndex:
                    RerollCreatePreviewHero();
                    break;
                case CreateStartIndex:
                    StartCreatedGame();
                    break;
                case CreateBackIndex:
                default:
                    ShowMainMenu();
                    break;
            }
        }

        private void HandleCreateDropdownInput()
        {
            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
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
            ResetNavigationRepeat();
        }

        private static bool GetConfirmDown()
        {
            return DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter);
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
            ResetNavigationRepeat();
        }

        private void ShowCreateMenu()
        {
            mode = TitleMode.Create;
            selectedIndex = 0;
            EnsureCreatePlayerName();
            ResetNavigationRepeat();
        }

        private void ShowMainMenu()
        {
            mode = TitleMode.Main;
            selectedIndex = 0;
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
            mainTitleStyle = new GUIStyle(uiTheme.TitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(64f * scale),
                fontStyle = FontStyle.Normal,
                wordWrap = false
            };
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
