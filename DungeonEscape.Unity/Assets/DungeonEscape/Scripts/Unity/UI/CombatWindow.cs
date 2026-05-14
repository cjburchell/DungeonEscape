using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow : MonoBehaviour
    {
        private const int WindowDepth = -2500;
        private const float DamageFlashDuration = 0.65f;
        private const float DamageFlashInterval = 0.08f;
        private const string EndFightSong = "not-in-vain";
        private const string TitleSong = "first-story";
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

        private readonly List<CombatMonster> monsters = new List<CombatMonster>();
        private readonly List<RoundAction> roundActions = new List<RoundAction>();
        private readonly List<Hero> pendingHeroes = new List<Hero>();
        private readonly CombatSelectionMemory selectionMemory = new CombatSelectionMemory();
        private readonly CombatMenuInput menuInput = new CombatMenuInput();
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
        private int round;
        private static CombatWindow currentWindow;

        public static bool IsOpen { get; private set; }

        public static bool IsPartyTargetCandidate(Hero hero)
        {
            return currentWindow != null && currentWindow.IsCurrentTargetCandidate(hero);
        }

        public static bool IsPartyTargetSelected(Hero hero)
        {
            return currentWindow != null && ReferenceEquals(currentWindow.GetSelectedTarget(), hero);
        }

        public static void SelectPartyTarget(Hero hero)
        {
            if (currentWindow != null)
            {
                currentWindow.SelectTarget(hero);
            }
        }

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
            window.selectionMemory.Clear();
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
            currentWindow = window;
            window.ShowMessage(window.GetEncounterMessage(), window.BeginRound);
            IsOpen = window.monsters.Count > 0;
            GameState.AutoSaveBlocked = IsOpen;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsOpen = false;
            currentWindow = null;
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

            var moveY = menuInput.GetMoveY();
            if (moveY != 0)
            {
                MoveSelection(moveY);
            }
            else if (state == CombatState.ChooseTarget)
            {
                var moveX = menuInput.GetMoveX();
                if (moveX != 0)
                {
                    MoveSelection(moveX);
                }
            }

            if (menuInput.CanAcceptInteract() && InputManager.GetCommandDown(InputCommand.Interact))
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

            if (ReferenceEquals(currentWindow, this))
            {
                currentWindow = null;
            }
        }
    }
}
