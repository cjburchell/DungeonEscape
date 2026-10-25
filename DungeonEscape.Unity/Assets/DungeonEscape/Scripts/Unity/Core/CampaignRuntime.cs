using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.Unity.Map.Tiled;

namespace Redpoint.DungeonEscape.Unity.Core
{
    public static class CampaignRuntime
    {
        private static CampaignCatalog catalog;
        public static CampaignContentContext Current { get; private set; }
        public static CampaignDefinition Campaign => Current == null ? Catalog.Default : Current.Campaign;

        public static CampaignCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    var index = JsonConvert.DeserializeObject<CampaignIndex>(ReadRequired("Assets/DungeonEscape/Data/campaigns.json"));
                    if (index == null || index.SchemaVersion != 1 || index.Manifests == null ||
                        index.Manifests.Any(path => !CampaignCatalog.IsDataAssetPath(path)) ||
                        index.Manifests.Distinct(StringComparer.Ordinal).Count() != index.Manifests.Count)
                        throw new InvalidOperationException("Invalid campaign index.");
                    catalog = new CampaignCatalog(index.Manifests.Select(path => JsonConvert.DeserializeObject<CampaignDefinition>(ReadRequired(path))), index.DefaultCampaignId);
                }
                return catalog;
            }
        }

        public static CampaignContentContext Initialize(DungeonEscapeDataSet source)
        {
            var shared = CampaignContentResolver.ExtractBase(source);
            var candidate = CampaignContentLoader.Load(Catalog.Default, shared, ReadAsset);
            // Publish only after every definition, map, and reference has validated.
            Loader.UseCatalog(candidate.Maps);
            Current = candidate;
            return candidate;
        }

        public static string ReadAsset(string assetPath)
        {
            var path = UnityAssetPath.ToRuntimePath(assetPath);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static string ReadRequired(string assetPath) => ReadAsset(assetPath) ??
            throw new InvalidOperationException("Required campaign file is missing: " + assetPath);
    }
}
