using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class StoreWindow : MonoBehaviour
    {
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private enum StoreTab
        {
            Buy,
            Sell
        }

        private enum StoreFocus
        {
            SellMembers,
            Items
        }

        private static StoreWindow instance;

        private GameState gameState;
        private TiledObjectInfo storeObject;
        private UiSettings uiSettings;
        private UiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle panelStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private StoreTab currentTab = StoreTab.Buy;
        private StoreFocus currentFocus = StoreFocus.Items;
        private int selectedHeroIndex;
        private int selectedBuyIndex;
        private int selectedSellIndex;
        private int selectedModalIndex;
        private Vector2 buyScroll;
        private Vector2 sellScroll;
        private string modalTitle;
        private string modalMessage;
        private List<string> modalChoices;
        private List<Hero> modalChoiceHeroes;
        private Action<int> modalSelected;
        private int acceptInteractAfterFrame;
        private bool waitForInteractRelease;
        private int repeatingMoveX;
        private int repeatingMoveY;
        private float nextMoveXTime;
        private float nextMoveYTime;
        private float buyListHeight;
        private float sellListHeight;
        private GUIStyle previousScrollbarThumb;
        private bool themedScrollActive;

        public static bool IsOpen
        {
            get { return instance != null && instance.storeObject != null; }
        }

        public static void Show(GameState state, TiledObjectInfo mapObject)
        {
            GetOrCreate().Open(state, mapObject);
        }

        private static StoreWindow GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<StoreWindow>();
            if (instance != null)
            {
                return instance;
            }

            instance = new GameObject("StoreWindow").AddComponent<StoreWindow>();
            return instance;
        }

        private void Open(GameState state, TiledObjectInfo mapObject)
        {
            gameState = state == null ? GameState.GetOrCreate() : state;
            storeObject = mapObject;
            currentTab = StoreTab.Buy;
            currentFocus = StoreFocus.Items;
            selectedHeroIndex = 0;
            selectedBuyIndex = 0;
            selectedSellIndex = 0;
            buyScroll = Vector2.zero;
            sellScroll = Vector2.zero;
            HideModal();
            ResetNavigationRepeat();
            BlockInteractUntilRelease();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (IsModalVisible())
            {
                HandleModalInput();
                return;
            }

            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
                Close();
                return;
            }

            HandleStoreInput();
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();
            var previousDepth = GUI.depth;
            GUI.depth = -900;

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 900f * scale);
            var height = Mathf.Min(Screen.height - 32f * scale, 620f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, rect.height - 28f * scale));
            DrawHeader();
            DrawTabs();
            GUILayout.Space(8f * scale);

            if (currentTab == StoreTab.Buy)
            {
                DrawBuyTab(height - 148f * scale);
            }
            else
            {
                DrawSellTab(height - 148f * scale);
            }

            GUILayout.EndArea();

            if (IsModalVisible())
            {
                DrawModal();
            }

            GUI.depth = previousDepth;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetStoreName(), titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gold: " + (gameState == null || gameState.Party == null ? 0 : gameState.Party.Gold), labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label(GetStoreText(), smallStyle);
        }

        private void DrawTabs()
        {
            var scale = GetPixelScale();
            GUILayout.BeginHorizontal();
            if (UiControls.TabButton("Buy", currentTab == StoreTab.Buy, uiTheme, 34f * scale))
            {
                currentFocus = StoreFocus.Items;
                SetCurrentTab(StoreTab.Buy);
            }
            DrawTabFocusIndicator(GUILayoutUtility.GetLastRect(), currentTab == StoreTab.Buy);

            GUI.enabled = StoreWillBuyItems();
            if (UiControls.TabButton("Sell", currentTab == StoreTab.Sell, uiTheme, 34f * scale))
            {
                currentFocus = StoreFocus.Items;
                SetCurrentTab(StoreTab.Sell);
            }
            DrawTabFocusIndicator(GUILayoutUtility.GetLastRect(), currentTab == StoreTab.Sell);

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawBuyTab(float height)
        {
            buyListHeight = height;
            var inventory = gameState == null ? new List<Item>() : gameState.GetStoreInventory(storeObject);
            selectedBuyIndex = Mathf.Clamp(selectedBuyIndex, 0, Mathf.Max(inventory.Count - 1, 0));
            if (inventory.Count == 0)
            {
                GUILayout.Label("I have nothing to sell right now.", labelStyle);
                return;
            }

            buyScroll = BeginThemedScroll(buyScroll, height);
            for (var i = 0; i < inventory.Count; i++)
            {
                DrawBuyItemRow(inventory[i], i);
            }

            EndThemedScroll();
        }

        private void DrawBuyItemRow(Item item, int index)
        {
            GUILayout.BeginHorizontal(GetRowStyle(currentFocus == StoreFocus.Items && index == selectedBuyIndex));
            var rowHeight = GetStoreRowHeight();
            Sprite sprite;
            UiControls.SpriteIcon(
                UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                34f * GetPixelScale(),
                uiTheme);
            GUILayout.Label(item.Name, GetCenteredRarityStyle(item), GUILayout.Height(rowHeight));
            GUILayout.FlexibleSpace();
            GUILayout.Label(item.Cost + "g", GetRightAlignedStyle(labelStyle), GUILayout.Width(92f * GetPixelScale()), GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
            HandleBuyRowMouse(GUILayoutUtility.GetLastRect(), item, index);
        }

        private void DrawSellTab(float height)
        {
            if (!StoreWillBuyItems())
            {
                GUILayout.Label("This store is not buying items.", labelStyle);
                return;
            }

            var members = GetPartyMembers();
            if (members.Count == 0)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                if (UiControls.TabButton(members[i].Name, selectedHeroIndex == i, uiTheme, 34f * GetPixelScale()))
                {
                    currentFocus = StoreFocus.SellMembers;
                    SetSelectedHeroIndex(i);
                }

                DrawTabFocusIndicator(GUILayoutUtility.GetLastRect(), selectedHeroIndex == i);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            var items = GetSellableItems(hero).ToList();
            selectedSellIndex = Mathf.Clamp(selectedSellIndex, 0, Mathf.Max(items.Count - 1, 0));
            if (items.Count == 0)
            {
                GUILayout.Label(hero.Name + " has no sellable items.", labelStyle);
                return;
            }

            sellListHeight = height - 44f * GetPixelScale();
            sellScroll = BeginThemedScroll(sellScroll, sellListHeight);
            for (var i = 0; i < items.Count; i++)
            {
                DrawSellItemRow(hero, items[i], i);
            }

            EndThemedScroll();
        }

        private void DrawSellItemRow(Hero hero, ItemInstance item, int index)
        {
            GUILayout.BeginHorizontal(GetRowStyle(currentFocus == StoreFocus.Items && index == selectedSellIndex));
            var rowHeight = GetStoreRowHeight();
            Sprite sprite;
            UiControls.SpriteIcon(
                UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                34f * GetPixelScale(),
                uiTheme);
            GUILayout.Label(item.Name + (item.IsEquipped ? "  Equipped" : ""), GetCenteredRarityStyle(item.Item), GUILayout.Height(rowHeight));
            GUILayout.FlexibleSpace();
            GUILayout.Label(GetSalePrice(item) + "g", GetRightAlignedStyle(labelStyle), GUILayout.Width(92f * GetPixelScale()), GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
            HandleSellRowMouse(GUILayoutUtility.GetLastRect(), hero, item, index);
        }

        private void ShowRecipientPicker(Item item)
        {
            var recipients = GetBuyRecipients();
            var labels = recipients.Select(hero => hero.Name + " (" + hero.Items.Count + "/" + Party.MaxItems + ")").ToList();
            labels.Add("Cancel");
            var choiceHeroes = recipients.Cast<Hero>().Concat(new Hero[] { null }).ToList();
            ShowModal("Buy " + item.Name, "Who should carry this item?", labels, choiceHeroes, index =>
            {
                if (index < 0 || index >= recipients.Count)
                {
                    return;
                }

                ItemInstance purchasedItem;
                var recipient = recipients[index];
                var message = gameState.BuyStoreItem(storeObject, item, recipient, out purchasedItem);
                if (purchasedItem != null && recipient.CanEquipItem(purchasedItem))
                {
                    ShowModal(
                        "Equip " + purchasedItem.Name,
                        message + "\nEquip it now?",
                        new[] { "Equip", "Keep" },
                        selected =>
                        {
                            if (selected == 0)
                            {
                                gameState.EquipHeroItem(recipient, purchasedItem);
                                ShowModal("Store", recipient.Name + " equipped " + purchasedItem.Name + ".", null, null);
                            }
                            else
                            {
                                ShowModal("Store", message, null, null);
                            }
                        });
                    return;
                }

                ShowModal("Store", message, null, null);
            });
        }

        private List<Hero> GetBuyRecipients()
        {
            return gameState == null || gameState.Party == null
                ? new List<Hero>()
                : gameState.Party.AliveMembers.Where(hero => hero.Items.Count < Party.MaxItems).ToList();
        }

        private List<Hero> GetPartyMembers()
        {
            return gameState == null || gameState.Party == null
                ? new List<Hero>()
                : gameState.Party.Members.ToList();
        }

        private static IEnumerable<ItemInstance> GetSellableItems(Hero hero)
        {
            return hero == null || hero.Items == null
                ? new List<ItemInstance>()
                : hero.Items.Where(item => item != null && item.Item != null && item.Item.CanBeSoldInStore && item.Type != ItemType.Quest);
        }

        private static int GetSalePrice(ItemInstance item)
        {
            return item == null ? 0 : Math.Max(1, item.Gold * 3 / 4);
        }

        private void ShowModal(string title, string message, IEnumerable<string> choices, Action<int> selected)
        {
            ShowModal(title, message, choices, null, selected);
        }

        private void ShowModal(string title, string message, IEnumerable<string> choices, IEnumerable<Hero> choiceHeroes, Action<int> selected)
        {
            modalTitle = title;
            modalMessage = message;
            modalChoices = choices == null ? null : choices.ToList();
            modalChoiceHeroes = choiceHeroes == null ? null : choiceHeroes.ToList();
            modalSelected = selected;
            selectedModalIndex = 0;
            BlockInteractUntilRelease();
        }

        private bool IsModalVisible()
        {
            return !string.IsNullOrEmpty(modalMessage);
        }

        private void HideModal()
        {
            modalTitle = null;
            modalMessage = null;
            modalChoices = null;
            modalChoiceHeroes = null;
            modalSelected = null;
        }

        private void DrawModal()
        {
            var scale = GetPixelScale();
            DrawModalBackdrop();
            var choiceCount = modalChoices == null || modalChoices.Count == 0 ? 1 : modalChoices.Count;
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var height = Mathf.Min(Screen.height - 48f * scale, (150f + choiceCount * 42f) * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            GUILayout.Label(modalTitle, titleStyle);
            GUILayout.Label(modalMessage, labelStyle);
            GUILayout.FlexibleSpace();
            if (modalChoices != null && modalChoices.Count > 0)
            {
                foreach (var choice in modalChoices.Select((label, index) => new { label, index }))
                {
                    if (modalChoiceHeroes != null)
                    {
                        DrawModalChoiceRow(choice.label, choice.index);
                    }
                    else if (UiControls.Button(choice.label, choice.index == selectedModalIndex, uiTheme))
                    {
                        selectedModalIndex = choice.index;
                        var selected = modalSelected;
                        HideModal();
                        if (selected != null)
                        {
                            selected(choice.index);
                        }

                        break;
                    }
                }
            }
            else if (UiControls.Button("OK", true, uiTheme, GUILayout.Width(120f * scale)))
            {
                HideModal();
            }

            GUILayout.EndArea();
        }

        private void DrawModalChoiceRow(string label, int index)
        {
            var scale = GetPixelScale();
            var rowHeight = 38f * scale;
            GUILayout.BeginHorizontal(GetRowStyle(index == selectedModalIndex), GUILayout.Height(rowHeight));
            var hero = index >= 0 && modalChoiceHeroes != null && index < modalChoiceHeroes.Count
                ? modalChoiceHeroes[index]
                : null;
            if (hero != null)
            {
                Sprite sprite;
                DrawSpriteNoFrame(UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null, 32f * scale);
            }
            else
            {
                GUILayout.Space(32f * scale);
            }

            GUILayout.Label(label, labelStyle, GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
            var rowRect = GUILayoutUtility.GetLastRect();
            if (Event.current == null ||
                Event.current.type != EventType.MouseDown ||
                Event.current.button != 0 ||
                !rowRect.Contains(Event.current.mousePosition))
            {
                return;
            }

            selectedModalIndex = index;
            SelectModalChoice(index);
            Event.current.Use();
        }

        private static void DrawModalBackdrop()
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void DrawSpriteNoFrame(Sprite sprite, float size)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            var aspect = textureRect.height <= 0f ? 1f : textureRect.width / textureRect.height;
            var drawWidth = rect.width;
            var drawHeight = aspect <= 0f ? rect.height : drawWidth / aspect;
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
            GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords, true);
        }

        private Vector2 BeginThemedScroll(Vector2 position, float height)
        {
            previousScrollbarThumb = GUI.skin.verticalScrollbarThumb;
            themedScrollActive = uiTheme != null;
            if (uiTheme != null)
            {
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
            }

            return GUILayout.BeginScrollView(
                position,
                false,
                false,
                GUIStyle.none,
                uiTheme == null ? GUI.skin.verticalScrollbar : uiTheme.VerticalScrollbarStyle,
                GUILayout.Height(height));
        }

        private void EndThemedScroll()
        {
            GUILayout.EndScrollView();
            if (themedScrollActive)
            {
                GUI.skin.verticalScrollbarThumb = previousScrollbarThumb;
            }

            previousScrollbarThumb = null;
            themedScrollActive = false;
        }

        private GUIStyle GetRarityStyle(Item item, GUIStyle fallback)
        {
            if (item == null || fallback == null)
            {
                return fallback;
            }

            var style = new GUIStyle(fallback);
            switch (item.Rarity)
            {
                case Rarity.Uncommon:
                    style.normal.textColor = Color.green;
                    break;
                case Rarity.Rare:
                    style.normal.textColor = new Color(0.4f, 0.6f, 1f);
                    break;
                case Rarity.Epic:
                    style.normal.textColor = new Color(0.75f, 0.4f, 1f);
                    break;
                default:
                    style.normal.textColor = uiTheme == null ? Color.white : uiTheme.TextColor;
                    break;
            }

            return style;
        }

        private GUIStyle GetCenteredRarityStyle(Item item)
        {
            var style = GetRarityStyle(item, labelStyle);
            if (style == null)
            {
                return null;
            }

            var centered = new GUIStyle(style)
            {
                alignment = TextAnchor.MiddleLeft
            };
            return centered;
        }

        private static GUIStyle GetRightAlignedStyle(GUIStyle fallback)
        {
            if (fallback == null)
            {
                return null;
            }

            return new GUIStyle(fallback)
            {
                alignment = TextAnchor.MiddleRight
            };
        }

        private bool StoreWillBuyItems()
        {
            return !IsKeyStoreObject() && GetBoolProperty("WillBuyItems", true);
        }

        private void HandleStoreInput()
        {
            if (InputManager.GetCommandDown(InputCommand.MenuPreviousTab))
            {
                currentFocus = StoreFocus.Items;
                SetCurrentTab(StoreTab.Buy);
                return;
            }

            if (InputManager.GetCommandDown(InputCommand.MenuNextTab) && StoreWillBuyItems())
            {
                currentFocus = StoreFocus.Items;
                SetCurrentTab(StoreTab.Sell);
                return;
            }

            var moveX = GetStoreMoveX();
            if (moveX != 0)
            {
                HandleHorizontalNavigation(moveX);
                return;
            }

            var moveY = GetStoreMoveY();
            if (moveY != 0)
            {
                HandleVerticalNavigation(moveY);
                return;
            }

            if (CanAcceptInteract() && InputManager.GetCommandDown(InputCommand.Interact))
            {
                ActivateSelectedStoreItem();
            }
        }

        private void HandleHorizontalNavigation(int moveX)
        {
            if (currentTab == StoreTab.Buy)
            {
                return;
            }

            var members = GetPartyMembers();
            if (members.Count > 0)
            {
                currentFocus = StoreFocus.SellMembers;
                SetSelectedHeroIndex(Mathf.Clamp(selectedHeroIndex + moveX, 0, members.Count - 1));
            }
        }

        private void HandleVerticalNavigation(int moveY)
        {
            if (currentTab == StoreTab.Buy)
            {
                var inventory = gameState == null ? new List<Item>() : gameState.GetStoreInventory(storeObject);
                if (inventory.Count > 0)
                {
                    currentFocus = StoreFocus.Items;
                    SetSelectedBuyIndex(Mathf.Clamp(selectedBuyIndex + moveY, 0, inventory.Count - 1));
                }

                return;
            }

            var members = GetPartyMembers();
            if (members.Count == 0)
            {
                return;
            }

            var hero = members[Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1)];
            var items = GetSellableItems(hero).ToList();
            if (items.Count > 0)
            {
                currentFocus = StoreFocus.Items;
                SetSelectedSellIndex(Mathf.Clamp(selectedSellIndex + moveY, 0, items.Count - 1));
            }
        }

        private void ActivateSelectedStoreItem()
        {
            if (currentTab == StoreTab.Buy)
            {
                var inventory = gameState == null ? new List<Item>() : gameState.GetStoreInventory(storeObject);
                if (inventory.Count == 0)
                {
                    return;
                }

                var item = inventory[Mathf.Clamp(selectedBuyIndex, 0, inventory.Count - 1)];
                if (CanBuy(item))
                {
                    UiControls.PlayConfirmSound();
                    ShowRecipientPicker(item);
                }

                return;
            }

            var members = GetPartyMembers();
            if (members.Count == 0)
            {
                return;
            }

            var hero = members[Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1)];
            var items = GetSellableItems(hero).ToList();
            if (items.Count == 0)
            {
                return;
            }

            UiControls.PlayConfirmSound();
            ShowSellConfirmation(hero, items[Mathf.Clamp(selectedSellIndex, 0, items.Count - 1)]);
        }

        private void HandleBuyRowMouse(Rect rowRect, Item item, int index)
        {
            if (Event.current == null ||
                Event.current.type != EventType.MouseDown ||
                Event.current.button != 0 ||
                !rowRect.Contains(Event.current.mousePosition))
            {
                return;
            }

            if (Event.current.clickCount >= 2 && CanBuy(item))
            {
                currentFocus = StoreFocus.Items;
                selectedBuyIndex = index;
                UiControls.PlayConfirmSound();
                ShowRecipientPicker(item);
                Event.current.Use();
            }
        }

        private void HandleSellRowMouse(Rect rowRect, Hero hero, ItemInstance item, int index)
        {
            if (Event.current == null ||
                Event.current.type != EventType.MouseDown ||
                Event.current.button != 0 ||
                !rowRect.Contains(Event.current.mousePosition))
            {
                return;
            }

            if (Event.current.clickCount >= 2)
            {
                currentFocus = StoreFocus.Items;
                selectedSellIndex = index;
                UiControls.PlayConfirmSound();
                ShowSellConfirmation(hero, item);
                Event.current.Use();
            }
        }

        private void HandleModalInput()
        {
            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
                HideModal();
                return;
            }

            var choiceCount = modalChoices == null || modalChoices.Count == 0 ? 1 : modalChoices.Count;
            var moveY = GetStoreMoveY();
            if (moveY != 0 && choiceCount > 1)
            {
                SetSelectedModalIndex(Mathf.Clamp(selectedModalIndex + moveY, 0, choiceCount - 1));
            }

            var moveX = GetStoreMoveX();
            if (moveX != 0 && choiceCount > 1)
            {
                SetSelectedModalIndex(Mathf.Clamp(selectedModalIndex + moveX, 0, choiceCount - 1));
            }

            if (!CanAcceptInteract() || !InputManager.GetCommandDown(InputCommand.Interact))
            {
                return;
            }

            UiControls.PlayConfirmSound();
            if (modalChoices == null || modalChoices.Count == 0)
            {
                HideModal();
                return;
            }

            SelectModalChoice(selectedModalIndex);
        }

        private void SelectModalChoice(int index)
        {
            var selected = modalSelected;
            HideModal();
            if (selected != null)
            {
                selected(index);
            }
        }

        private void ShowSellConfirmation(Hero hero, ItemInstance item)
        {
            ShowModal(
                "Sell " + item.Name,
                "Sell " + item.Name + " for " + GetSalePrice(item) + " gold?",
                new[] { "Sell", "Cancel" },
                index =>
                {
                    if (index == 0)
                    {
                        ShowModal("Store", gameState.SellHeroItem(storeObject, hero, item), null, null);
                    }
                });
        }

        private bool CanBuy(Item item)
        {
            return item != null &&
                   gameState != null &&
                   gameState.Party != null &&
                   gameState.Party.Gold >= item.Cost &&
                   GetBuyRecipients().Count > 0;
        }

        private GUIStyle GetRowStyle(bool selected)
        {
            if (uiTheme == null)
            {
                return GUI.skin.box;
            }

            return selected ? uiTheme.SelectedRowStyle : uiTheme.RowStyle;
        }

        private void DrawTabFocusIndicator(Rect rect, bool focused)
        {
            if (!focused || uiTheme == null || Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = uiTheme.HighlightColor;
            var thickness = Mathf.Max(2f, uiTheme.BorderThickness);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void SetCurrentTab(StoreTab tab)
        {
            if (tab == StoreTab.Sell && !StoreWillBuyItems())
            {
                return;
            }

            if (currentTab == tab)
            {
                return;
            }

            currentTab = tab;
            UiControls.PlaySelectSound();
        }

        private void SetSelectedHeroIndex(int index)
        {
            if (selectedHeroIndex == index)
            {
                return;
            }

            selectedHeroIndex = index;
            selectedSellIndex = 0;
            sellScroll = Vector2.zero;
            UiControls.PlaySelectSound();
        }

        private void SetSelectedBuyIndex(int index)
        {
            if (selectedBuyIndex == index)
            {
                return;
            }

            selectedBuyIndex = index;
            buyScroll.y = GetScrollYForSelectedIndex(selectedBuyIndex, buyScroll.y, buyListHeight);
            UiControls.PlaySelectSound();
        }

        private void SetSelectedSellIndex(int index)
        {
            if (selectedSellIndex == index)
            {
                return;
            }

            selectedSellIndex = index;
            sellScroll.y = GetScrollYForSelectedIndex(selectedSellIndex, sellScroll.y, sellListHeight);
            UiControls.PlaySelectSound();
        }

        private float GetScrollYForSelectedIndex(int index, float currentScrollY, float visibleHeight)
        {
            if (visibleHeight <= 0f)
            {
                return currentScrollY;
            }

            var rowHeight = GetStoreRowHeight();
            var rowTop = index * rowHeight;
            var rowBottom = rowTop + rowHeight;
            if (rowTop < currentScrollY)
            {
                return rowTop;
            }

            if (rowBottom > currentScrollY + visibleHeight)
            {
                return Mathf.Max(0f, rowBottom - visibleHeight);
            }

            return currentScrollY;
        }

        private float GetStoreRowHeight()
        {
            return 44f * GetPixelScale();
        }

        private void SetSelectedModalIndex(int index)
        {
            if (selectedModalIndex == index)
            {
                return;
            }

            selectedModalIndex = index;
            UiControls.PlaySelectSound();
        }

        private void BlockInteractUntilRelease()
        {
            acceptInteractAfterFrame = Time.frameCount + 1;
            waitForInteractRelease = true;
        }

        private bool CanAcceptInteract()
        {
            if (Time.frameCount <= acceptInteractAfterFrame)
            {
                return false;
            }

            if (!waitForInteractRelease)
            {
                return true;
            }

            if (InputManager.GetCommand(InputCommand.Interact))
            {
                return false;
            }

            waitForInteractRelease = false;
            return true;
        }

        private int GetStoreMoveX()
        {
            return GetRepeatedMove(InputManager.GetUiMoveXWithRightStick(), ref repeatingMoveX, ref nextMoveXTime);
        }

        private int GetStoreMoveY()
        {
            return GetRepeatedMove(InputManager.GetUiMoveYWithRightStick(), ref repeatingMoveY, ref nextMoveYTime);
        }

        private static int GetRepeatedMove(int held, ref int repeatingMove, ref float nextMoveTime)
        {
            if (held == 0)
            {
                repeatingMove = 0;
                nextMoveTime = 0f;
                return 0;
            }

            if (held != repeatingMove)
            {
                repeatingMove = held;
                nextMoveTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMoveTime)
            {
                return 0;
            }

            nextMoveTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetNavigationRepeat()
        {
            repeatingMoveX = 0;
            repeatingMoveY = 0;
            nextMoveXTime = 0f;
            nextMoveYTime = 0f;
        }

        private bool IsKeyStoreObject()
        {
            return storeObject != null &&
                   string.Equals(storeObject.Class, "NpcKey", StringComparison.OrdinalIgnoreCase);
        }

        private string GetStoreName()
        {
            return storeObject == null || string.IsNullOrEmpty(storeObject.Name) ||
                   string.Equals(storeObject.Name, "#Random#", StringComparison.OrdinalIgnoreCase)
                ? "Store"
                : storeObject.Name;
        }

        private string GetStoreText()
        {
            return IsKeyStoreObject()
                ? "Would you like to buy a key?"
                : GetStringProperty("Text", "Welcome to my store. I buy and sell items.");
        }

        private string GetStringProperty(string propertyName, string defaultValue)
        {
            string value;
            return storeObject != null &&
                   storeObject.Properties != null &&
                   storeObject.Properties.TryGetValue(propertyName, out value) &&
                   !string.IsNullOrEmpty(value)
                ? value
                : defaultValue;
        }

        private bool GetBoolProperty(string propertyName, bool defaultValue)
        {
            string value;
            bool result;
            return storeObject != null &&
                   storeObject.Properties != null &&
                   storeObject.Properties.TryGetValue(propertyName, out value) &&
                   bool.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private void Close()
        {
            storeObject = null;
            gameState = null;
            HideModal();
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (panelStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            titleStyle = uiTheme.TitleStyle;
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
    }
}
