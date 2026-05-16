using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private void DrawActionMenu(Rect panelRect, float scale)
        {
            var actions = BuildActionButtons().ToList();
            DrawMenuButtons(panelRect, scale, actingHero == null ? string.Empty : actingHero.Name, actions);
        }

        private IEnumerable<CombatButton> BuildActionButtons()
        {
            var skills = actingHero == null ? new List<Skill>() : GetAvailableEncounterSkills(actingHero).ToList();
            var rows = viewModel.GetActionRows(
                actingHero,
                actingHero != null && GetAvailableEncounterSpells(actingHero).Any(),
                skills,
                actingHero != null && GetAvailableEncounterItems(actingHero).Any());
            foreach (var row in rows)
            {
                switch (row.Kind)
                {
                    case CombatActionKind.Fight:
                        yield return new CombatButton(row.Label, BeginTargetSelection);
                        break;
                    case CombatActionKind.Spell:
                        yield return new CombatButton(row.Label, BeginSpellSelection);
                        break;
                    case CombatActionKind.Skill:
                        if (row.SkillIndex >= 0 && row.SkillIndex < skills.Count)
                        {
                            var selectedSkill = skills[row.SkillIndex];
                            yield return new CombatButton(row.Label, () => ResolveHeroSkill(selectedSkill));
                        }
                        break;
                    case CombatActionKind.Item:
                        yield return new CombatButton(row.Label, BeginItemSelection);
                        break;
                    case CombatActionKind.Run:
                        yield return new CombatButton(row.Label, ResolveHeroRun);
                        break;
                }
            }
        }

        private void DrawSpellMenu(Rect panelRect, float scale)
        {
            var spells = actingHero == null ? new List<Spell>() : GetAvailableEncounterSpells(actingHero).ToList();
            DrawIconList(
                panelRect,
                scale,
                "Spell",
                spells,
                viewModel.GetSpellRows(spells),
                (Spell spell, out Sprite sprite) => UiAssetResolver.TryGetSpellSprite(spell, out sprite),
                ResolveHeroSpell);
        }

        private void DrawItemMenu(Rect panelRect, float scale)
        {
            var items = actingHero == null ? new List<ItemInstance>() : GetAvailableEncounterItems(actingHero).ToList();
            DrawIconList(
                panelRect,
                scale,
                "Item",
                items,
                viewModel.GetItemRows(items),
                (ItemInstance item, out Sprite sprite) => UiAssetResolver.TryGetItemSprite(item, out sprite),
                ResolveHeroItem);
        }

        private delegate bool TryGetSpriteDelegate<T>(T value, out Sprite sprite);

        private void DrawIconList<T>(
            Rect panelRect,
            float scale,
            string title,
            IList<T> values,
            IList<CombatMenuRow> rows,
            TryGetSpriteDelegate<T> getSprite,
            Action<T> onSelect)
        {
            var rowHeight = GetCombatMenuRowHeight(scale);
            var menuWidth = GetCombatMenuWidth(panelRect, scale);
            var x = panelRect.x + 14f * scale;
            var y = GetCombatMenuY(panelRect, scale);
            DrawCombatMenuTitle(x, panelRect.y, menuWidth, scale, title);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Index < 0 || row.Index >= values.Count)
                {
                    continue;
                }

                var value = values[row.Index];
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), menuWidth, rowHeight);
                var selected = row.Index == selectedMenuIndex;
                if (GUI.Button(rect, GUIContent.none, GetCombatRowStyle(selected)))
                {
                    UiControls.PlayConfirmSound();
                    selectedMenuIndex = row.Index;
                    onSelect(value);
                }

                Sprite sprite;
                if (getSprite(value, out sprite) && sprite != null && sprite.texture != null)
                {
                    var iconSize = 26f * scale;
                    DrawSprite(sprite, new Rect(rect.x + 6f * scale, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize));
                }

                GUI.Label(
                    new Rect(rect.x + 40f * scale, rect.y, rect.width - 46f * scale, rect.height),
                    row.Label,
                    GetCombatRowLabelStyle(selected));
            }

        }

        private void DrawMenuButtons(Rect panelRect, float scale, string title, IList<CombatButton> buttons)
        {
            var menuWidth = GetCombatMenuWidth(panelRect, scale);
            var rowHeight = GetCombatMenuRowHeight(scale);
            var x = panelRect.x + 14f * scale;
            var y = GetCombatMenuY(panelRect, scale);
            DrawCombatMenuTitle(x, panelRect.y, menuWidth, scale, title);
            for (var i = 0; i < buttons.Count; i++)
            {
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), menuWidth, rowHeight);
                var selected = i == selectedMenuIndex;
                if (GUI.Button(rect, GUIContent.none, GetCombatRowStyle(selected)))
                {
                    UiControls.PlayConfirmSound();
                    selectedMenuIndex = i;
                    RememberAction(buttons[i].Label);
                    buttons[i].Action();
                }

                GUI.Label(new Rect(rect.x + 8f * scale, rect.y, rect.width - 16f * scale, rect.height), buttons[i].Label, GetCombatRowLabelStyle(selected));
            }
        }

        private static float GetCombatMenuWidth(Rect panelRect, float scale)
        {
            return Mathf.Min(310f * scale, panelRect.width - 28f * scale);
        }

        private static float GetCombatMenuRowHeight(float scale)
        {
            return 32f * scale;
        }

        private void DrawCombatMenuTitle(float x, float panelY, float width, float scale, string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            GUI.Label(new Rect(x, panelY + 8f * scale, width, 24f * scale), title, titleStyle);
        }

        private static float GetCombatMenuY(Rect panelRect, float scale)
        {
            return panelRect.y + 40f * scale;
        }

        private void RememberAction(string label)
        {
            selectionMemory.RememberAction(actingHero, label);
        }

        private void RememberSpell(Spell spell)
        {
            selectionMemory.RememberSpell(actingHero, spell);
        }

        private void RememberItem(ItemInstance item)
        {
            selectionMemory.RememberItem(actingHero, item);
        }

        private void RememberTarget(IFighter target)
        {
            selectionMemory.RememberTarget(actingHero, target);
        }

        private int GetRememberedActionIndex(IList<CombatButton> actions)
        {
            return selectionMemory.GetRememberedActionIndex(actingHero, actions);
        }

        private int GetRememberedSpellIndex(IList<Spell> spells)
        {
            return selectionMemory.GetRememberedSpellIndex(actingHero, spells);
        }

        private int GetRememberedItemIndex(IList<ItemInstance> items)
        {
            return selectionMemory.GetRememberedItemIndex(actingHero, items);
        }

        private int GetRememberedTargetIndex(IList<IFighter> targets)
        {
            return selectionMemory.GetRememberedTargetIndex(actingHero, targets);
        }

        private IEnumerable<Spell> GetAvailableEncounterSpells(Hero hero)
        {
            return hero == null ||
                   hero.IsDead ||
                   GameDataCache.Current == null ||
                   GameDataCache.Current.Spells == null
                ? Enumerable.Empty<Spell>()
                : hero.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => spell != null && spell.IsEncounterSpell && spell.Cost <= hero.Magic);
        }

        private IEnumerable<Skill> GetAvailableEncounterSkills(Hero hero)
        {
            return hero == null ||
                   hero.IsDead ||
                   GameDataCache.Current == null ||
                   GameDataCache.Current.Skills == null
                ? Enumerable.Empty<Skill>()
                : hero.GetSkills(GameDataCache.Current.Skills)
                    .Where(skill => skill != null && skill.IsEncounterSkill);
        }

        private IEnumerable<ItemInstance> GetAvailableEncounterItems(Hero hero)
        {
            return hero == null || hero.IsDead || hero.Items == null
                ? Enumerable.Empty<ItemInstance>()
                : hero.Items.Where(item =>
                {
                    EnsureItemLinked(item);
                    return item != null &&
                           item.Item != null &&
                           item.Item.Skill != null &&
                           item.Item.Skill.IsEncounterSkill &&
                           item.HasCharges;
                });
        }

        private void DrawCenteredButtons(Rect panelRect, float scale, IEnumerable<CombatButton> buttons)
        {
            var buttonList = buttons.ToList();
            var buttonWidth = 112f * scale;
            var buttonHeight = 32f * scale;
            var gap = 10f * scale;
            var totalWidth = buttonList.Count * buttonWidth + Math.Max(0, buttonList.Count - 1) * gap;
            var startX = panelRect.x + (panelRect.width - totalWidth) / 2f;
            var y = panelRect.yMax - buttonHeight - 16f * scale;
            for (var i = 0; i < buttonList.Count; i++)
            {
                var rect = new Rect(startX + i * (buttonWidth + gap), y, buttonWidth, buttonHeight);
                if (UiControls.Button(rect, buttonList[i].Label, buttonStyle))
                {
                    buttonList[i].Action();
                }
            }
        }

        private void DrawTargetSelectionFooter(Rect panelRect, float scale)
        {
            if (!string.IsNullOrEmpty(targetSelectionTitle))
            {
                GUI.Label(
                    new Rect(panelRect.x + 14f * scale, panelRect.y + 10f * scale, panelRect.width - 28f * scale, 26f * scale),
                    targetSelectionTitle,
                    titleStyle);
            }

        }

        private void DrawSelectionBorder(Rect rect, float scale)
        {
            var previousColor = GUI.color;
            GUI.color = uiTheme == null ? Color.yellow : uiTheme.HighlightColor;
            var thickness = uiTheme == null ? Mathf.Max(1f, scale) : Mathf.Max(1f, uiTheme.BorderThickness);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private IFighter GetSelectedTarget()
        {
            return viewModel.GetSelectedTarget(targetSelectionCandidates);
        }

        private bool IsCurrentTargetCandidate(IFighter fighter)
        {
            return state == CombatState.ChooseTarget &&
                   viewModel.IsTargetCandidate(targetSelectionCandidates, fighter);
        }

        private bool IsMonsterTargetSelection()
        {
            return state == CombatState.ChooseTarget &&
                   viewModel.HasMonsterTargets(targetSelectionCandidates);
        }

        private void SelectTarget(IFighter target)
        {
            if (targetSelectionCandidates == null || target == null)
            {
                return;
            }

            var index = viewModel.GetTargetIndex(targetSelectionCandidates, target);
            if (index < 0)
            {
                return;
            }

            UiControls.PlayConfirmSound();
            selectedMenuIndex = index;
            ActivateTargetSelection(index);
        }

        private void ActivateTargetSelection(int index)
        {
            if (targetSelectionCandidates == null || index < 0 || index >= targetSelectionCandidates.Count)
            {
                return;
            }

            var target = targetSelectionCandidates[index];
            UiControls.PlayConfirmSound();
            RememberTarget(target);
            var done = targetSelectionDone;
            targetSelectionDone = null;
            targetSelectionCandidates.Clear();
            if (done != null)
            {
                done(new List<IFighter> { target });
            }
        }

        private void Close()
        {
            Close(true);
        }

        private void Close(bool restoreMapMusic)
        {
            ClearRoundStatusEffects();
            IsOpen = false;
            GameState.AutoSaveBlocked = false;
            if (ReferenceEquals(currentWindow, this))
            {
                currentWindow = null;
            }

            if (restoreMapMusic)
            {
                var currentBiome = gameState == null || gameState.Party == null ? biome : gameState.Party.CurrentBiome;
                Audio.GetOrCreate().RestoreMapOrBiomeMusic(currentBiome);
            }
        }

        private void ClearRoundStatusEffects()
        {
            var party = gameState == null ? null : gameState.Party;
            if (party == null || party.Members == null)
            {
                return;
            }

            foreach (var hero in party.Members.Where(member => member != null))
            {
                foreach (var effect in hero.Status.Where(item => item.DurationType == DurationType.Rounds).ToList())
                {
                    hero.RemoveEffect(effect);
                }
            }
        }
    }
}
