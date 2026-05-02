using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeCombatWindow : MonoBehaviour
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
            ChooseTarget
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
        private DungeonEscapeGameState gameState;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
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
        private int round;
        private int turnIndex;

        public static bool IsOpen { get; private set; }

        public static void Open(IEnumerable<Monster> encounterMonsters, Biome encounterBiome)
        {
            var window = FindAnyObjectByType<DungeonEscapeCombatWindow>();
            if (window == null)
            {
                window = new GameObject("DungeonEscapeCombatWindow").AddComponent<DungeonEscapeCombatWindow>();
            }

            window.monsters.Clear();
            window.turnOrder.Clear();
            window.gameState = DungeonEscapeGameState.GetOrCreate();
            if (encounterMonsters != null)
            {
                window.CreateMonsterInstances(encounterMonsters.Where(monster => monster != null));
            }

            window.biome = encounterBiome;
            window.round = 0;
            window.turnIndex = 0;
            window.actingHero = null;
            window.ShowMessage(window.GetEncounterMessage(), window.BeginRound);
            IsOpen = window.monsters.Count > 0;
            DungeonEscapeGameState.AutoSaveBlocked = IsOpen;
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

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact) ||
                DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                ContinueMessage();
            }
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                IsOpen = false;
                DungeonEscapeGameState.AutoSaveBlocked = false;
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
                DrawCenteredButtons(panelRect, scale, new[] { new CombatButton("Attack", BeginTargetSelection) });
                return;
            }

            if (state == CombatState.ChooseTarget)
            {
                DrawTargetButtons(panelRect, scale);
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
                    state = CombatState.ChooseAction;
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

            var targets = AliveMonsters().ToList();
            if (targets.Count == 1)
            {
                ResolveHeroAttack(targets[0].Instance);
                return;
            }

            state = CombatState.ChooseTarget;
            messageText = "Choose a target for " + actingHero.Name + ".";
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

            DungeonEscapeAudio.GetOrCreate().PlaySoundEffect(damage == 0 ? "miss" : "receive-damage");
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

        private string GetVictoryMessage()
        {
            return gameState == null
                ? "The enemies have been defeated."
                : gameState.ApplyCombatRewards(monsters.Select(monster => monster.Instance));
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
            var targets = AliveMonsters().ToList();
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
                if (GUI.Button(rect, target.Instance.Name, buttonStyle))
                {
                    ResolveHeroAttack(target.Instance);
                }
            }
        }

        private void Close()
        {
            IsOpen = false;
            DungeonEscapeGameState.AutoSaveBlocked = false;
        }

        private void EnsureStyles()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (uiTheme != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = DungeonEscapeUiTheme.Create(settings, scale);
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

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
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
