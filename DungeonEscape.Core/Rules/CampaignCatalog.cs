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

        public static bool IsDataAssetPath(string path) => ContentPaths.IsJsonPath(path);

        public static void Validate(CampaignDefinition campaign)
        {
            if (campaign == null || campaign.SchemaVersion != 1 || !ContentId.IsValid(campaign.Id, "campaign") ||
                string.IsNullOrWhiteSpace(campaign.Name) || string.IsNullOrWhiteSpace(campaign.ContentVersion))
                throw new ArgumentException("Invalid campaign manifest.");
            var scope = GetScope(campaign);
            if (campaign.MaxStartingPartyMembers < 1)
                throw new ArgumentException("Maximum starting party members must be at least one: " + campaign.Id);
            if (scope == "base" || !ContentId.IsValid(campaign.StartMapId, "map") ||
                !campaign.StartMapId.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
                !ContentId.IsValid(campaign.OverworldMapId, "map") ||
                !campaign.OverworldMapId.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
                campaign.StartColumn < 0 || campaign.StartRow < 0 ||
                !ContentPaths.IsCampaignPath(campaign.MapsAssetPath, scope, ".json") || !ContentPaths.IsCampaignPath(campaign.ItemsAssetPath, scope, ".json") ||
                !ContentPaths.IsCampaignPath(campaign.QuestsAssetPath, scope, ".json") || !ContentPaths.IsCampaignPath(campaign.DialogsAssetPath, scope, ".json") ||
                (!string.IsNullOrEmpty(campaign.MonstersAssetPath) && !ContentPaths.IsCampaignPath(campaign.MonstersAssetPath, scope, ".json")) ||
                campaign.OverworldMonsterIds == null || campaign.OverworldMonsterIds.Distinct(StringComparer.Ordinal).Count() != campaign.OverworldMonsterIds.Count)
                throw new ArgumentException("Invalid campaign scope, paths, start location, or roster: " + campaign.Id);
        }
    }
}
