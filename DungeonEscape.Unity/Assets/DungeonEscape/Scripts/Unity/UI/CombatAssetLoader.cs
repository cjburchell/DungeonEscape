using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Redpoint.DungeonEscape.Unity.UI
{
    internal static class CombatAssetLoader
    {
        private const string MonsterTilesetAssetPath = "Assets/DungeonEscape/Tilesets/allmonsters.tsx";
        private static readonly Dictionary<int, string> MonsterImagePaths = new Dictionary<int, string>();
        private static readonly Dictionary<string, Texture2D> Textures =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        public static Texture2D LoadMonsterTexture(Monster monster)
        {
            EnsureMonsterImagePaths();
            string assetPath;
            return monster != null &&
                   MonsterImagePaths.TryGetValue(monster.ImageId, out assetPath)
                ? LoadTexture(assetPath)
                : null;
        }

        public static Texture2D LoadTexture(string assetPath)
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

        public static string GetBackgroundAssetPath(Biome biome)
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
    }
}
