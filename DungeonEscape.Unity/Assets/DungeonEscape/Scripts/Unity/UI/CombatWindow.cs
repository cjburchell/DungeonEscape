using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class CombatWindow : MonoBehaviour
    {
        private const int WindowDepth = -2500;
        private const string MonsterTilesetAssetPath = "Assets/DungeonEscape/Tilesets/allmonsters.tsx";
        private static readonly Dictionary<int, string> MonsterImagePaths = new Dictionary<int, string>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Random CombatRandom = new System.Random();

        private enum CombatState
        {
            Message,
            ChooseAction,
            ChooseTarget,
            ChooseSpell,
            ChooseItem
        }

        private sealed class CombatMonster
        {
            public Monster Data { get; set; }
            public MonsterInstance Instance { get; set; }
        }

        private sealed class CombatTurn
        {
            public IFighter Actor { get; set; }
            public bool IsHero { get; set; }
            public int Initiative { get; set; }
        }

        private readonly List<CombatMonster> monsters = new List<CombatMonster>();
        private readonly List<CombatTurn> turnOrder = new List<CombatTurn>();
        private Biome biome;
        private GameState gameState;
        private UiSettings uiSettings;
        private UiTheme uiTheme;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle titleStyle;
        private float lastPixelScale;
        private string lastThemeSignature;
        private CombatState state;
        private string messageText;
        private Action afterMessage;
        private Hero actingHero;
        private Action<List<IFighter>> targetSelectionDone;
        private List<IFighter> targetSelectionCandidates = new List<IFighter>();
        private string targetSelectionTitle;
        private int selectedMenuIndex;
        private int round;
        private int turnIndex;

        public static bool IsOpen { get; private set; }

        public static void Open(IEnumerable<Monster> encounterMonsters, Biome encounterBiome)
        {
            var window = FindAnyObjectByType<CombatWindow>();
            if (window == null)
            {
                window = new GameObject("CombatWindow").AddComponent<CombatWindow>();
            }

            window.monsters.Clear();
            window.turnOrder.Clear();
            window.targetSelectionCandidates.Clear();
            window.targetSelectionDone = null;
            window.gameState = GameState.GetOrCreate();
            if (encounterMonsters != null)
            {
                window.CreateMonsterInstances(encounterMonsters.Where(monster => monster != null));
            }

            window.biome = encounterBiome;
            window.round = 0;
            window.turnIndex = 0;
            window.selectedMenuIndex = 0;
            window.actingHero = null;
            window.ShowMessage(window.GetEncounterMessage(), window.BeginRound);
            IsOpen = window.monsters.Count > 0;
            GameState.AutoSaveBlocked = IsOpen;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsOpen = false;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (state == CombatState.Message)
            {
                if (InputManager.GetCommandDown(InputCommand.Interact) ||
                    InputManager.GetCommandDown(InputCommand.Cancel))
                {
                    ContinueMessage();
                }

                return;
            }

            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                ReturnToActionMenu();
                return;
            }

            var moveY = InputManager.GetMoveYDown();
            if (moveY != 0)
            {
                MoveSelection(moveY);
            }

            if (InputManager.GetCommandDown(InputCommand.Interact))
            {
                ActivateSelection();
            }
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                IsOpen = false;
                GameState.AutoSaveBlocked = false;
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
            var background = LoadTexture(GetBackgroundAssetPath(biome));
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
            for (var i = 0; i < encounterMonsters.Count; i++)
            {
                var monster = encounterMonsters[i];
                var texture = LoadMonsterTexture(monster.Data);
                var slotRect = new Rect(startX + i * (slotWidth + gap), y, slotWidth, slotHeight);
                if (texture != null)
                {
                    DrawTextureAtNativeCombatSize(texture, slotRect, scale);
                }

                DrawHealthBar(
                    monster.Instance.Health,
                    monster.Instance.MaxHealth,
                    new Rect(slotRect.x + 8f * scale, slotRect.yMax + 10f * scale, slotRect.width - 16f * scale, 14f * scale));
            }
        }

        private void DrawFooter()
        {
            var scale = GetPixelScale();
            var panelWidth = Screen.width - 16f * scale;
            var panelHeight = Mathf.Min(220f * scale, Screen.height * 0.32f);
            var panelRect = new Rect(8f * scale, Screen.height - panelHeight - 8f * scale, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            GUI.Label(
                new Rect(panelRect.x + 14f * scale, panelRect.y + 12f * scale, panelRect.width - 28f * scale, 70f * scale),
                messageText,
                labelStyle);

            if (state == CombatState.ChooseAction)
            {
                DrawActionMenu(panelRect, scale);
                return;
            }

            if (state == CombatState.ChooseTarget)
            {
                DrawTargetButtons(panelRect, scale);
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

            DrawCenteredButtons(panelRect, scale, new[] { new CombatButton("OK", ContinueMessage) });
        }

        private static Rect GetBattlefieldRect(float scale)
        {
            var footerHeight = Mathf.Min(220f * scale, Screen.height * 0.32f);
            return new Rect(0f, 0f, Screen.width, Screen.height - footerHeight - 16f * scale);
        }

        private string GetEncounterMessage()
        {
            return monsters.Count == 1
                ? "You have encountered a " + monsters[0].Instance.Name + "!"
                : "You have encountered " + monsters.Count + " enemies!";
        }

        private void CreateMonsterInstances(IEnumerable<Monster> encounterMonsters)
        {
            foreach (var monsterGroup in encounterMonsters.OrderBy(monster => monster.MinLevel).GroupBy(monster => monster.Name))
            {
                var monsterId = 'A';
                foreach (var monster in monsterGroup)
                {
                    var instance = new MonsterInstance(monster, gameState);
                    if (monsterGroup.Count() != 1)
                    {
                        instance.Name = instance.Name + " " + monsterId;
                        monsterId++;
                    }

                    monsters.Add(new CombatMonster
                    {
                        Data = monster,
                        Instance = instance
                    });
                }
            }
        }

        private void ShowMessage(string text, Action next)
        {
            state = CombatState.Message;
            selectedMenuIndex = 0;
            messageText = text;
            afterMessage = next;
        }

        private void ContinueMessage()
        {
            if (state != CombatState.Message)
            {
                return;
            }

            var next = afterMessage;
            afterMessage = null;
            if (next == null)
            {
                Close();
                return;
            }

            next();
        }

        private void MoveSelection(int moveY)
        {
            var count = GetCurrentSelectionCount();
            if (count <= 0)
            {
                selectedMenuIndex = 0;
                return;
            }

            selectedMenuIndex = Mathf.Clamp(selectedMenuIndex + (moveY > 0 ? 1 : -1), 0, count - 1);
        }

        private int GetCurrentSelectionCount()
        {
            switch (state)
            {
                case CombatState.ChooseAction:
                    return BuildActionButtons().Count();
                case CombatState.ChooseSpell:
                    return actingHero == null ? 0 : GetAvailableEncounterSpells(actingHero).Count();
                case CombatState.ChooseItem:
                    return actingHero == null ? 0 : GetAvailableEncounterItems(actingHero).Count();
                case CombatState.ChooseTarget:
                    return targetSelectionCandidates == null ? 0 : targetSelectionCandidates.Count;
                default:
                    return 0;
            }
        }

        private void ActivateSelection()
        {
            switch (state)
            {
                case CombatState.ChooseAction:
                    var actions = BuildActionButtons().ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < actions.Count)
                    {
                        actions[selectedMenuIndex].Action();
                    }

                    return;
                case CombatState.ChooseSpell:
                    var spells = actingHero == null ? new List<Spell>() : GetAvailableEncounterSpells(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < spells.Count)
                    {
                        ResolveHeroSpell(spells[selectedMenuIndex]);
                    }

                    return;
                case CombatState.ChooseItem:
                    var items = actingHero == null ? new List<ItemInstance>() : GetAvailableEncounterItems(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < items.Count)
                    {
                        ResolveHeroItem(items[selectedMenuIndex]);
                    }

                    return;
                case CombatState.ChooseTarget:
                    ActivateTargetSelection(selectedMenuIndex);
                    return;
            }
        }

        private void ReturnToActionMenu()
        {
            if (state == CombatState.ChooseAction)
            {
                return;
            }

            targetSelectionDone = null;
            targetSelectionCandidates.Clear();
            state = CombatState.ChooseAction;
            selectedMenuIndex = 0;
            messageText = actingHero == null ? "Choose an action." : actingHero.Name + "'s turn.";
        }

        private void BeginRound()
        {
            round++;
            actingHero = null;
            turnIndex = 0;
            turnOrder.Clear();

            var party = gameState == null ? null : gameState.Party;
            if (party != null)
            {
                foreach (var hero in party.AliveMembers.Where(CanBeAttacked))
                {
                    turnOrder.Add(new CombatTurn
                    {
                        Actor = hero,
                        IsHero = true,
                        Initiative = RollInitiative(hero)
                    });
                }
            }

            foreach (var monster in AliveMonsters())
            {
                turnOrder.Add(new CombatTurn
                {
                    Actor = monster.Instance,
                    IsHero = false,
                    Initiative = RollInitiative(monster.Instance)
                });
            }

            turnOrder.Sort((left, right) => right.Initiative.CompareTo(left.Initiative));
            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            if (!AliveHeroes().Any())
            {
                ShowMessage("The party has been defeated.", null);
                return;
            }

            if (!AliveMonsters().Any())
            {
                ShowMessage(GetVictoryMessage(), null);
                return;
            }

            while (turnIndex < turnOrder.Count)
            {
                var turn = turnOrder[turnIndex];
                turnIndex++;
                if (!CanBeAttacked(turn.Actor))
                {
                    continue;
                }

                if (turn.IsHero)
                {
                    actingHero = turn.Actor as Hero;
                    if (actingHero != null && actingHero.Status.Any(effect => effect.Type == EffectType.Sleep))
                    {
                        ShowMessage(actingHero.Name + " is asleep.", AdvanceTurn);
                        return;
                    }

                    if (actingHero != null && actingHero.Status.Any(effect => effect.Type == EffectType.Confusion))
                    {
                        var confusedTargets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>()
                            .Concat(AliveHeroes().Cast<IFighter>())
                            .Where(target => target != actingHero)
                            .ToList();
                        if (confusedTargets.Count > 0)
                        {
                            ResolveHeroAttack(confusedTargets[CombatRandom.Next(confusedTargets.Count)]);
                            return;
                        }
                    }

                    state = CombatState.ChooseAction;
                    selectedMenuIndex = 0;
                    messageText = actingHero == null ? "Choose an action." : actingHero.Name + "'s turn.";
                    return;
                }

                ResolveMonsterTurn(turn.Actor);
                return;
            }

            BeginRound();
        }

        private void ResolveMonsterTurn(IFighter monster)
        {
            var targets = AliveHeroes().Cast<IFighter>().ToList();
            if (monster == null || targets.Count == 0)
            {
                AdvanceTurn();
                return;
            }

            var target = targets[CombatRandom.Next(targets.Count)];
            ShowMessage(Fight(monster, target), AdvanceTurn);
        }

        private void BeginTargetSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                AdvanceTurn();
                return;
            }

            BeginTargetSelection(
                "Choose a target for " + actingHero.Name + ".",
                AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList(),
                Target.Single,
                1,
                targets => ResolveHeroAttack(targets.FirstOrDefault()));
        }

        private void BeginSpellSelection()
        {
            if (actingHero == null)
            {
                return;
            }

            var spells = GetAvailableEncounterSpells(actingHero).ToList();
            if (spells.Count == 0)
            {
                ShowMessage(actingHero.Name + " cannot cast any combat spells.", AdvanceTurn);
                return;
            }

            state = CombatState.ChooseSpell;
            selectedMenuIndex = 0;
            messageText = "Choose a spell for " + actingHero.Name + ".";
        }

        private void BeginItemSelection()
        {
            if (actingHero == null)
            {
                return;
            }

            var items = GetAvailableEncounterItems(actingHero).ToList();
            if (items.Count == 0)
            {
                ShowMessage(actingHero.Name + " has no combat items.", AdvanceTurn);
                return;
            }

            state = CombatState.ChooseItem;
            selectedMenuIndex = 0;
            messageText = "Choose an item for " + actingHero.Name + ".";
        }

        private void BeginTargetSelection(
            string title,
            List<IFighter> candidates,
            Target targetMode,
            int maxTargets,
            Action<List<IFighter>> done,
            bool allowDead = false)
        {
            candidates = candidates == null
                ? new List<IFighter>()
                : candidates.Where(candidate => allowDead ? candidate != null : CanBeAttacked(candidate)).ToList();
            if (targetMode == Target.None || candidates.Count == 0)
            {
                done(new List<IFighter>());
                return;
            }

            if (targetMode == Target.Group)
            {
                done(maxTargets > 0 ? candidates.Take(maxTargets).ToList() : candidates);
                return;
            }

            if (candidates.Count == 1)
            {
                done(new List<IFighter> { candidates[0] });
                return;
            }

            targetSelectionTitle = title;
            targetSelectionCandidates = candidates;
            targetSelectionDone = done;
            state = CombatState.ChooseTarget;
            selectedMenuIndex = 0;
            messageText = title;
        }

        private void ResolveHeroAttack(IFighter target)
        {
            if (actingHero == null || target == null)
            {
                AdvanceTurn();
                return;
            }

            ShowMessage(Fight(actingHero, target), AdvanceTurn);
        }

        private void ResolveHeroSpell(Spell spell)
        {
            if (actingHero == null || spell == null)
            {
                AdvanceTurn();
                return;
            }

            spell.Setup(GameDataCache.Current == null ? null : GameDataCache.Current.Skills);
            var candidates = spell.IsAttackSpell
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySpellTargets(spell);
            BeginTargetSelection(
                "Choose a target for " + spell.Name + ".",
                candidates,
                spell.Targets,
                spell.MaxTargets,
                targets => ShowMessage(CastSpell(spell, targets), AdvanceTurn),
                spell.Type == SkillType.Revive);
        }

        private void ResolveHeroSkill(Skill skill)
        {
            if (actingHero == null || skill == null)
            {
                AdvanceTurn();
                return;
            }

            var candidates = skill.IsAttackSkill
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySkillTargets(skill);
            BeginTargetSelection(
                "Choose a target for " + skill.Name + ".",
                candidates,
                skill.Targets,
                skill.MaxTargets,
                targets => ShowMessage(DoSkill(skill, targets), AdvanceTurn),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroItem(ItemInstance item)
        {
            if (actingHero == null || item == null || item.Item == null)
            {
                AdvanceTurn();
                return;
            }

            EnsureItemLinked(item);
            var skill = item.Item.Skill;
            if (skill == null)
            {
                ShowMessage(item.Name + " cannot be used in combat.", AdvanceTurn);
                return;
            }

            var candidates = skill.IsAttackSkill
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySkillTargets(skill);
            BeginTargetSelection(
                "Choose a target for " + item.Name + ".",
                candidates,
                item.Target,
                skill.MaxTargets,
                targets => ShowMessage(UseItem(item, targets), AdvanceTurn),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroRun()
        {
            if (actingHero == null)
            {
                AdvanceTurn();
                return;
            }

            var fastestMonster = AliveMonsters()
                .Select(monster => monster.Instance)
                .OrderByDescending(monster => monster.Agility)
                .FirstOrDefault();
            var message = actingHero.Name + " tried to run.\n";
            if (fastestMonster == null || actingHero.CanHit(fastestMonster))
            {
                Audio.GetOrCreate().PlaySoundEffect("stairs-up");
                ShowMessage(message + "And got away.", null);
                return;
            }

            ShowMessage(message + "But could not get away.", AdvanceTurn);
        }

        private List<IFighter> GetPartySpellTargets(Spell spell)
        {
            return spell != null && spell.Type == SkillType.Revive
                ? DeadHeroes().Cast<IFighter>().ToList()
                : AliveHeroes().Cast<IFighter>().ToList();
        }

        private List<IFighter> GetPartySkillTargets(Skill skill)
        {
            return skill != null && skill.Type == SkillType.Revive
                ? DeadHeroes().Cast<IFighter>().ToList()
                : AliveHeroes().Cast<IFighter>().ToList();
        }

        private string CastSpell(Spell spell, List<IFighter> targets)
        {
            if (actingHero == null || spell == null)
            {
                return "";
            }

            var message = spell.Cast(targets ?? new List<IFighter>(), new BaseState[0], actingHero, gameState, round);
            return string.IsNullOrEmpty(message) ? actingHero.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        private string DoSkill(Skill skill, List<IFighter> targets)
        {
            if (actingHero == null || skill == null)
            {
                return "";
            }

            var message = actingHero.Name + " uses " + skill.Name + ".\n";
            var selectedTargets = targets ?? new List<IFighter>();
            if (selectedTargets.Count == 0)
            {
                var result = skill.Do(null, actingHero, null, gameState, round);
                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }
            }

            foreach (var target in selectedTargets)
            {
                var result = skill.Do(target, actingHero, null, gameState, round);
                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }
            }

            return message.TrimEnd();
        }

        private string UseItem(ItemInstance item, List<IFighter> targets)
        {
            if (actingHero == null || item == null)
            {
                return "";
            }

            var message = "";
            var worked = false;
            var selectedTargets = targets == null || targets.Count == 0
                ? new List<IFighter> { actingHero }
                : targets;
            foreach (var target in selectedTargets)
            {
                var result = item.Use(actingHero, target, null, gameState, round);
                if (result.Item2)
                {
                    worked = true;
                }

                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1 + "\n";
                }
            }

            if (worked && item.Type == ItemType.OneUse)
            {
                item.UnEquip(gameState.Party.Members);
                actingHero.Items.Remove(item);
            }
            else if (!item.HasCharges)
            {
                item.UnEquip(gameState.Party.Members);
                actingHero.Items.Remove(item);
                message += item.Name + " has been destroyed.\n";
            }

            return string.IsNullOrEmpty(message) ? actingHero.Name + " used " + item.Name + "." : message.TrimEnd();
        }

        private string Fight(IFighter source, IFighter target)
        {
            var message = source.Name + " attacks " + target.Name + ".\n";
            var damage = 0;
            if (source.CanCriticalHit(target))
            {
                damage = target.CalculateDamage(RandomAttack(source.CriticalAttack));
                message += "Heroic maneuver!\n";
                message += target.Name;
            }
            else if (source.CanHit(target))
            {
                damage = target.CalculateDamage(RandomAttack(source.Attack));
                message += target.Name;
            }
            else
            {
                message += target.Name + " dodges the attack and";
            }

            if (damage <= 0)
            {
                message += " was unharmed";
            }
            else
            {
                target.Health -= damage;
                target.PlayDamageAnimation();
                message += " took " + damage + " points of damage";
                message += "\n" + target.HitCheck().TrimEnd();
            }

            if (target.IsDead)
            {
                target.Health = 0;
                message += "\nand has died!";
            }

            Audio.GetOrCreate().PlaySoundEffect(damage == 0 ? "miss" : "receive-damage");
            return message.TrimEnd();
        }

        private static int RandomAttack(int attack)
        {
            return attack <= 0 ? 0 : CombatRandom.Next(attack);
        }

        private static int RollInitiative(IFighter fighter)
        {
            return Dice.RollD20() + (fighter == null ? 0 : fighter.Agility);
        }

        private static void EnsureItemLinked(ItemInstance item)
        {
            if (item == null || item.Item == null || GameDataCache.Current == null)
            {
                return;
            }

            item.Item.Setup(GameDataCache.Current.Skills);
        }

        private static bool CanBeAttacked(IFighter fighter)
        {
            return fighter != null && !fighter.IsDead && !fighter.RanAway;
        }

        private IEnumerable<CombatMonster> AliveMonsters()
        {
            return monsters.Where(monster => monster != null && CanBeAttacked(monster.Instance));
        }

        private IEnumerable<Hero> AliveHeroes()
        {
            return gameState == null || gameState.Party == null
                ? Enumerable.Empty<Hero>()
                : gameState.Party.AliveMembers.Where(CanBeAttacked);
        }

        private IEnumerable<Hero> DeadHeroes()
        {
            return gameState == null || gameState.Party == null
                ? Enumerable.Empty<Hero>()
                : gameState.Party.DeadMembers.Where(hero => hero != null);
        }

        private string GetVictoryMessage()
        {
            return gameState == null
                ? "The enemies have been defeated."
                : gameState.ApplyCombatRewards(monsters.Select(monster => monster.Instance));
        }

        private void DrawActionMenu(Rect panelRect, float scale)
        {
            var actions = BuildActionButtons().ToList();
            DrawMenuButtons(panelRect, scale, actingHero == null ? "Action" : actingHero.Name + " Action", actions);
        }

        private IEnumerable<CombatButton> BuildActionButtons()
        {
            yield return new CombatButton("Fight", BeginTargetSelection);

            if (actingHero != null &&
                !actingHero.Status.Any(effect => effect.Type == EffectType.StopSpell) &&
                GetAvailableEncounterSpells(actingHero).Any())
            {
                yield return new CombatButton("Spell", BeginSpellSelection);
            }

            if (actingHero != null)
            {
                foreach (var skill in GetAvailableEncounterSkills(actingHero))
                {
                    var selectedSkill = skill;
                    yield return new CombatButton(selectedSkill.Name, () => ResolveHeroSkill(selectedSkill));
                }
            }

            if (actingHero != null && GetAvailableEncounterItems(actingHero).Any())
            {
                yield return new CombatButton("Item", BeginItemSelection);
            }

            yield return new CombatButton("Run", ResolveHeroRun);
        }

        private void DrawSpellMenu(Rect panelRect, float scale)
        {
            var spells = actingHero == null ? new List<Spell>() : GetAvailableEncounterSpells(actingHero).ToList();
            DrawIconList(
                panelRect,
                scale,
                "Spell",
                spells,
                spell => spell.Name + "  " + spell.Cost + " MP",
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
                item => item.NameWithStats,
                (ItemInstance item, out Sprite sprite) => UiAssetResolver.TryGetItemSprite(item, out sprite),
                ResolveHeroItem);
        }

        private delegate bool TryGetSpriteDelegate<T>(T value, out Sprite sprite);

        private void DrawIconList<T>(
            Rect panelRect,
            float scale,
            string title,
            IList<T> values,
            Func<T, string> getLabel,
            TryGetSpriteDelegate<T> getSprite,
            Action<T> onSelect)
        {
            var rowHeight = 38f * scale;
            var menuWidth = Mathf.Min(360f * scale, panelRect.width - 28f * scale);
            var x = panelRect.x + 14f * scale;
            var y = panelRect.y + 82f * scale;
            GUI.Label(new Rect(x, y - 30f * scale, menuWidth, 24f * scale), title, titleStyle);

            for (var i = 0; i < values.Count; i++)
            {
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), menuWidth, rowHeight);
                if (GUI.Button(rect, GUIContent.none, GetMenuButtonStyle(i == selectedMenuIndex)))
                {
                    selectedMenuIndex = i;
                    onSelect(values[i]);
                }

                Sprite sprite;
                if (getSprite(values[i], out sprite) && sprite != null && sprite.texture != null)
                {
                    DrawSprite(sprite, new Rect(rect.x + 6f * scale, rect.y + 3f * scale, 32f * scale, 32f * scale));
                }

                GUI.Label(
                    new Rect(rect.x + 44f * scale, rect.y, rect.width - 50f * scale, rect.height),
                    getLabel(values[i]),
                    labelStyle);
            }

            var backRect = new Rect(x, panelRect.yMax - 40f * scale, 112f * scale, 30f * scale);
            if (GUI.Button(backRect, "Back", buttonStyle))
            {
                ReturnToActionMenu();
            }
        }

        private void DrawMenuButtons(Rect panelRect, float scale, string title, IList<CombatButton> buttons)
        {
            var menuWidth = 240f * scale;
            var rowHeight = 32f * scale;
            var x = panelRect.x + 14f * scale;
            var y = panelRect.y + 82f * scale;
            GUI.Label(new Rect(x, y - 30f * scale, menuWidth, 24f * scale), title, titleStyle);
            for (var i = 0; i < buttons.Count; i++)
            {
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), menuWidth, rowHeight);
                if (GUI.Button(rect, buttons[i].Label, GetMenuButtonStyle(i == selectedMenuIndex)))
                {
                    selectedMenuIndex = i;
                    buttons[i].Action();
                }
            }
        }

        private IEnumerable<Spell> GetAvailableEncounterSpells(Hero hero)
        {
            return hero == null ||
                   GameDataCache.Current == null ||
                   GameDataCache.Current.Spells == null
                ? Enumerable.Empty<Spell>()
                : hero.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => spell != null && spell.IsEncounterSpell && spell.Cost <= hero.Magic);
        }

        private IEnumerable<Skill> GetAvailableEncounterSkills(Hero hero)
        {
            return hero == null ||
                   GameDataCache.Current == null ||
                   GameDataCache.Current.Skills == null
                ? Enumerable.Empty<Skill>()
                : hero.GetSkills(GameDataCache.Current.Skills)
                    .Where(skill => skill != null && skill.IsEncounterSkill);
        }

        private IEnumerable<ItemInstance> GetAvailableEncounterItems(Hero hero)
        {
            return hero == null || hero.Items == null
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
                if (GUI.Button(rect, buttonList[i].Label, buttonStyle))
                {
                    buttonList[i].Action();
                }
            }
        }

        private void DrawTargetButtons(Rect panelRect, float scale)
        {
            var targets = targetSelectionCandidates == null
                ? new List<IFighter>()
                : targetSelectionCandidates.ToList();
            var buttonWidth = 126f * scale;
            var buttonHeight = 32f * scale;
            var gap = 8f * scale;
            var totalWidth = targets.Count * buttonWidth + Math.Max(0, targets.Count - 1) * gap;
            var startX = panelRect.x + (panelRect.width - totalWidth) / 2f;
            var y = panelRect.yMax - buttonHeight - 16f * scale;
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var rect = new Rect(startX + i * (buttonWidth + gap), y, buttonWidth, buttonHeight);
                if (GUI.Button(rect, target.Name, GetMenuButtonStyle(i == selectedMenuIndex)))
                {
                    selectedMenuIndex = i;
                    ActivateTargetSelection(i);
                }
            }
        }

        private void ActivateTargetSelection(int index)
        {
            if (targetSelectionCandidates == null || index < 0 || index >= targetSelectionCandidates.Count)
            {
                return;
            }

            var target = targetSelectionCandidates[index];
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
            IsOpen = false;
            GameState.AutoSaveBlocked = false;
        }

        private void EnsureStyles()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (uiTheme != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            labelStyle = new GUIStyle(uiTheme.LabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            titleStyle = new GUIStyle(uiTheme.TitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f * scale)
            };
            buttonStyle = uiTheme.ButtonStyle;
        }

        private GUIStyle GetMenuButtonStyle(bool selected)
        {
            if (uiTheme == null)
            {
                return buttonStyle;
            }

            return selected ? uiTheme.SelectedTabStyle : uiTheme.ButtonStyle;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }

        private static Texture2D LoadMonsterTexture(Monster monster)
        {
            EnsureMonsterImagePaths();
            string assetPath;
            return monster != null &&
                   MonsterImagePaths.TryGetValue(monster.ImageId, out assetPath)
                ? LoadTexture(assetPath)
                : null;
        }

        private static void EnsureMonsterImagePaths()
        {
            if (MonsterImagePaths.Count > 0)
            {
                return;
            }

            var fullPath = UnityAssetPath.ToRuntimePath(MonsterTilesetAssetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Monster tileset not found: " + MonsterTilesetAssetPath);
                return;
            }

            var document = XDocument.Parse(File.ReadAllText(fullPath));
            var root = document.Root;
            if (root == null)
            {
                return;
            }

            foreach (var tile in root.Elements("tile"))
            {
                var image = tile.Element("image");
                if (image == null)
                {
                    continue;
                }

                int id;
                var idAttribute = tile.Attribute("id");
                var sourceAttribute = image.Attribute("source");
                if (idAttribute == null ||
                    sourceAttribute == null ||
                    !int.TryParse(idAttribute.Value, out id))
                {
                    continue;
                }

                MonsterImagePaths[id] = ResolveImageAssetPath(sourceAttribute.Value);
            }
        }

        private static string ResolveImageAssetPath(string source)
        {
            var normalized = source.Replace('\\', '/');
            while (normalized.StartsWith("../", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(3);
            }

            const string imagesPrefix = "Images/";
            if (normalized.StartsWith(imagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(imagesPrefix.Length);
            }

            return "Assets/DungeonEscape/Images/" + normalized;
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            Texture2D texture;
            if (Textures.TryGetValue(assetPath, out texture))
            {
                return texture;
            }

            var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
            if (!File.Exists(fullPath))
            {
                fullPath = FindCaseInsensitiveFile(fullPath);
            }

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                Debug.LogWarning("Combat image not found: " + assetPath);
                Textures[assetPath] = null;
                return null;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning("Could not load Combat image: " + assetPath);
                Textures[assetPath] = null;
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(assetPath);
            Textures[assetPath] = texture;
            return texture;
        }

        private static string FindCaseInsensitiveFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName) || !Directory.Exists(directory))
            {
                return path;
            }

            return Directory.GetFiles(directory)
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase)) ?? path;
        }

        private static void DrawTextureAtNativeCombatSize(Texture2D texture, Rect rect, float scale)
        {
            var width = texture.width * scale;
            var height = texture.height * scale;
            var maxWidth = rect.width;
            var maxHeight = rect.height;
            var shrink = Mathf.Min(1f, Mathf.Min(maxWidth / width, maxHeight / height));
            width *= shrink;
            height *= shrink;
            var drawRect = new Rect(
                rect.x + (rect.width - width) / 2f,
                rect.y + rect.height - height,
                width,
                height);
            GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
        }

        private static void DrawSprite(Sprite sprite, Rect rect)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var drawRect = FitRect(sprite.rect.width, sprite.rect.height, rect);
            var texCoords = new Rect(
                sprite.rect.x / sprite.texture.width,
                sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width,
                sprite.rect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, texCoords);
        }

        private static Rect FitRect(float sourceWidth, float sourceHeight, Rect target)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f)
            {
                return target;
            }

            var scale = Mathf.Min(target.width / sourceWidth, target.height / sourceHeight);
            var width = sourceWidth * scale;
            var height = sourceHeight * scale;
            return new Rect(
                target.x + (target.width - width) / 2f,
                target.y + (target.height - height) / 2f,
                width,
                height);
        }

        private void DrawHealthBar(int currentHealth, int maxHealth, Rect rect)
        {
            GUI.Box(rect, GUIContent.none, buttonStyle);
            var previousColor = GUI.color;
            GUI.color = Color.white;
            var inset = Mathf.Max(1f, uiTheme.BorderThickness);
            var progress = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
            GUI.DrawTexture(
                new Rect(
                    rect.x + inset,
                    rect.y + inset,
                    Mathf.Max(0f, rect.width - inset * 2f) * progress,
                    Mathf.Max(0f, rect.height - inset * 2f)),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static string GetBackgroundAssetPath(Biome biome)
        {
            switch (biome)
            {
                case Biome.Water:
                    return "Assets/DungeonEscape/Images/background/ocean.png";
                case Biome.Hills:
                    return "Assets/DungeonEscape/Images/background/mountain.png";
                case Biome.Desert:
                    return "Assets/DungeonEscape/Images/background/desert.png";
                case Biome.Swamp:
                    return "Assets/DungeonEscape/Images/background/swamp.png";
                case Biome.Cave:
                    return "Assets/DungeonEscape/Images/background/cave.png";
                case Biome.Town:
                    return "Assets/DungeonEscape/Images/background/castle.png";
                case Biome.Tower:
                    return "Assets/DungeonEscape/Images/background/tower.png";
                case Biome.Grassland:
                case Biome.Forest:
                case Biome.None:
                default:
                    return "Assets/DungeonEscape/Images/background/field.png";
            }
        }

        private sealed class CombatButton
        {
            public CombatButton(string label, Action action)
            {
                Label = label;
                Action = action;
            }

            public string Label { get; private set; }
            public Action Action { get; private set; }
        }
    }
}
