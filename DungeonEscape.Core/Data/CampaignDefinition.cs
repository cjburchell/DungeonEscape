using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Data
{
    public sealed class CampaignDefinition
    {
        public int SchemaVersion { get; set; } = 1;
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ContentVersion { get; set; }
        public string StartMapId { get; set; }
        public string StartSpawnId { get; set; }
        public int StartColumn { get; set; }
        public int StartRow { get; set; }
        public string OverworldMapId { get; set; }
        public string MapsAssetPath { get; set; }
        public string ItemsAssetPath { get; set; }
        public string MonstersAssetPath { get; set; }
        public string QuestsAssetPath { get; set; }
        public string DialogsAssetPath { get; set; }
        public List<string> OverworldMonsterIds { get; set; } = new List<string>();
    }

    public sealed class CampaignIndex
    {
        public int SchemaVersion { get; set; } = 1;
        public string DefaultCampaignId { get; set; }
        public List<string> Manifests { get; set; } = new List<string>();
    }
}
