using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeStoreWindow : MonoBehaviour
    {
        private enum StoreTab
        {
            Buy,
            Sell
        }

        private static DungeonEscapeStoreWindow instance;

        private DungeonEscapeGameState gameState;
        private TiledObjectInfo storeObject;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private StoreTab currentTab = StoreTab.Buy;
        private int selectedHeroIndex;
        private Vector2 buyScroll;
        private Vector2 sellScroll;
        private string modalTitle;
        private string modalMessage;
        private List<string> modalChoices;
        private Action<int> modalSelected;

        public static bool IsOpen
        {
            get { return instance != null && instance.storeObject != null; }
        }

        public static void Show(DungeonEscapeGameState state, TiledObjectInfo mapObject)
        {
            GetOrCreate().Open(state, mapObject);
        }

        private static DungeonEscapeStoreWindow GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<DungeonEscapeStoreWindow>();
            if (instance != null)
            {
                return instance;
            }

            instance = new GameObject("DungeonEscapeStoreWindow").AddComponent<DungeonEscapeStoreWindow>();
            return instance;
        }

        private void Open(DungeonEscapeGameState state, TiledObjectInfo mapObject)
        {
            gameState = state == null ? DungeonEscapeGameState.GetOrCreate() : state;
            storeObject = mapObject;
            currentTab = StoreTab.Buy;
            selectedHeroIndex = 0;
            buyScroll = Vector2.zero;
            sellScroll = Vector2.zero;
            HideModal();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (!IsModalVisible() && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                Close();
            }
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
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(GetStoreText(), smallStyle);
        }

        private void DrawTabs()
        {
            var scale = GetPixelScale();
            GUILayout.BeginHorizontal();
            if (DungeonEscapeUiControls.TabButton("Buy", currentTab == StoreTab.Buy, uiTheme, 34f * scale))
            {
                currentTab = StoreTab.Buy;
            }

            GUI.enabled = StoreWillBuyItems();
            if (DungeonEscapeUiControls.TabButton("Sell", currentTab == StoreTab.Sell, uiTheme, 34f * scale))
            {
                currentTab = StoreTab.Sell;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawBuyTab(float height)
        {
            var inventory = gameState == null ? new List<Item>() : gameState.GetStoreInventory(storeObject);
            if (inventory.Count == 0)
            {
                GUILayout.Label("I have nothing to sell right now.", labelStyle);
                return;
            }

            buyScroll = BeginThemedScroll(buyScroll, height);
            foreach (var item in inventory.ToList())
            {
                DrawBuyItemRow(item);
            }

            GUILayout.EndScrollView();
        }

        private void DrawBuyItemRow(Item item)
        {
            GUILayout.BeginHorizontal(uiTheme == null ? GUI.skin.box : uiTheme.RowStyle);
            Sprite sprite;
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                34f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(item.NameWithStats, GetRarityStyle(item, labelStyle));
            GUILayout.Label(item.Type + "  Level " + item.MinLevel + "  " + item.Cost + "g", smallStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = gameState != null && gameState.Party != null && gameState.Party.Gold >= item.Cost && GetBuyRecipients().Count > 0;
            if (GUILayout.Button("Buy", buttonStyle, GUILayout.Width(86f * GetPixelScale())))
            {
                ShowRecipientPicker(item);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
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
                if (DungeonEscapeUiControls.TabButton(members[i].Name, selectedHeroIndex == i, uiTheme, 34f * GetPixelScale()))
                {
                    selectedHeroIndex = i;
                    sellScroll = Vector2.zero;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            var items = GetSellableItems(hero).ToList();
            if (items.Count == 0)
            {
                GUILayout.Label(hero.Name + " has no sellable items.", labelStyle);
                return;
            }

            sellScroll = BeginThemedScroll(sellScroll, height - 44f * GetPixelScale());
            foreach (var item in items)
            {
                DrawSellItemRow(hero, item);
            }

            GUILayout.EndScrollView();
        }

        private void DrawSellItemRow(Hero hero, ItemInstance item)
        {
            GUILayout.BeginHorizontal(uiTheme == null ? GUI.skin.box : uiTheme.RowStyle);
            Sprite sprite;
            DungeonEscapeUiControls.SpriteIcon(
                DungeonEscapeUiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                34f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(item.NameWithStats + (item.IsEquipped ? "  Equipped" : ""), GetRarityStyle(item.Item, labelStyle));
            GUILayout.Label(item.Type + "  " + GetSalePrice(item) + "g", smallStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Sell", buttonStyle, GUILayout.Width(86f * GetPixelScale())))
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

            GUILayout.EndHorizontal();
        }

        private void ShowRecipientPicker(Item item)
        {
            var recipients = GetBuyRecipients();
            var labels = recipients.Select(hero => hero.Name + " (" + hero.Items.Count + "/" + Party.MaxItems + ")").ToList();
            labels.Add("Cancel");
            ShowModal("Buy " + item.Name, "Who should carry this item?", labels, index =>
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
            modalTitle = title;
            modalMessage = message;
            modalChoices = choices == null ? null : choices.ToList();
            modalSelected = selected;
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
                    if (DungeonEscapeUiControls.Button(choice.label, false, uiTheme))
                    {
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
            else if (GUILayout.Button("OK", buttonStyle, GUILayout.Width(120f * scale)))
            {
                HideModal();
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

        private bool StoreWillBuyItems()
        {
            return !IsKeyStoreObject() && GetBoolProperty("WillBuyItems", true);
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
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (panelStyle != null &&
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
