using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class HealerWindow : MonoBehaviour
    {
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static HealerWindow instance;

        private readonly HealerViewModel viewModel = new HealerViewModel();
        private GameState gameState;
        private TiledObjectInfo healerObject;
        private Action changed;
        private UiSettings uiSettings;
        private UiTheme uiTheme;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle panelStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private HealerFocus focus;
        private int selectedServiceIndex;
        private int selectedTargetIndex;
        private string resultMessage;
        private int repeatingMoveX;
        private int repeatingMoveY;
        private float nextMoveXTime;
        private float nextMoveYTime;
        private int acceptInteractAfterFrame;
        private bool waitForInteractRelease;

        public static bool IsOpen
        {
            get { return instance != null && instance.healerObject != null; }
        }

        public static bool IsOpenFor(string mapId, int objectId)
        {
            return instance != null &&
                   instance.healerObject != null &&
                   instance.healerObject.Id == objectId &&
                   instance.gameState != null &&
                   instance.gameState.Party != null &&
                   string.Equals(instance.gameState.Party.CurrentMapId, mapId, StringComparison.OrdinalIgnoreCase);
        }

        public static void Show(GameState state, TiledObjectInfo mapObject, Action onChanged)
        {
            GetOrCreate().Open(state, mapObject, onChanged);
        }

        private static HealerWindow GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<HealerWindow>();
            if (instance != null)
            {
                return instance;
            }

            instance = new GameObject("HealerWindow").AddComponent<HealerWindow>();
            return instance;
        }

        private void Open(GameState state, TiledObjectInfo mapObject, Action onChanged)
        {
            gameState = state == null ? GameState.GetOrCreate() : state;
            healerObject = mapObject;
            changed = onChanged;
            viewModel.Reset();
            focus = viewModel.Focus;
            selectedServiceIndex = viewModel.SelectedServiceIndex;
            selectedTargetIndex = viewModel.SelectedTargetIndex;
            resultMessage = null;
            ResetNavigationRepeat();
            BlockInteractUntilRelease();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
            if (focus == HealerFocus.Target)
            {
                    SetFocus(HealerFocus.Service);
                    SetSelectedTargetIndex(0);
                    return;
                }

                Close();
                return;
            }

            HandleInput();
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
            var width = Mathf.Min(Screen.width - 32f * scale, 780f * scale);
            var height = Mathf.Min(Screen.height - 32f * scale, 500f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, rect.height - 28f * scale));
            DrawHeader();
            GUILayout.Space(10f * scale);
            DrawBody(height - 118f * scale);
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(resultMessage))
            {
                GUILayout.Label(resultMessage, smallStyle);
            }

            GUILayout.EndArea();
            GUI.depth = previousDepth;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetHealerName(), titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gold: " + (gameState == null || gameState.Party == null ? 0 : gameState.Party.Gold), labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label(GetHealerText(), smallStyle);
        }

        private void DrawBody(float height)
        {
            var services = BuildServices();
            if (services.Count == 0)
            {
                GUILayout.Label("You do not require any of my services.", labelStyle);
                return;
            }

            viewModel.ClampServiceSelection(services);
            selectedServiceIndex = viewModel.SelectedServiceIndex;
            var selectedService = services[selectedServiceIndex];
            GUILayout.BeginHorizontal(GUILayout.Height(height));
            GUILayout.BeginVertical(GUILayout.Width(330f * GetPixelScale()));
            for (var i = 0; i < services.Count; i++)
            {
                DrawServiceRow(services[i], i);
            }

            GUILayout.EndVertical();
            GUILayout.Space(12f * GetPixelScale());
            DrawTargetPanel(selectedService);
            GUILayout.EndHorizontal();
        }

        private void DrawServiceRow(HealerServiceRow row, int index)
        {
            var selected = focus == HealerFocus.Service && selectedServiceIndex == index;
            GUILayout.BeginHorizontal(GetRowStyle(selected), GUILayout.Height(GetRowHeight()));
            GUILayout.Label(row.Label, GetRowLabelStyle(selected), GUILayout.Height(GetRowHeight()));
            GUILayout.FlexibleSpace();
            GUILayout.Label(row.Cost + "g", GetRightAlignedStyle(GetRowLabelStyle(selected)), GUILayout.Width(72f * GetPixelScale()), GUILayout.Height(GetRowHeight()));
            GUILayout.EndHorizontal();
            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SetSelectedServiceIndex(index);
                SetFocus(row.NeedsTarget ? HealerFocus.Target : HealerFocus.Service);
                selectedTargetIndex = viewModel.SelectedTargetIndex;
                if (!row.NeedsTarget && Event.current.clickCount >= 2)
                {
                    ApplyService(row, null);
                }
            }
        }

        private void DrawTargetPanel(HealerServiceRow service)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (service == null || !service.NeedsTarget)
            {
                GUILayout.Label("Press Action to confirm.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Target", labelStyle);
            var targets = service.Targets ?? new List<Hero>();
            viewModel.ClampTargetSelection(service);
            selectedTargetIndex = viewModel.SelectedTargetIndex;
            if (targets.Count == 0)
            {
                GUILayout.Label("No valid targets.", smallStyle);
                GUILayout.EndVertical();
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                DrawTargetRow(targets[i], i);
            }

            GUILayout.EndVertical();
        }

        private void DrawTargetRow(Hero hero, int index)
        {
            var selected = focus == HealerFocus.Target && selectedTargetIndex == index;
            GUILayout.BeginHorizontal(GetRowStyle(selected), GUILayout.Height(GetRowHeight()));
            Sprite sprite;
            UiControls.SpriteIcon(UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null, 34f * GetPixelScale(), uiTheme);
            GUILayout.Label(hero.Name, GetRowLabelStyle(selected), GUILayout.Height(GetRowHeight()));
            GUILayout.FlexibleSpace();
            GUILayout.Label(hero.Health + "/" + hero.MaxHealth + " HP", GetRightAlignedStyle(GetRowLabelStyle(selected)), GUILayout.Width(110f * GetPixelScale()), GUILayout.Height(GetRowHeight()));
            GUILayout.EndHorizontal();
            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SetSelectedTargetIndex(index);
                SetFocus(HealerFocus.Target);
                if (Event.current.clickCount >= 2)
                {
                    ApplySelected();
                }
            }
        }

        private void HandleInput()
        {
            if (waitForInteractRelease)
            {
                if (InputManager.GetCommand(InputCommand.Interact))
                {
                    return;
                }

                waitForInteractRelease = false;
            }

            var services = BuildServices();
            if (services.Count == 0)
            {
                return;
            }

            viewModel.ClampServiceSelection(services);
            selectedServiceIndex = viewModel.SelectedServiceIndex;
            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                if (focus == HealerFocus.Target && services[selectedServiceIndex].NeedsTarget)
                {
                    SetSelectedTargetIndex(Clamp(selectedTargetIndex + moveY, 0, Math.Max(services[selectedServiceIndex].Targets.Count - 1, 0)));
                }
                else
                {
                    SetSelectedServiceIndex(Clamp(selectedServiceIndex + moveY, 0, services.Count - 1));
                }

                UiControls.PlaySelectSound();
            }

            var moveX = GetMenuMoveX();
            if (moveX < 0)
            {
                SetFocus(HealerFocus.Service);
                UiControls.PlaySelectSound();
            }
            else if (moveX > 0 && services[selectedServiceIndex].NeedsTarget)
            {
                SetFocus(HealerFocus.Target);
                SetSelectedTargetIndex(Clamp(selectedTargetIndex, 0, Math.Max(services[selectedServiceIndex].Targets.Count - 1, 0)));
                UiControls.PlaySelectSound();
            }

            if (CanAcceptInteract() && InputManager.GetCommandDown(InputCommand.Interact))
            {
                UiControls.PlayConfirmSound();
                ApplySelected();
            }
        }

        private void ApplySelected()
        {
            var services = BuildServices();
            if (services.Count == 0)
            {
                return;
            }

            var service = viewModel.GetSelectedService(services);
            selectedServiceIndex = viewModel.SelectedServiceIndex;
            if (service.NeedsTarget && focus != HealerFocus.Target)
            {
                SetFocus(HealerFocus.Target);
                SetSelectedTargetIndex(0);
                BlockInteractUntilRelease();
                return;
            }

            var target = service.NeedsTarget ? viewModel.GetSelectedTarget(service) : null;
            selectedTargetIndex = viewModel.SelectedTargetIndex;
            ApplyService(service, target);
        }

        private void ApplyService(HealerServiceRow service, Hero target)
        {
            if (service == null || gameState == null)
            {
                return;
            }

            switch (service.Service)
            {
                case HealerService.Heal:
                    resultMessage = gameState.HealHero(target, service.Cost);
                    break;
                case HealerService.HealAll:
                    resultMessage = gameState.HealAllHeroes(service.Cost);
                    break;
                case HealerService.RenewMagic:
                    resultMessage = gameState.RenewMagic(service.Cost);
                    break;
                case HealerService.Cure:
                    resultMessage = gameState.CureHero(target, service.Cost);
                    break;
                case HealerService.Revive:
                    resultMessage = gameState.ReviveHero(target, service.Cost);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            changed?.Invoke();
            SetFocus(HealerFocus.Service);
            SetSelectedTargetIndex(0);
            BlockInteractUntilRelease();
        }

        private List<HealerServiceRow> BuildServices()
        {
            return viewModel.BuildServices(gameState == null ? null : gameState.Party, healerObject);
        }

        private int GetHealerCost()
        {
            return viewModel.GetHealerCost(healerObject);
        }

        private string GetHealerName()
        {
            return viewModel.GetHealerName(healerObject);
        }

        private string GetHealerText()
        {
            return viewModel.GetHealerText(healerObject);
        }

        private void Close()
        {
            healerObject = null;
            resultMessage = null;
        }

        private void SetFocus(HealerFocus value)
        {
            viewModel.SetFocus(value);
            focus = viewModel.Focus;
        }

        private void SetSelectedServiceIndex(int index)
        {
            viewModel.SetSelectedServiceIndex(index);
            selectedServiceIndex = viewModel.SelectedServiceIndex;
            selectedTargetIndex = viewModel.SelectedTargetIndex;
        }

        private void SetSelectedTargetIndex(int index)
        {
            viewModel.SetSelectedTargetIndex(index);
            selectedTargetIndex = viewModel.SelectedTargetIndex;
        }

        private bool CanAcceptInteract()
        {
            return Time.frameCount >= acceptInteractAfterFrame && !waitForInteractRelease;
        }

        private void BlockInteractUntilRelease()
        {
            acceptInteractAfterFrame = Time.frameCount + 1;
            waitForInteractRelease = InputManager.GetCommand(InputCommand.Interact);
        }

        private int GetMenuMoveY()
        {
            var moveY = InputManager.GetUiMoveYWithRightStick();
            return GetRepeatedMove(moveY, ref repeatingMoveY, ref nextMoveYTime);
        }

        private int GetMenuMoveX()
        {
            var moveX = InputManager.GetUiMoveXWithRightStick();
            return GetRepeatedMove(moveX, ref repeatingMoveX, ref nextMoveXTime);
        }

        private static int GetRepeatedMove(int move, ref int repeatingMove, ref float nextMoveTime)
        {
            if (move == 0)
            {
                repeatingMove = 0;
                nextMoveTime = 0f;
                return 0;
            }

            if (repeatingMove != move)
            {
                repeatingMove = move;
                nextMoveTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return move;
            }

            if (Time.unscaledTime < nextMoveTime)
            {
                return 0;
            }

            nextMoveTime = Time.unscaledTime + NavigationRepeatDelay;
            return move;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private void ResetNavigationRepeat()
        {
            repeatingMoveX = 0;
            repeatingMoveY = 0;
            nextMoveXTime = 0f;
            nextMoveYTime = 0f;
        }

        private void EnsureStyles()
        {
            uiSettings = UiSettings.GetOrCreate();
            var scale = uiSettings == null ? 1f : uiSettings.PixelScale;
            var settings = SettingsCache.Current;
            var signature = UiTheme.GetSignature(settings);
            if (uiTheme != null && Mathf.Approximately(lastPixelScale, scale) && string.Equals(lastThemeSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            uiTheme = UiTheme.Create(settings, scale);
            lastPixelScale = scale;
            lastThemeSignature = signature;
            titleStyle = new GUIStyle(uiTheme.TitleStyle) { fontSize = Mathf.RoundToInt(24f * scale), alignment = TextAnchor.MiddleLeft };
            labelStyle = new GUIStyle(uiTheme.LabelStyle) { alignment = TextAnchor.MiddleLeft };
            smallStyle = new GUIStyle(uiTheme.LabelStyle) { fontSize = Mathf.RoundToInt(14f * scale), alignment = TextAnchor.MiddleLeft, wordWrap = true };
            panelStyle = uiTheme.PanelStyle;
        }

        private GUIStyle GetRowStyle(bool selected)
        {
            return uiTheme == null ? GUI.skin.box : selected ? uiTheme.SelectedRowStyle : uiTheme.RowStyle;
        }

        private GUIStyle GetRowLabelStyle(bool selected)
        {
            var style = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            if (uiTheme != null)
            {
                style.normal.textColor = selected ? uiTheme.HighlightColor : uiTheme.TextColor;
            }

            return style;
        }

        private static GUIStyle GetRightAlignedStyle(GUIStyle source)
        {
            return new GUIStyle(source) { alignment = TextAnchor.MiddleRight };
        }

        private float GetPixelScale()
        {
            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }

        private float GetRowHeight()
        {
            return 42f * GetPixelScale();
        }
    }
}
