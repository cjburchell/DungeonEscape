using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeCombatPreviewWindow : MonoBehaviour
    {
        private const int WindowDepth = -2500;
        private const string MonsterTilesetAssetPath = "Assets/DungeonEscape/Tilesets/allmonsters.tsx";
        private static readonly Dictionary<int, string> MonsterImagePaths = new Dictionary<int, string>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private readonly List<CombatPreviewMonster> monsters = new List<CombatPreviewMonster>();
        private Biome biome;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle titleStyle;
        private float lastPixelScale;
        private string lastThemeSignature;

        public static bool IsOpen { get; private set; }

        public static void Open(IEnumerable<Monster> encounterMonsters, Biome encounterBiome)
        {
            var window = FindAnyObjectByType<DungeonEscapeCombatPreviewWindow>();
            if (window == null)
            {
                window = new GameObject("DungeonEscapeCombatPreviewWindow").AddComponent<DungeonEscapeCombatPreviewWindow>();
            }

            window.monsters.Clear();
            if (encounterMonsters != null)
            {
                window.monsters.AddRange(encounterMonsters
                    .Where(monster => monster != null)
                    .Select(monster => new CombatPreviewMonster(monster)));
            }

            window.biome = encounterBiome;
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
                Close();
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
                    monster.CurrentHealth,
                    monster.MaxHealth,
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
                new Rect(panelRect.x + 14f * scale, panelRect.y + 12f * scale, panelRect.width - 28f * scale, 34f * scale),
                GetEncounterMessage(),
                labelStyle);

            var buttonWidth = 96f * scale;
            var buttonHeight = 32f * scale;
            var buttonRect = new Rect((Screen.width - buttonWidth) / 2f, panelRect.yMax - buttonHeight - 16f * scale, buttonWidth, buttonHeight);
            if (GUI.Button(buttonRect, "OK", buttonStyle))
            {
                Close();
            }
        }

        private static Rect GetBattlefieldRect(float scale)
        {
            var footerHeight = Mathf.Min(220f * scale, Screen.height * 0.32f);
            return new Rect(0f, 0f, Screen.width, Screen.height - footerHeight - 16f * scale);
        }

        private string GetEncounterMessage()
        {
            return monsters.Count == 1
                ? "You have encountered a " + monsters[0].Data.Name + "!"
                : "You have encountered " + monsters.Count + " enemies!";
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
                Debug.LogWarning("Combat preview image not found: " + assetPath);
                Textures[assetPath] = null;
                return null;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning("Could not load combat preview image: " + assetPath);
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

        private sealed class CombatPreviewMonster
        {
            public CombatPreviewMonster(Monster monster)
            {
                Data = monster;
                MaxHealth = Math.Max(1, Dice.Roll(monster.HealthRandom, monster.HealthTimes, monster.HealthConst));
                CurrentHealth = MaxHealth;
            }

            public Monster Data { get; private set; }
            public int CurrentHealth { get; private set; }
            public int MaxHealth { get; private set; }
        }
    }
}
