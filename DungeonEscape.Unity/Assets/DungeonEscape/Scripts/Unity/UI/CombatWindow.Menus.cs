using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
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
            var skills = actingHero == null ? new List<Skill>() : GetAvailableActionSkills(actingHero).ToList();
            var rows = viewModel.GetActionRows(
                actingHero,
                actingHero != null && GetAvailableActionSpells(actingHero).Any(),
                skills,
                actingHero != null && GetAvailableActionItems(actingHero).Any(),
                actingHeroActionQueued,
                actingHeroBonusActionQueued,
                actingHero != null && GetAvailableBonusActionButtons().Any());
            foreach (var row in rows)
            {
                switch (row.Kind)
                {
                    case CombatActionKind.Fight:
                        yield return new CombatButton(row.Label, BeginWeaponSelection);
                        break;
                    case CombatActionKind.BonusAction:
                        yield return new CombatButton(row.Label, BeginBonusActionSelection);
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
                    case CombatActionKind.EndTurn:
                        yield return new CombatButton(row.Label, EndHeroTurn);
                        break;
                    case CombatActionKind.Run:
                        yield return new CombatButton(row.Label, ResolveHeroRun);
                        break;
                }
            }
        }

        private void DrawSpellMenu(Rect panelRect, float scale)
        {
            var spells = actingHero == null ? new List<Spell>() : GetAvailableActionSpells(actingHero).ToList();
            DrawIconList(
                panelRect,
                scale,
                "Spell",
                spells,
                viewModel.GetSpellRows(spells),
                (Spell spell, out Sprite sprite) => UiAssetResolver.TryGetSpellSprite(spell, out sprite),
                ResolveHeroSpell);
        }

        private void DrawWeaponMenu(Rect panelRect, float scale)
        {
            var weapons = actingHero == null ? new List<ItemInstance>() : GetAvailableFightWeapons(actingHero).ToList();
            DrawIconList(
                panelRect,
                scale,
                "Weapon",
                weapons,
                GetWeaponRows(weapons),
                (ItemInstance item, out Sprite sprite) => UiAssetResolver.TryGetItemSprite(item, out sprite),
                ResolveHeroWeapon);
        }

        private void DrawBonusActionMenu(Rect panelRect, float scale)
        {
            DrawMenuButtons(panelRect, scale, "Bonus Action", GetAvailableBonusActionButtons());
        }

        private void DrawItemMenu(Rect panelRect, float scale)
        {
            var items = actingHero == null ? new List<ItemInstance>() : GetAvailableActionItems(actingHero).ToList();
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

        private int GetCurrentWeaponIndex(IList<ItemInstance> weapons)
        {
            if (weapons == null || weapons.Count == 0)
            {
                return 0;
            }

            var primaryWeapon = GetPrimaryEquippedWeapon(actingHero);
            if (primaryWeapon != null)
            {
                var primaryIndex = weapons.IndexOf(primaryWeapon);
                if (primaryIndex >= 0)
                {
                    return primaryIndex;
                }
            }

            var equippedIndex = weapons.Select((item, index) => new { item, index })
                .Where(row => row.item != null && row.item.IsEquipped)
                .Select(row => row.index)
                .FirstOrDefault();
            return equippedIndex;
        }

        private int GetRememberedTargetIndex(IList<IFighter> targets)
        {
            return selectionMemory.GetRememberedTargetIndex(actingHero, targets);
        }

        private List<Spell> GetAvailableEncounterSpells(Hero hero)
        {
            return viewModel.GetAvailableEncounterSpells(
                hero,
                GameDataCache.Current == null ? null : GameDataCache.Current.Spells);
        }

        private List<Spell> GetAvailableActionSpells(Hero hero)
        {
            return GetAvailableEncounterSpells(hero).Where(spell => spell != null && !spell.IsBonusAction).ToList();
        }

        private List<Spell> GetAvailableBonusSpells(Hero hero)
        {
            return GetAvailableEncounterSpells(hero).Where(spell => spell != null && spell.IsBonusAction).ToList();
        }

        private List<ItemInstance> GetAvailableFightWeapons(Hero hero)
        {
            if (hero == null || hero.IsDead || hero.Items == null)
            {
                return new List<ItemInstance>();
            }

            var weapons = new List<ItemInstance>();
            var primaryWeapon = GetPrimaryEquippedWeapon(hero);
            if (primaryWeapon != null)
            {
                weapons.Add(primaryWeapon);
            }

            weapons.AddRange(hero.Items.Where(item =>
                item != null &&
                !weapons.Contains(item) &&
                item.Type == ItemType.Weapon &&
                item.IsEquipped));
            weapons.AddRange(hero.Items.Where(item =>
                item != null &&
                !weapons.Contains(item) &&
                item.Type == ItemType.Weapon &&
                !item.IsEquipped &&
                hero.CanEquipItem(item)));
            return weapons;
        }

        private List<CombatMenuRow> GetWeaponRows(IList<ItemInstance> weapons)
        {
            var rows = new List<CombatMenuRow>();
            if (weapons == null)
            {
                return rows;
            }

            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                if (weapon != null)
                {
                    rows.Add(new CombatMenuRow { Index = i, Label = FormatWeaponRow(weapon) });
                }
            }

            return rows;
        }

        private string FormatWeaponRow(ItemInstance weapon)
        {
            if (weapon == null)
            {
                return "";
            }

            var attack = DndStatRules.GetAttackBonus(actingHero, weapon);
            var damage = DndStatRules.GetDamageBonus(actingHero, weapon);
            var dice = Math.Max(1, DndStatRules.GetDamageDice(actingHero, weapon)) + "d" + DndStatRules.GetDamageDie(actingHero, weapon);
            return weapon.Name + "  Atk " + FormatSigned(attack) + "  " + dice + FormatSigned(damage);
        }

        private static ItemInstance GetPrimaryEquippedWeapon(Hero hero)
        {
            if (hero == null || hero.Slots == null || hero.Items == null)
            {
                return null;
            }

            string itemId;
            if (!hero.Slots.TryGetValue(Slot.PrimaryHand, out itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return hero.Items.FirstOrDefault(item => item != null && item.Id == itemId && item.IsEquipped && item.Type == ItemType.Weapon);
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private List<Skill> GetAvailableEncounterSkills(Hero hero)
        {
            return viewModel.GetAvailableEncounterSkills(
                hero,
                GameDataCache.Current == null ? null : GameDataCache.Current.Skills);
        }

        private List<Skill> GetAvailableActionSkills(Hero hero)
        {
            return GetAvailableEncounterSkills(hero).Where(skill => skill != null && !skill.IsBonusAction).ToList();
        }

        private List<Skill> GetAvailableBonusSkills(Hero hero)
        {
            return GetAvailableEncounterSkills(hero).Where(skill => skill != null && skill.IsBonusAction).ToList();
        }

        private List<ItemInstance> GetAvailableEncounterItems(Hero hero)
        {
            if (hero == null || hero.Items == null)
            {
                return viewModel.GetAvailableEncounterItems(hero);
            }

            foreach (var item in hero.Items)
            {
                if (item != null)
                {
                    EnsureItemLinked(item);
                }
            }

            return viewModel.GetAvailableEncounterItems(hero);
        }

        private List<ItemInstance> GetAvailableActionItems(Hero hero)
        {
            return GetAvailableEncounterItems(hero)
                .Where(item => item == null || item.Item == null || item.Item.Skill == null || !item.Item.Skill.IsBonusAction)
                .ToList();
        }

        private List<ItemInstance> GetAvailableBonusItems(Hero hero)
        {
            return GetAvailableEncounterItems(hero)
                .Where(item => item != null && item.Item != null && item.Item.Skill != null && item.Item.Skill.IsBonusAction)
                .ToList();
        }

        private List<CombatButton> GetAvailableBonusActionButtons()
        {
            var buttons = new List<CombatButton>();
            if (actingHero == null || actingHero.IsDead || actingHeroBonusActionQueued)
            {
                return buttons;
            }

            foreach (var spell in GetAvailableBonusSpells(actingHero))
            {
                var selectedSpell = spell;
                buttons.Add(new CombatButton("Spell: " + selectedSpell.Name, () => ResolveHeroSpell(selectedSpell, true)));
            }

            foreach (var skill in GetAvailableBonusSkills(actingHero))
            {
                var selectedSkill = skill;
                buttons.Add(new CombatButton(selectedSkill.Name, () => ResolveHeroSkill(selectedSkill, true)));
            }

            foreach (var item in GetAvailableBonusItems(actingHero))
            {
                var selectedItem = item;
                buttons.Add(new CombatButton("Item: " + selectedItem.Name, () => ResolveHeroItem(selectedItem, true)));
            }

            buttons.Add(new CombatButton("Back", ReturnToActionMenu));
            return buttons;
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
