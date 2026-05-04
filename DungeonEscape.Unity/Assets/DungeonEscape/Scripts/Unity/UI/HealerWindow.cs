using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class HealerWindow : MonoBehaviour
    {
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private enum HealerFocus
        {
            Service,
            Target
        }

        private enum HealerService
        {
            Heal,
            HealAll,
            RenewMagic,
            Cure,
            Revive
        }

        private sealed class ServiceRow
        {
            public HealerService Service;
            public string Label;
            public int Cost;
            public List<Hero> Targets;
            public bool NeedsTarget;
        }

        private static HealerWindow instance;

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
            focus = HealerFocus.Service;
            selectedServiceIndex = 0;
            selectedTargetIndex = 0;
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
                    focus = HealerFocus.Service;
                    selectedTargetIndex = 0;
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

            selectedServiceIndex = Mathf.Clamp(selectedServiceIndex, 0, services.Count - 1);
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

        private void DrawServiceRow(ServiceRow row, int index)
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
                selectedServiceIndex = index;
                focus = row.NeedsTarget ? HealerFocus.Target : HealerFocus.Service;
                selectedTargetIndex = 0;
                if (!row.NeedsTarget && Event.current.clickCount >= 2)
                {
                    ApplyService(row, null);
                }
            }
        }

        private void DrawTargetPanel(ServiceRow service)
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
            selectedTargetIndex = Mathf.Clamp(selectedTargetIndex, 0, Mathf.Max(targets.Count - 1, 0));
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
                selectedTargetIndex = index;
                focus = HealerFocus.Target;
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

            selectedServiceIndex = Mathf.Clamp(selectedServiceIndex, 0, services.Count - 1);
            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                if (focus == HealerFocus.Target && services[selectedServiceIndex].NeedsTarget)
                {
                    selectedTargetIndex = Mathf.Clamp(selectedTargetIndex + moveY, 0, Math.Max(services[selectedServiceIndex].Targets.Count - 1, 0));
                }
                else
                {
                    selectedServiceIndex = Mathf.Clamp(selectedServiceIndex + moveY, 0, services.Count - 1);
                    selectedTargetIndex = 0;
                }

                UiControls.PlaySelectSound();
            }

            var moveX = GetMenuMoveX();
            if (moveX < 0)
            {
                focus = HealerFocus.Service;
                UiControls.PlaySelectSound();
            }
            else if (moveX > 0 && services[selectedServiceIndex].NeedsTarget)
            {
                focus = HealerFocus.Target;
                selectedTargetIndex = Mathf.Clamp(selectedTargetIndex, 0, Math.Max(services[selectedServiceIndex].Targets.Count - 1, 0));
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

            selectedServiceIndex = Mathf.Clamp(selectedServiceIndex, 0, services.Count - 1);
            var service = services[selectedServiceIndex];
            if (service.NeedsTarget && focus != HealerFocus.Target)
            {
                focus = HealerFocus.Target;
                selectedTargetIndex = 0;
                BlockInteractUntilRelease();
                return;
            }

            var target = service.NeedsTarget && service.Targets.Count > 0
                ? service.Targets[Mathf.Clamp(selectedTargetIndex, 0, service.Targets.Count - 1)]
                : null;
            ApplyService(service, target);
        }

        private void ApplyService(ServiceRow service, Hero target)
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
            focus = HealerFocus.Service;
            selectedTargetIndex = 0;
            BlockInteractUntilRelease();
        }

        private List<ServiceRow> BuildServices()
        {
            if (gameState == null || gameState.Party == null)
            {
                return new List<ServiceRow>();
            }

            var cost = GetHealerCost();
            var wounded = gameState.Party.AliveMembers.Where(member => member.Health != member.MaxHealth).ToList();
            var magicMissing = gameState.Party.AliveMembers.Where(member => member.Magic != member.MaxMagic).ToList();
            var statusMembers = gameState.Party.AliveMembers.Where(member => member.Status != null && member.Status.Count != 0).ToList();
            var dead = gameState.Party.DeadMembers.ToList();
            var rows = new List<ServiceRow>();
            if (wounded.Count > 0)
            {
                rows.Add(new ServiceRow { Service = HealerService.Heal, Label = "Heal", Cost = cost, Targets = wounded, NeedsTarget = true });
                if (wounded.Count > 1)
                {
                    rows.Add(new ServiceRow { Service = HealerService.HealAll, Label = "Heal All", Cost = cost * wounded.Count });
                }
            }

            if (magicMissing.Count > 0)
            {
                rows.Add(new ServiceRow { Service = HealerService.RenewMagic, Label = "Renew Magic", Cost = cost * 2 * magicMissing.Count });
            }

            if (statusMembers.Count > 0)
            {
                rows.Add(new ServiceRow { Service = HealerService.Cure, Label = "Cure", Cost = cost * 2, Targets = statusMembers, NeedsTarget = true });
            }

            if (dead.Count > 0)
            {
                rows.Add(new ServiceRow { Service = HealerService.Revive, Label = "Revive", Cost = cost * 10, Targets = dead, NeedsTarget = true });
            }

            return rows;
        }

        private int GetHealerCost()
        {
            string value;
            int result;
            return healerObject != null &&
                   healerObject.Properties != null &&
                   healerObject.Properties.TryGetValue("Cost", out value) &&
                   int.TryParse(value, out result)
                ? result
                : 25;
        }

        private string GetHealerName()
        {
            return healerObject == null || string.IsNullOrEmpty(healerObject.Name) ? "Healer" : healerObject.Name;
        }

        private string GetHealerText()
        {
            string text;
            return healerObject != null &&
                   healerObject.Properties != null &&
                   healerObject.Properties.TryGetValue("Text", out text) &&
                   !string.IsNullOrEmpty(text)
                ? text
                : "Do you require my services as a healer?";
        }

        private void Close()
        {
            healerObject = null;
            resultMessage = null;
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
