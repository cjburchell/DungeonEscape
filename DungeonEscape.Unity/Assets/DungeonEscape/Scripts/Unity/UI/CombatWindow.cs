using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
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
    public sealed class CombatWindow : MonoBehaviour
    {
        private const int WindowDepth = -2500;
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;
        private const float DamageFlashDuration = 0.65f;
        private const float DamageFlashInterval = 0.08f;
        private const string EndFightSong = "not-in-vain";
        private const string TitleSong = "first-story";
        private const string MonsterTilesetAssetPath = "Assets/DungeonEscape/Tilesets/allmonsters.tsx";
        private static readonly Dictionary<int, string> MonsterImagePaths = new Dictionary<int, string>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> DamageFlashEndTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> HealFlashEndTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<IFighter, float> DefeatedFighterVisibleEndTimes = new Dictionary<IFighter, float>();
        private static readonly System.Random CombatRandom = new System.Random();

        private enum CombatState
        {
            Message,
            ChooseAction,
            ChooseTarget,
            ChooseSpell,
            ChooseItem
        }

        private enum RoundActionState
        {
            Run,
            Fight,
            Spell,
            Item,
            Nothing,
            Skill
        }

        private sealed class CombatMonster
        {
            public Monster Data { get; set; }
            public MonsterInstance Instance { get; set; }
        }

        private sealed class RoundAction
        {
            public IFighter Source { get; set; }
            public RoundActionState State { get; set; }
            public Spell Spell { get; set; }
            public ItemInstance Item { get; set; }
            public Skill Skill { get; set; }
            public List<IFighter> Targets { get; set; }
        }

        private sealed class HeroCombatSelection
        {
            public string ActionLabel { get; set; }
            public string SpellName { get; set; }
            public string ItemId { get; set; }
            public string ItemName { get; set; }
            public string TargetName { get; set; }
        }

        private readonly List<CombatMonster> monsters = new List<CombatMonster>();
        private readonly List<RoundAction> roundActions = new List<RoundAction>();
        private readonly List<Hero> pendingHeroes = new List<Hero>();
        private readonly Dictionary<string, HeroCombatSelection> heroSelections =
            new Dictionary<string, HeroCombatSelection>(StringComparer.OrdinalIgnoreCase);
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
        private int visibleMessageCharacters;
        private float revealCharacterAccumulator;
        private Vector2 messageScrollPosition;
        private int repeatingMenuMoveY;
        private float nextMenuMoveYTime;
        private int acceptMenuInteractAfterFrame;
        private bool waitForMenuInteractRelease;
        private int round;

        public static bool IsOpen { get; private set; }

        public static bool IsFighterFlashing(string fighterName)
        {
            return IsFighterDamageFlashing(fighterName);
        }

        public static bool IsFighterDamageFlashing(string fighterName)
        {
            if (!IsFighterDamageFlashActive(fighterName))
            {
                return false;
            }

            float endTime;
            DamageFlashEndTimes.TryGetValue(fighterName, out endTime);
            var elapsed = DamageFlashDuration - (endTime - Time.unscaledTime);
            return Mathf.FloorToInt(elapsed / DamageFlashInterval) % 2 == 0;
        }

        private static bool IsFighterDamageFlashActive(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName))
            {
                return false;
            }

            float endTime;
            if (!DamageFlashEndTimes.TryGetValue(fighterName, out endTime))
            {
                return false;
            }

            if (Time.unscaledTime >= endTime)
            {
                DamageFlashEndTimes.Remove(fighterName);
                return false;
            }

            return true;
        }

        public static bool IsFighterHealFlashing(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName))
            {
                return false;
            }

            float endTime;
            if (!HealFlashEndTimes.TryGetValue(fighterName, out endTime))
            {
                return false;
            }

            if (Time.unscaledTime >= endTime)
            {
                HealFlashEndTimes.Remove(fighterName);
                return false;
            }

            var elapsed = DamageFlashDuration - (endTime - Time.unscaledTime);
            return Mathf.FloorToInt(elapsed / DamageFlashInterval) % 2 == 0;
        }

        public static void Open(IEnumerable<Monster> encounterMonsters, Biome encounterBiome)
        {
            var window = FindAnyObjectByType<CombatWindow>();
            if (window == null)
            {
                window = new GameObject("CombatWindow").AddComponent<CombatWindow>();
            }

            window.monsters.Clear();
            window.roundActions.Clear();
            window.pendingHeroes.Clear();
            window.heroSelections.Clear();
            DamageFlashEndTimes.Clear();
            HealFlashEndTimes.Clear();
            DefeatedFighterVisibleEndTimes.Clear();
            window.targetSelectionCandidates.Clear();
            window.targetSelectionDone = null;
            window.gameState = GameState.GetOrCreate();
            Audio.GetOrCreate().PlayCombatMusic();
            if (encounterMonsters != null)
            {
                window.CreateMonsterInstances(encounterMonsters.Where(monster => monster != null));
            }

            window.biome = encounterBiome;
            window.round = 0;
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
                AdvanceTextReveal();
                if (InputManager.GetCommandDown(InputCommand.Interact) ||
                    InputManager.GetCommandDown(InputCommand.Cancel))
                {
                    UiControls.PlayConfirmSound();
                    ContinueMessage();
                }

                return;
            }

            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
                ReturnToActionMenu();
                return;
            }

            var moveY = GetCombatMenuMoveY();
            if (moveY != 0)
            {
                MoveSelection(moveY);
            }

            if (CanAcceptMenuInteract() && InputManager.GetCommandDown(InputCommand.Interact))
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
                if (monster.Instance == null || monster.Instance.RanAway)
                {
                    continue;
                }

                var damageFlashActive = IsFighterDamageFlashActive(monster.Instance.Name);
                if (monster.Instance.IsDead && !IsDefeatedFighterVisible(monster.Instance))
                {
                    continue;
                }

                var texture = LoadMonsterTexture(monster.Data);
                var slotRect = new Rect(startX + i * (slotWidth + gap), y, slotWidth, slotHeight);
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
            messageScrollPosition = Vector2.zero;
            StartTextReveal();
        }

        private void ContinueMessage()
        {
            if (state != CombatState.Message)
            {
                return;
            }

            if (!IsTextFullyRevealed)
            {
                UiControls.PlayConfirmSound();
                FinishTextReveal();
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

        private bool IsTextFullyRevealed
        {
            get { return string.IsNullOrEmpty(messageText) || visibleMessageCharacters >= messageText.Length; }
        }

        private string DisplayedMessage
        {
            get
            {
                if (string.IsNullOrEmpty(messageText) || IsTextFullyRevealed)
                {
                    return messageText;
                }

                return messageText.Substring(0, Mathf.Clamp(visibleMessageCharacters, 0, messageText.Length));
            }
        }

        private void StartTextReveal()
        {
            revealCharacterAccumulator = 0f;
            visibleMessageCharacters = GetTextRevealSpeed() <= 0f || string.IsNullOrEmpty(messageText)
                ? string.IsNullOrEmpty(messageText) ? 0 : messageText.Length
                : 0;
        }

        private void AdvanceTextReveal()
        {
            if (IsTextFullyRevealed)
            {
                return;
            }

            var speed = GetTextRevealSpeed();
            if (speed <= 0f)
            {
                FinishTextReveal();
                return;
            }

            revealCharacterAccumulator += speed * Time.unscaledDeltaTime;
            var charactersToAdd = Mathf.FloorToInt(revealCharacterAccumulator);
            if (charactersToAdd <= 0)
            {
                return;
            }

            revealCharacterAccumulator -= charactersToAdd;
            visibleMessageCharacters = Mathf.Min(messageText.Length, visibleMessageCharacters + charactersToAdd);
        }

        private void FinishTextReveal()
        {
            visibleMessageCharacters = string.IsNullOrEmpty(messageText) ? 0 : messageText.Length;
            revealCharacterAccumulator = 0f;
        }

        private static float GetTextRevealSpeed()
        {
            var settings = SettingsCache.Current;
            return settings == null ? 60f : settings.DialogTextCharactersPerSecond;
        }

        private void MoveSelection(int moveY)
        {
            var count = GetCurrentSelectionCount();
            if (count <= 0)
            {
                selectedMenuIndex = 0;
                return;
            }

            var previousIndex = selectedMenuIndex;
            selectedMenuIndex = Mathf.Clamp(selectedMenuIndex + (moveY > 0 ? 1 : -1), 0, count - 1);
            if (selectedMenuIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
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
                        UiControls.PlayConfirmSound();
                        RememberAction(actions[selectedMenuIndex].Label);
                        actions[selectedMenuIndex].Action();
                    }

                    return;
                case CombatState.ChooseSpell:
                    var spells = actingHero == null ? new List<Spell>() : GetAvailableEncounterSpells(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < spells.Count)
                    {
                        UiControls.PlayConfirmSound();
                        ResolveHeroSpell(spells[selectedMenuIndex]);
                    }

                    return;
                case CombatState.ChooseItem:
                    var items = actingHero == null ? new List<ItemInstance>() : GetAvailableEncounterItems(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < items.Count)
                    {
                        UiControls.PlayConfirmSound();
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
            selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
            messageText = actingHero == null ? "Choose an action." : actingHero.Name + "'s turn.";
        }

        private void BeginRound()
        {
            round++;
            actingHero = null;
            roundActions.Clear();
            pendingHeroes.Clear();

            var party = gameState == null ? null : gameState.Party;
            if (party != null)
            {
                pendingHeroes.AddRange(party.AliveMembers.Where(CanBeAttacked));
            }

            foreach (var monster in AliveMonsters())
            {
                roundActions.Add(ChooseMonsterAction(monster.Instance));
            }

            ChooseNextHeroAction();
        }

        private void ChooseNextHeroAction()
        {
            if (!AliveHeroes().Any())
            {
                ShowDefeatMessage();
                return;
            }

            if (!AliveMonsters().Any())
            {
                ShowVictoryMessage();
                return;
            }

            while (pendingHeroes.Count > 0)
            {
                actingHero = pendingHeroes[0];
                pendingHeroes.RemoveAt(0);
                if (!CanBeAttacked(actingHero))
                {
                    continue;
                }

                if (actingHero.Status.Any(effect => effect.Type == EffectType.Sleep))
                {
                    QueueHeroAction(new RoundAction
                    {
                        Source = actingHero,
                        State = RoundActionState.Nothing
                    });
                    continue;
                }

                if (actingHero.Status.Any(effect => effect.Type == EffectType.Confusion))
                {
                    var confusedTargets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>()
                        .Concat(AliveHeroes().Cast<IFighter>())
                        .Where(target => target != actingHero)
                        .ToList();
                    if (confusedTargets.Count > 0)
                    {
                        QueueHeroAction(new RoundAction
                        {
                            Source = actingHero,
                            State = RoundActionState.Fight,
                            Targets = new List<IFighter> { confusedTargets[CombatRandom.Next(confusedTargets.Count)] }
                        });
                        continue;
                    }
                }

                state = CombatState.ChooseAction;
                selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
                messageText = actingHero.Name + "'s action.";
                return;
            }

            ResolveNextRoundAction();
        }

        private void QueueHeroAction(RoundAction action)
        {
            if (action != null)
            {
                roundActions.Add(action);
            }

            actingHero = null;
            ChooseNextHeroAction();
        }

        private void ResolveNextRoundAction()
        {
            if (!AliveHeroes().Any())
            {
                ShowDefeatMessage();
                return;
            }

            if (!AliveMonsters().Any())
            {
                ShowVictoryMessage();
                return;
            }

            var action = roundActions
                .Where(IsActionResolvable)
                .OrderByDescending(item => item.Source.Agility)
                .FirstOrDefault();
            if (action == null)
            {
                EndRound();
                return;
            }

            roundActions.Remove(action);
            bool endFight;
            var message = ExecuteRoundAction(action, out endFight);
            ShowMessage(message, endFight ? (Action)null : ResolveNextRoundAction);
        }

        private void EndRound()
        {
            if (AliveHeroes().Any() && AliveMonsters().Any())
            {
                BeginRound();
                return;
            }

            ResolveNextRoundAction();
        }

        private void BeginTargetSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            BeginTargetSelection(
                "Choose a target for " + actingHero.Name + ".",
                AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList(),
                Target.Single,
                1,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Fight,
                    Targets = targets
                }));
        }

        private void BeginSpellSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            var spells = GetAvailableEncounterSpells(actingHero).ToList();
            if (spells.Count == 0)
            {
                ShowMessage(actingHero.Name + " cannot cast any combat spells.", ChooseNextHeroAction);
                return;
            }

            state = CombatState.ChooseSpell;
            selectedMenuIndex = GetRememberedSpellIndex(spells);
            messageText = "Choose a spell for " + actingHero.Name + ".";
            BlockMenuInteractUntilRelease();
        }

        private void BeginItemSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            var items = GetAvailableEncounterItems(actingHero).ToList();
            if (items.Count == 0)
            {
                ShowMessage(actingHero.Name + " has no combat items.", ChooseNextHeroAction);
                return;
            }

            state = CombatState.ChooseItem;
            selectedMenuIndex = GetRememberedItemIndex(items);
            messageText = "Choose an item for " + actingHero.Name + ".";
            BlockMenuInteractUntilRelease();
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
                RememberTarget(candidates[0]);
                done(new List<IFighter> { candidates[0] });
                return;
            }

            targetSelectionTitle = title;
            targetSelectionCandidates = candidates;
            targetSelectionDone = done;
            state = CombatState.ChooseTarget;
            selectedMenuIndex = GetRememberedTargetIndex(candidates);
            messageText = title;
            BlockMenuInteractUntilRelease();
        }

        private void ResolveHeroSpell(Spell spell)
        {
            if (actingHero == null || actingHero.IsDead || spell == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Spell");
            RememberSpell(spell);
            spell.Setup(GameDataCache.Current == null ? null : GameDataCache.Current.Skills);
            var candidates = spell.IsAttackSpell
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySpellTargets(spell);
            BeginTargetSelection(
                "Choose a target for " + spell.Name + ".",
                candidates,
                spell.Targets,
                spell.MaxTargets,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Spell,
                    Spell = spell,
                    Targets = targets
                }),
                spell.Type == SkillType.Revive);
        }

        private void ResolveHeroSkill(Skill skill)
        {
            if (actingHero == null || actingHero.IsDead || skill == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction(skill.Name);
            var candidates = skill.IsAttackSkill
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySkillTargets(skill);
            BeginTargetSelection(
                "Choose a target for " + skill.Name + ".",
                candidates,
                skill.Targets,
                skill.MaxTargets,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Skill,
                    Skill = skill,
                    Targets = targets
                }),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroItem(ItemInstance item)
        {
            if (actingHero == null || actingHero.IsDead || item == null || item.Item == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Item");
            RememberItem(item);
            EnsureItemLinked(item);
            var skill = item.Item.Skill;
            if (skill == null)
            {
                ShowMessage(item.Name + " cannot be used in combat.", ChooseNextHeroAction);
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
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Item,
                    Item = item,
                    Targets = targets
                }),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroRun()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Run");
            QueueHeroAction(new RoundAction
            {
                Source = actingHero,
                State = RoundActionState.Run,
                Targets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
            });
        }

        private RoundAction ChooseMonsterAction(IFighter monster)
        {
            if (monster == null)
            {
                return null;
            }

            if (monster.Status.Any(effect => effect.Type == EffectType.Sleep))
            {
                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Nothing
                };
            }

            var availableTargets = AliveHeroes().Cast<IFighter>().Where(CanBeAttacked).ToList();
            if (monster.Status.Any(effect => effect.Type == EffectType.Confusion))
            {
                availableTargets.AddRange(AliveMonsters().Select(item => item.Instance).Where(CanBeAttacked));
                availableTargets.Remove(monster);
            }

            if (availableTargets.Count == 0)
            {
                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Nothing
                };
            }

            var availableSpells = GameDataCache.Current == null || GameDataCache.Current.Spells == null
                ? new List<Spell>()
                : monster.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => spell != null && spell.IsEncounterSpell && spell.Cost <= monster.Magic)
                    .ToList();
            if (!monster.Status.Any(effect => effect.Type == EffectType.StopSpell) && availableSpells.Count > 0)
            {
                var lowHealthHealSpells = availableSpells
                    .Where(spell => spell.Type == SkillType.Heal && monster.MaxHealth > 0 && (float)monster.Health / monster.MaxHealth < 0.1f)
                    .ToList();
                if (lowHealthHealSpells.Count > 0)
                {
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Spell,
                        Spell = lowHealthHealSpells[CombatRandom.Next(lowHealthHealSpells.Count)],
                        Targets = new List<IFighter> { monster }
                    };
                }

                var attackSpells = availableSpells.Where(spell => spell.IsAttackSpell).ToList();
                if (attackSpells.Count > 0)
                {
                    var spell = attackSpells[CombatRandom.Next(attackSpells.Count)];
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Spell,
                        Spell = spell,
                        Targets = GetTargets(spell.Targets, spell.MaxTargets, availableTargets)
                    };
                }
            }

            var availableSkills = GameDataCache.Current == null || GameDataCache.Current.Skills == null
                ? new List<Skill>()
                : monster.GetSkills(GameDataCache.Current.Skills).Where(skill => skill != null && skill.IsEncounterSkill).ToList();
            if (availableSkills.Count > 0 && Dice.RollD100() > 75)
            {
                var skill = availableSkills[CombatRandom.Next(availableSkills.Count)];
                if (skill.Type == SkillType.Flee)
                {
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Run,
                        Targets = availableTargets
                    };
                }

                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Skill,
                    Skill = skill,
                    Targets = GetTargets(skill.Targets, skill.MaxTargets, availableTargets)
                };
            }

            return new RoundAction
            {
                Source = monster,
                State = RoundActionState.Fight,
                Targets = new List<IFighter> { ChooseFighter(availableTargets) }
            };
        }

        private bool IsActionResolvable(RoundAction action)
        {
            if (action == null || action.Source == null || !CanBeAttacked(action.Source))
            {
                return false;
            }

            if (action.State == RoundActionState.Nothing || action.Targets == null)
            {
                return true;
            }

            if (IsReviveAction(action))
            {
                return action.Targets.Any(target => target != null && target.IsDead);
            }

            if (IsOffensiveAction(action))
            {
                return ResolveActionTargets(action).Any();
            }

            return action.Targets.Any(CanBeAttacked);
        }

        private string ExecuteRoundAction(RoundAction action, out bool endFight)
        {
            endFight = false;
            if (action == null || action.Source == null)
            {
                return "";
            }

            var message = action.Source.CheckForExpiredStates(round, DurationType.Rounds);
            message += action.Source.UpdateStatusEffects(gameState);
            if (action.Source.IsDead)
            {
                return string.IsNullOrEmpty(message) ? action.Source.Name + " cannot act." : message.TrimEnd();
            }

            switch (action.State)
            {
                case RoundActionState.Run:
                    string runMessage;
                    bool didEndFight;
                    Run(action.Source, action.Targets, out runMessage, out didEndFight);
                    endFight = didEndFight;
                    message += runMessage;
                    break;
                case RoundActionState.Fight:
                    message += Fight(action.Source, ResolveActionTargets(action).FirstOrDefault());
                    break;
                case RoundActionState.Spell:
                    message += CastSpell(action.Spell, ResolveActionTargets(action), action.Source);
                    break;
                case RoundActionState.Item:
                    message += UseItem(action.Item, ResolveActionTargets(action), action.Source);
                    break;
                case RoundActionState.Nothing:
                    message += action.Source.Name + " doesn't do anything.";
                    break;
                case RoundActionState.Skill:
                    message += DoSkill(action.Skill, ResolveActionTargets(action), action.Source);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return string.IsNullOrEmpty(message) ? "" : message.TrimEnd();
        }

        private void Run(IFighter source, IEnumerable<IFighter> targets, out string message, out bool endFight)
        {
            message = source.Name + " tried to run.\n";
            endFight = false;
            var fastestTarget = targets == null ? null : targets.Where(CanBeAttacked).OrderByDescending(target => target.Agility).FirstOrDefault();
            if (fastestTarget != null && !source.CanHit(fastestTarget))
            {
                message += "But could not get away.";
                return;
            }

            Audio.GetOrCreate().PlaySoundEffect("stairs-up");
            message += "And got away.";
            if (source is Hero)
            {
                Audio.GetOrCreate().PlayMusic(EndFightSong);
                endFight = true;
                return;
            }

            var fighter = source as Fighter;
            if (fighter != null)
            {
                fighter.RanAway = true;
            }
        }

        private List<IFighter> ResolveActionTargets(RoundAction action)
        {
            if (action == null)
            {
                return new List<IFighter>();
            }

            if (IsReviveAction(action))
            {
                return action.Targets == null
                    ? new List<IFighter>()
                    : action.Targets.Where(target => target != null && target.IsDead).ToList();
            }

            var targets = action.Targets == null
                ? new List<IFighter>()
                : action.Targets.Where(CanBeAttacked).ToList();
            if (targets.Count > 0 || !IsOffensiveAction(action))
            {
                return targets;
            }

            return GetFallbackTargets(action);
        }

        private List<IFighter> GetOpposingTargets(IFighter source)
        {
            if (source is Hero)
            {
                return AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().Where(CanBeAttacked).ToList();
            }

            return AliveHeroes().Cast<IFighter>().Where(CanBeAttacked).ToList();
        }

        private List<IFighter> GetFallbackTargets(RoundAction action)
        {
            var candidates = GetOpposingTargets(action.Source);
            if (candidates.Count == 0)
            {
                return candidates;
            }

            var targetMode = GetActionTargetMode(action);
            var maxTargets = GetActionMaxTargets(action);
            if (targetMode == Target.Group)
            {
                return maxTargets > 0 ? candidates.Take(maxTargets).ToList() : candidates;
            }

            return new List<IFighter> { candidates[0] };
        }

        private static Target GetActionTargetMode(RoundAction action)
        {
            switch (action.State)
            {
                case RoundActionState.Spell:
                    return action.Spell == null ? Target.Single : action.Spell.Targets;
                case RoundActionState.Skill:
                    return action.Skill == null ? Target.Single : action.Skill.Targets;
                case RoundActionState.Item:
                    return action.Item == null ? Target.Single : action.Item.Target;
                default:
                    return Target.Single;
            }
        }

        private static int GetActionMaxTargets(RoundAction action)
        {
            switch (action.State)
            {
                case RoundActionState.Spell:
                    return action.Spell == null ? 1 : action.Spell.MaxTargets;
                case RoundActionState.Skill:
                    return action.Skill == null ? 1 : action.Skill.MaxTargets;
                case RoundActionState.Item:
                    return action.Item == null || action.Item.Item == null || action.Item.Item.Skill == null
                        ? 1
                        : action.Item.Item.Skill.MaxTargets;
                default:
                    return 1;
            }
        }

        private static bool IsReviveAction(RoundAction action)
        {
            return action != null &&
                   ((action.State == RoundActionState.Spell &&
                     action.Spell != null &&
                     action.Spell.Type == SkillType.Revive) ||
                    (action.State == RoundActionState.Skill &&
                     action.Skill != null &&
                     action.Skill.Type == SkillType.Revive) ||
                    (action.State == RoundActionState.Item &&
                     action.Item != null &&
                     action.Item.Item != null &&
                     action.Item.Item.Skill != null &&
                     action.Item.Item.Skill.Type == SkillType.Revive));
        }

        private static bool IsOffensiveAction(RoundAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.State)
            {
                case RoundActionState.Fight:
                    return true;
                case RoundActionState.Spell:
                    return action.Spell != null && action.Spell.IsAttackSpell;
                case RoundActionState.Skill:
                    return action.Skill != null && (action.Skill.IsAttackSkill || action.Skill.DoAttack);
                case RoundActionState.Item:
                    return action.Item != null &&
                           action.Item.Item != null &&
                           action.Item.Item.Skill != null &&
                           (action.Item.Item.Skill.IsAttackSkill || action.Item.Item.Skill.DoAttack);
                default:
                    return false;
            }
        }

        private static IFighter ChooseFighter(IReadOnlyCollection<IFighter> availableTargets)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return null;
            }

            if (availableTargets.Count == 1)
            {
                return availableTargets.First();
            }

            var roll = Dice.RollD100();
            var totalHealth = Math.Max(1, availableTargets.Sum(target => Math.Max(1, target.MaxHealth)));
            var maxRoll = 0f;
            foreach (var target in availableTargets.OrderByDescending(target => target.MaxHealth))
            {
                maxRoll += (float)Math.Max(1, target.MaxHealth) / totalHealth * 100f;
                if (roll <= maxRoll)
                {
                    return target;
                }
            }

            return availableTargets.First();
        }

        private static List<IFighter> GetTargets(Target targetType, int maxTargets, IReadOnlyCollection<IFighter> availableTargets)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return new List<IFighter>();
            }

            if (targetType != Target.Group)
            {
                return new List<IFighter> { ChooseFighter(availableTargets) };
            }

            if (maxTargets == 0)
            {
                return availableTargets.ToList();
            }

            var targets = new List<IFighter>();
            for (var i = 0; i < maxTargets; i++)
            {
                var target = ChooseFighter(availableTargets);
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
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

        private string CastSpell(Spell spell, List<IFighter> targets, IFighter caster)
        {
            if (caster == null || spell == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect("spell", true);
            var startingHealth = CaptureTargetHealth(targets);
            var message = spell.Cast(targets ?? new List<IFighter>(), new BaseState[0], caster, gameState, round);
            FlashHealthChangedTargets(startingHealth, targets);
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        private string DoSkill(Skill skill, List<IFighter> targets, IFighter source)
        {
            if (source == null || skill == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect(skill.IsAttackSkill || skill.DoAttack ? "prepare-attack" : "spell", true);
            var message = source.Name + " uses " + skill.Name + ".\n";
            var hit = false;
            var selectedTargets = targets ?? new List<IFighter>();
            if (selectedTargets.Count == 0)
            {
                var result = skill.Do(null, source, null, gameState, round);
                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }
            }

            foreach (var target in selectedTargets)
            {
                if (target == null)
                {
                    continue;
                }

                if (skill.DoAttack || skill.Type == SkillType.Attack)
                {
                    int damage;
                    message += Fight(source, target, out damage, false) + "\n";
                    if (damage != 0)
                    {
                        hit = true;
                    }
                }

                if (target.IsDead)
                {
                    continue;
                }

                if (skill.IsAttackSkill)
                {
                    if (skill.Type == SkillType.Attack && !skill.DoAttack)
                    {
                        message += source.Name + " attacks " + target.Name + " with " + skill.EffectName + ".\n";
                    }

                    if (!source.CanHit(target))
                    {
                        if (!skill.DoAttack)
                        {
                            message += target.Name + " dodges the " + skill.EffectName + "\n";
                        }

                        continue;
                    }
                }

                var startingHealth = target.Health;
                var result = skill.Do(target, source, null, gameState, round);
                if (target.Health < startingHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target.Health > startingHealth)
                {
                    StartHealFlash(target);
                }

                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }

                if (skill.IsAttackSkill && result.Item2)
                {
                    hit = true;
                }
            }

            if (skill.IsAttackSkill || skill.DoAttack)
            {
                Audio.GetOrCreate().PlaySoundEffect(hit ? "receive-damage" : "miss");
            }

            return message.TrimEnd();
        }

        private string UseItem(ItemInstance item, List<IFighter> targets, IFighter source)
        {
            if (source == null || item == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect("confirm");
            var message = "";
            var worked = false;
            var selectedTargets = targets == null || targets.Count == 0
                ? new List<IFighter> { source }
                : targets;
            foreach (var target in selectedTargets)
            {
                var startingHealth = target == null ? 0 : target.Health;
                var result = item.Use(source, target, null, gameState, round);
                if (target != null && target.Health < startingHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target != null && target.Health > startingHealth)
                {
                    StartHealFlash(target);
                }

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
                source.Items.Remove(item);
            }
            else if (!item.HasCharges)
            {
                item.UnEquip(gameState.Party.Members);
                source.Items.Remove(item);
                message += item.Name + " has been destroyed.\n";
            }

            return string.IsNullOrEmpty(message) ? source.Name + " used " + item.Name + "." : message.TrimEnd();
        }

        private static Dictionary<string, int> CaptureTargetHealth(IEnumerable<IFighter> targets)
        {
            var health = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (targets == null)
            {
                return health;
            }

            foreach (var target in targets)
            {
                if (target != null && !string.IsNullOrEmpty(target.Name))
                {
                    health[target.Name] = target.Health;
                }
            }

            return health;
        }

        private static void FlashHealthChangedTargets(IDictionary<string, int> startingHealth, IEnumerable<IFighter> targets)
        {
            if (startingHealth == null || targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                int previousHealth;
                if (target != null &&
                    !string.IsNullOrEmpty(target.Name) &&
                    startingHealth.TryGetValue(target.Name, out previousHealth) &&
                    target.Health < previousHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target != null &&
                         !string.IsNullOrEmpty(target.Name) &&
                         startingHealth.TryGetValue(target.Name, out previousHealth) &&
                         target.Health > previousHealth)
                {
                    StartHealFlash(target);
                }
            }
        }

        private static void StartDamageFlash(IFighter target)
        {
            if (target == null || string.IsNullOrEmpty(target.Name))
            {
                return;
            }

            DamageFlashEndTimes[target.Name] = Time.unscaledTime + DamageFlashDuration;
            if (target.IsDead)
            {
                DefeatedFighterVisibleEndTimes[target] = Time.unscaledTime + DamageFlashDuration;
            }
        }

        private static bool IsDefeatedFighterVisible(IFighter target)
        {
            if (target == null)
            {
                return false;
            }

            float endTime;
            if (!DefeatedFighterVisibleEndTimes.TryGetValue(target, out endTime))
            {
                return false;
            }

            if (Time.unscaledTime >= endTime)
            {
                DefeatedFighterVisibleEndTimes.Remove(target);
                return false;
            }

            return true;
        }

        private static void StartHealFlash(IFighter target)
        {
            if (target == null || string.IsNullOrEmpty(target.Name))
            {
                return;
            }

            HealFlashEndTimes[target.Name] = Time.unscaledTime + DamageFlashDuration;
        }

        private string Fight(IFighter source, IFighter target)
        {
            int damage;
            return Fight(source, target, out damage, true);
        }

        private string Fight(IFighter source, IFighter target, out int damage, bool playSounds)
        {
            if (source == null || target == null)
            {
                damage = 0;
                return "";
            }

            if (playSounds)
            {
                Audio.GetOrCreate().PlaySoundEffect("prepare-attack", true);
            }

            var message = source.Name + " attacks " + target.Name + ".\n";
            damage = 0;
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
                StartDamageFlash(target);
                message += " took " + damage + " points of damage";
                message += "\n" + target.HitCheck().TrimEnd();
            }

            if (target.IsDead)
            {
                target.Health = 0;
                message += "\nand has died!";
            }

            if (playSounds)
            {
                Audio.GetOrCreate().PlaySoundEffect(damage == 0 ? "miss" : "receive-damage");
            }

            return message.TrimEnd();
        }

        private static int RandomAttack(int attack)
        {
            return attack <= 0 ? 0 : CombatRandom.Next(attack);
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

        private void ShowVictoryMessage()
        {
            Audio.GetOrCreate().PlaySoundEffect("victory", true);
            Audio.GetOrCreate().PlayMusic(EndFightSong);
            ShowMessage(GetVictoryMessage(), null);
        }

        private void ShowDefeatMessage()
        {
            Audio.GetOrCreate().PlaySoundEffect("receive-damage");
            ShowMessage("Everyone has died!", ReturnToTitleAfterDefeat);
        }

        private void ReturnToTitleAfterDefeat()
        {
            Close(false);
            Audio.GetOrCreate().PlayMusic(TitleSong);
            TitleMenu.OpenMainMenu();
        }

        private void DrawActionMenu(Rect panelRect, float scale)
        {
            var actions = BuildActionButtons().ToList();
            DrawMenuButtons(panelRect, scale, actingHero == null ? string.Empty : actingHero.Name, actions);
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
            var rowHeight = GetCombatMenuRowHeight(scale);
            var menuWidth = GetCombatMenuWidth(panelRect, scale);
            var x = panelRect.x + 14f * scale;
            var y = GetCombatMenuY(panelRect, scale);
            DrawCombatMenuTitle(x, panelRect.y, menuWidth, scale, title);
            for (var i = 0; i < values.Count; i++)
            {
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), menuWidth, rowHeight);
                var selected = i == selectedMenuIndex;
                if (GUI.Button(rect, GUIContent.none, GetCombatRowStyle(selected)))
                {
                    UiControls.PlayConfirmSound();
                    selectedMenuIndex = i;
                    onSelect(values[i]);
                }

                Sprite sprite;
                if (getSprite(values[i], out sprite) && sprite != null && sprite.texture != null)
                {
                    var iconSize = 26f * scale;
                    DrawSprite(sprite, new Rect(rect.x + 6f * scale, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize));
                }

                GUI.Label(
                    new Rect(rect.x + 40f * scale, rect.y, rect.width - 46f * scale, rect.height),
                    getLabel(values[i]),
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

        private HeroCombatSelection GetHeroSelection()
        {
            if (actingHero == null || string.IsNullOrEmpty(actingHero.Name))
            {
                return null;
            }

            HeroCombatSelection selection;
            if (!heroSelections.TryGetValue(actingHero.Name, out selection))
            {
                selection = new HeroCombatSelection();
                heroSelections[actingHero.Name] = selection;
            }

            return selection;
        }

        private void RememberAction(string label)
        {
            var selection = GetHeroSelection();
            if (selection != null && !string.IsNullOrEmpty(label))
            {
                selection.ActionLabel = label;
            }
        }

        private void RememberSpell(Spell spell)
        {
            var selection = GetHeroSelection();
            if (selection != null && spell != null)
            {
                selection.SpellName = spell.Name;
            }
        }

        private void RememberItem(ItemInstance item)
        {
            var selection = GetHeroSelection();
            if (selection != null && item != null)
            {
                selection.ItemId = item.Id;
                selection.ItemName = item.Name;
            }
        }

        private void RememberTarget(IFighter target)
        {
            var selection = GetHeroSelection();
            if (selection != null && target != null)
            {
                selection.TargetName = target.Name;
            }
        }

        private int GetRememberedActionIndex(IList<CombatButton> actions)
        {
            var selection = GetHeroSelection();
            return GetIndexOrDefault(actions, action => action.Label, selection == null ? null : selection.ActionLabel);
        }

        private int GetRememberedSpellIndex(IList<Spell> spells)
        {
            var selection = GetHeroSelection();
            return GetIndexOrDefault(spells, spell => spell.Name, selection == null ? null : selection.SpellName);
        }

        private int GetRememberedItemIndex(IList<ItemInstance> items)
        {
            var selection = GetHeroSelection();
            if (selection == null)
            {
                return 0;
            }

            var byId = GetIndexOrDefault(items, item => item.Id, selection.ItemId);
            return byId != 0 || string.IsNullOrEmpty(selection.ItemId)
                ? byId
                : GetIndexOrDefault(items, item => item.Name, selection.ItemName);
        }

        private int GetRememberedTargetIndex(IList<IFighter> targets)
        {
            var selection = GetHeroSelection();
            return GetIndexOrDefault(targets, target => target.Name, selection == null ? null : selection.TargetName);
        }

        private static int GetIndexOrDefault<T>(IList<T> values, Func<T, string> getKey, string rememberedKey)
        {
            if (values == null || values.Count == 0 || string.IsNullOrEmpty(rememberedKey))
            {
                return 0;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value != null && string.Equals(getKey(value), rememberedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return 0;
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

        private void DrawTargetButtons(Rect panelRect, float scale)
        {
            var targets = targetSelectionCandidates == null
                ? new List<IFighter>()
                : targetSelectionCandidates.ToList();
            var rowHeight = GetCombatMenuRowHeight(scale);
            var listWidth = GetCombatMenuWidth(panelRect, scale);
            var x = panelRect.x + 14f * scale;
            var y = GetCombatMenuY(panelRect, scale);

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var rect = new Rect(x, y + i * (rowHeight + 4f * scale), listWidth, rowHeight);
                var selected = i == selectedMenuIndex;
                if (GUI.Button(rect, GUIContent.none, GetCombatRowStyle(selected)))
                {
                    UiControls.PlayConfirmSound();
                    selectedMenuIndex = i;
                    ActivateTargetSelection(i);
                }

                DrawTargetButtonContent(rect, target, selected, scale);
            }
        }

        private void DrawTargetButtonContent(Rect rect, IFighter target, bool selected, float scale)
        {
            var contentRect = new Rect(rect.x + 8f * scale, rect.y, rect.width - 16f * scale, rect.height);
            var hero = target as Hero;
            if (hero != null)
            {
                Sprite sprite;
                if (UiAssetResolver.TryGetHeroSprite(hero, out sprite))
                {
                    DrawSprite(sprite, new Rect(contentRect.x, contentRect.y + 2f * scale, 32f * scale, rect.height - 4f * scale));
                }

                contentRect.x += 38f * scale;
                contentRect.width -= 38f * scale;
            }

            var style = new GUIStyle(GetTargetButtonStyle(target, selected))
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                focused = { background = null },
                border = new RectOffset()
            };
            GUI.Label(contentRect, target == null ? string.Empty : target.Name, style);
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
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            titleStyle = new GUIStyle(uiTheme.TitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f * scale)
            };
            buttonStyle = uiTheme.ButtonStyle;
        }

        private int GetCombatMenuMoveY()
        {
            var held = InputManager.GetUiMoveYWithRightStick();
            if (held == 0)
            {
                ResetMenuMoveRepeat();
                return 0;
            }

            if (held != repeatingMenuMoveY)
            {
                repeatingMenuMoveY = held;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMenuMoveYTime)
            {
                return 0;
            }

            nextMenuMoveYTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetMenuMoveRepeat()
        {
            repeatingMenuMoveY = 0;
            nextMenuMoveYTime = 0f;
        }

        private void BlockMenuInteractUntilRelease()
        {
            acceptMenuInteractAfterFrame = Time.frameCount + 1;
            waitForMenuInteractRelease = true;
        }

        private bool CanAcceptMenuInteract()
        {
            if (Time.frameCount <= acceptMenuInteractAfterFrame)
            {
                return false;
            }

            if (!waitForMenuInteractRelease)
            {
                return true;
            }

            if (InputManager.GetCommand(InputCommand.Interact))
            {
                return false;
            }

            waitForMenuInteractRelease = false;
            return true;
        }

        private GUIStyle GetMenuButtonStyle(bool selected)
        {
            if (uiTheme == null)
            {
                return buttonStyle;
            }

            return selected ? uiTheme.SelectedTabStyle : uiTheme.ButtonStyle;
        }

        private GUIStyle GetCombatRowStyle(bool selected)
        {
            if (uiTheme == null)
            {
                return selected ? GUI.skin.box : GUIStyle.none;
            }

            return selected ? uiTheme.SelectedRowStyle : GUIStyle.none;
        }

        private GUIStyle GetCombatRowLabelStyle(bool selected)
        {
            var style = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            if (selected && uiTheme != null)
            {
                style.normal.textColor = uiTheme.HighlightColor;
                style.hover.textColor = uiTheme.HighlightColor;
                style.active.textColor = uiTheme.HighlightColor;
                style.focused.textColor = uiTheme.HighlightColor;
            }

            return style;
        }

        private GUIStyle GetTargetButtonStyle(IFighter target, bool selected)
        {
            var style = new GUIStyle(GetCombatRowLabelStyle(selected));
            var color = GetHealthColor(target == null ? 0 : target.Health, target == null ? 0 : target.MaxHealth);
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            return style;
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

#if UNITY_EDITOR
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                Textures[assetPath] = texture;
                return texture;
            }
#endif

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
            GUI.color = GetHealthColor(currentHealth, maxHealth);
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

        private static Color GetHealthColor(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0 || currentHealth <= 0)
            {
                return Color.red;
            }

            var progress = Mathf.Clamp01((float)currentHealth / maxHealth);
            if (progress < 0.1f)
            {
                return new Color(1f, 0.55f, 0f, 1f);
            }

            return progress < 0.5f ? Color.yellow : Color.green;
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
                case Biome.Forest:
                    return "Assets/DungeonEscape/Images/background/forest.png";
                case Biome.Grassland:
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
