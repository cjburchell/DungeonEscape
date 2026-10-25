using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public sealed class CampaignCatalog
    {
        private readonly Dictionary<string, CampaignDefinition> campaigns = new Dictionary<string, CampaignDefinition>(StringComparer.Ordinal);
        public IReadOnlyCollection<CampaignDefinition> Campaigns => campaigns.Values;
        public CampaignDefinition Default { get; }

        public CampaignCatalog(IEnumerable<CampaignDefinition> definitions, string defaultId)
        {
            var scopes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var campaign in definitions ?? Enumerable.Empty<CampaignDefinition>())
            {
                Validate(campaign);
                if (campaigns.ContainsKey(campaign.Id) || !scopes.Add(GetScope(campaign)))
                    throw new ArgumentException("Duplicate campaign ID or scope: " + campaign.Id);
                campaigns.Add(campaign.Id, campaign);
            }
            Default = campaigns.Count == 0 ? null : Get(defaultId);
        }

        public CampaignDefinition Get(string id)
        {
            CampaignDefinition campaign;
            if (id != null && campaigns.TryGetValue(id, out campaign)) return campaign;
            throw new ArgumentException("Campaign is not installed: " + id);
        }

        public static string GetScope(CampaignDefinition campaign) => campaign.Id.Split('/')[1];

        public static bool IsDataAssetPath(string path)
        {
            return path != null && path.StartsWith("Assets/DungeonEscape/Data/", StringComparison.Ordinal) &&
                   path.EndsWith(".json", StringComparison.Ordinal) && !path.Contains("\\") &&
                   !path.Split('/').Any(part => part == "." || part == ".." || part.Length == 0);
        }

        public static void Validate(CampaignDefinition campaign)
        {
            if (campaign == null || campaign.SchemaVersion != 1 || !ContentId.IsValid(campaign.Id, "campaign") ||
                string.IsNullOrWhiteSpace(campaign.Name) || string.IsNullOrWhiteSpace(campaign.ContentVersion))
                throw new ArgumentException("Invalid campaign manifest.");
            var scope = GetScope(campaign);
            if (scope == "base" || !ContentId.IsValid(campaign.StartMapId, "map") ||
                !campaign.StartMapId.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
                !ContentId.IsValid(campaign.OverworldMapId, "map") ||
                !campaign.OverworldMapId.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
                campaign.StartColumn < 0 || campaign.StartRow < 0 ||
                !IsDataAssetPath(campaign.MapsAssetPath) || !IsDataAssetPath(campaign.ItemsAssetPath) ||
                !IsDataAssetPath(campaign.QuestsAssetPath) || !IsDataAssetPath(campaign.DialogsAssetPath) ||
                (!string.IsNullOrEmpty(campaign.MonstersAssetPath) && !IsDataAssetPath(campaign.MonstersAssetPath)) ||
                campaign.OverworldMonsterIds == null || campaign.OverworldMonsterIds.Distinct(StringComparer.Ordinal).Count() != campaign.OverworldMonsterIds.Count)
                throw new ArgumentException("Invalid campaign scope, paths, start location, or roster: " + campaign.Id);
        }
    }
}
