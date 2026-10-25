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
        private static DungeonEscapeDataSet sharedData;
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
            if (Catalog.Campaigns.Count == 0)
            {
                sharedData = shared;
                Current = null;
                Loader.UseCatalog(new MapCatalog(new MapDefinition[0]));
                GameDataCache.Load(shared);
                return null;
            }
            var candidate = LoadCandidate(Catalog.Default, shared);
            sharedData = shared;
            Activate(candidate);
            return candidate;
        }

        public static CampaignContentContext Prepare(string campaignId)
        {
            if (sharedData == null) throw new InvalidOperationException("Game content has not initialized yet.");
            return LoadCandidate(Catalog.Get(campaignId), sharedData);
        }

        private static CampaignContentContext LoadCandidate(CampaignDefinition campaign, DungeonEscapeDataSet shared)
        {
            var candidate = CampaignContentLoader.Load(campaign, shared, ReadAsset);
            candidate.Data.TestMap = TiledMapInfo.Parse(ReadRequired(candidate.Maps.Get(campaign.StartMapId).AssetPath));
            return candidate;
        }

        public static void Activate(CampaignContentContext candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            Loader.UseCatalog(candidate.Maps);
            GameDataCache.Load(candidate.Data);
            Current = candidate;
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
