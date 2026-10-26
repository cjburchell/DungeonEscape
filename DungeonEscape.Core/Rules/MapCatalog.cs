using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    /// <summary>Resolves permanent map IDs independently of map filenames.</summary>
    public sealed class MapCatalog
    {
        private readonly Dictionary<string, MapDefinition> byId = new Dictionary<string, MapDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, MapDefinition> byPath = new Dictionary<string, MapDefinition>(StringComparer.Ordinal);
        public IReadOnlyCollection<MapDefinition> Maps => byId.Values;

        public MapCatalog(IEnumerable<MapDefinition> maps)
        {
            foreach (var map in maps ?? Enumerable.Empty<MapDefinition>())
            {
                if (map == null || !ContentId.IsValid(map.Id, "map") || string.IsNullOrWhiteSpace(map.Name) ||
                    !(IsAssetPath(map.AssetPath, "Maps/", ".tmx") || ContentPaths.IsCampaignPath(map.AssetPath, map.Id.Split('/')[1], ".tmx")) ||
                    (!string.IsNullOrEmpty(map.EncounterAssetPath) && !(IsAssetPath(map.EncounterAssetPath, "Data/maps/", ".json") || ContentPaths.IsCampaignPath(map.EncounterAssetPath, map.Id.Split('/')[1], ".json"))))
                {
                    throw new ArgumentException("Invalid map catalog entry: " + map?.Id);
                }
                if (byId.ContainsKey(map.Id) || byPath.ContainsKey(map.AssetPath))
                {
                    throw new ArgumentException("Duplicate map ID or path: " + map.Id);
                }
                byId.Add(map.Id, map);
                byPath.Add(map.AssetPath, map);
            }
        }

        public MapDefinition Get(string idOrAssetPath)
        {
            MapDefinition map;
            if (string.IsNullOrEmpty(idOrAssetPath))
            {
                idOrAssetPath = ContentId.OriginalOverworld;
            }
            if (idOrAssetPath.StartsWith("map/", StringComparison.Ordinal))
            {
                if (byId.TryGetValue(idOrAssetPath, out map)) return map;
            }
            else if (byPath.TryGetValue(TiledMapPaths.NormalizeMapAssetPath(idOrAssetPath), out map))
            {
                return map;
            }
            throw new KeyNotFoundException("Map is not registered: " + idOrAssetPath);
        }

        private static bool IsAssetPath(string path, string folder, string extension)
        {
            return path != null && path.StartsWith("Assets/DungeonEscape/" + folder, StringComparison.Ordinal) &&
                   path.EndsWith(extension, StringComparison.Ordinal) && !path.Contains("\\") &&
                   !path.Split('/').Any(part => part == ".." || part == "." || part.Length == 0);
        }
    }
}
