using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();
            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = WindowDepth;
            GUI.color = Color.white;

            DrawBackground();
            DrawMonsters();
            DrawFooter();

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }

        private void DrawBackground()
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var background = CombatAssetLoader.LoadTexture(CombatAssetLoader.GetBackgroundAssetPath(biome));
            if (background != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
            }
        }

        private void DrawMonsters()
        {
            var scale = GetPixelScale();
            var encounterMonsters = monsters
                .OrderBy(monster => monster.Data.MinLevel)
                .ThenBy(monster => monster.Data.Name)
                .ToList();
            if (encounterMonsters.Count == 0)
            {
                return;
            }

            var battlefield = GetBattlefieldRect(scale);
            var slotWidth = 122f * scale;
            var slotHeight = 132f * scale;
            var gap = 12f * scale;
            var totalWidth = encounterMonsters.Count * slotWidth + Math.Max(0, encounterMonsters.Count - 1) * gap;
            var startX = battlefield.x + (battlefield.width - totalWidth) / 2f;
            var y = battlefield.y + battlefield.height * 0.56f;
            var selectedTarget = GetSelectedTarget();
            for (var i = 0; i < encounterMonsters.Count; i++)
            {
                var monster = encounterMonsters[i];
                if (monster.Instance == null || monster.Instance.RanAway)
                {
                    continue;
                }

                var damageFlashActive = IsFighterDamageFlashActive(monster.Instance.Name);
                if (monster.Instance.IsDead && !IsDefeatedFighterVisible(monster.Instance))
                {
                    continue;
                }

                var texture = CombatAssetLoader.LoadMonsterTexture(monster.Data);
                var slotRect = new Rect(startX + i * (slotWidth + gap), y, slotWidth, slotHeight);
                var targetRect = new Rect(slotRect.x, slotRect.y, slotRect.width, slotRect.height + 28f * scale);
                var selectable = IsCurrentTargetCandidate(monster.Instance);
                var selected = ReferenceEquals(monster.Instance, selectedTarget);
                if (selectable && GUI.Button(targetRect, GUIContent.none, GUIStyle.none))
                {
                    SelectTarget(monster.Instance);
                }

                if (texture != null)
                {
                    var previousColor = GUI.color;
                    if (IsFighterHealFlashing(monster.Instance.Name))
                    {
                        GUI.color = Color.blue;
                    }
                    else if (damageFlashActive && IsFighterDamageFlashing(monster.Instance.Name))
                    {
                        GUI.color = Color.red;
                    }

                    DrawTextureAtNativeCombatSize(texture, slotRect, scale);
                    GUI.color = previousColor;
                }

                if (state == CombatState.ChooseTarget && selectable)
                {
                    var nameStyle = new GUIStyle(GetTargetButtonStyle(monster.Instance, selected))
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                    GUI.Label(new Rect(slotRect.x, slotRect.y - 24f * scale, slotRect.width, 22f * scale), monster.Instance.Name, nameStyle);
                }

                DrawHealthBar(
                    monster.Instance.Health,
                    monster.Instance.MaxHealth,
                    new Rect(slotRect.x + 8f * scale, slotRect.yMax + 10f * scale, slotRect.width - 16f * scale, 14f * scale));

                if (selected)
                {
                    DrawSelectionBorder(targetRect, scale);
                }
            }
        }

        private void DrawFooter()
        {
            var scale = GetPixelScale();
            var panelWidth = Screen.width - 16f * scale;
            var panelHeight = Mathf.Min(220f * scale, Screen.height * 0.32f);
            var panelRect = new Rect(8f * scale, Screen.height - panelHeight - 8f * scale, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            if (state == CombatState.Message)
            {
                var messageBottomPadding = IsTextFullyRevealed ? 56f * scale : 16f * scale;
                var messageRect = new Rect(
                    panelRect.x + 14f * scale,
                    panelRect.y + 12f * scale,
                    panelRect.width - 28f * scale,
                    panelRect.height - 24f * scale - messageBottomPadding);
                DrawScrollableMessage(messageRect, DisplayedMessage, scale);
            }

            if (state == CombatState.ChooseAction)
            {
                DrawActionMenu(panelRect, scale);
                return;
            }

            if (state == CombatState.ChooseTarget)
            {
                DrawTargetSelectionFooter(panelRect, scale);
                return;
            }

            if (state == CombatState.ChooseSpell)
            {
                DrawSpellMenu(panelRect, scale);
                return;
            }

            if (state == CombatState.ChooseItem)
            {
                DrawItemMenu(panelRect, scale);
                return;
            }

            if (IsTextFullyRevealed)
            {
                DrawCenteredButtons(panelRect, scale, new[] { new CombatButton("OK", ContinueMessage) });
            }
        }

        private void DrawScrollableMessage(Rect rect, string text, float scale)
        {
            var content = text ?? "";
            var contentHeight = Mathf.Max(
                rect.height,
                labelStyle.CalcHeight(new GUIContent(content), rect.width - 18f * scale) + 4f * scale);
            if (contentHeight <= rect.height + 1f)
            {
                GUI.Label(rect, content, labelStyle);
                return;
            }

            var previousVerticalScrollbar = GUI.skin.verticalScrollbar;
            var previousVerticalThumb = GUI.skin.verticalScrollbarThumb;
            if (uiTheme != null)
            {
                GUI.skin.verticalScrollbar = uiTheme.VerticalScrollbarStyle;
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
            }

            messageScrollPosition = GUI.BeginScrollView(
                rect,
                messageScrollPosition,
                new Rect(0f, 0f, rect.width - 18f * scale, contentHeight),
                false,
                true,
                GUIStyle.none,
                uiTheme == null ? GUI.skin.verticalScrollbar : uiTheme.VerticalScrollbarStyle);
            GUI.Label(new Rect(0f, 0f, rect.width - 18f * scale, contentHeight), content, labelStyle);
            GUI.EndScrollView();
            GUI.skin.verticalScrollbar = previousVerticalScrollbar;
            GUI.skin.verticalScrollbarThumb = previousVerticalThumb;
        }

        private static Rect GetBattlefieldRect(float scale)
        {
            var footerHeight = Mathf.Min(220f * scale, Screen.height * 0.32f);
            return new Rect(0f, 0f, Screen.width, Screen.height - footerHeight - 16f * scale);
        }
    }
}
