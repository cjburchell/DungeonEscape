using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public sealed class CampaignProjectTemplate
    {
        public CampaignDefinition Campaign { get; }
        public IReadOnlyDictionary<string, string> Files { get; }

        public CampaignProjectTemplate(string key, string name, string description)
        {
            var reserved = new[] { "base", "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9" };
            if (!ContentId.IsValid("campaign/" + key + "/main", "campaign") || reserved.Contains(key) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Choose a unique lowercase campaign key using letters, digits, and hyphens, and enter a name.");
            var root = ContentPaths.AssetPrefix + "Campaigns/" + key + "/";
            Campaign = new CampaignDefinition
            {
                Id = "campaign/" + key + "/main", Name = name.Trim(), Description = description?.Trim() ?? "", ContentVersion = "1.0",
                StartMapId = "map/" + key + "/overworld", OverworldMapId = "map/" + key + "/overworld",
                StartSpawnId = "start", StartColumn = 8, StartRow = 8,
                MapsAssetPath = root + "Data/maps.json", ItemsAssetPath = root + "Data/items.json",
                MonstersAssetPath = root + "Data/monsters.json", QuestsAssetPath = root + "Data/quests.json", DialogsAssetPath = root + "Data/dialog.json"
            };
            CampaignCatalog.Validate(Campaign);
            var map = new MapDefinition
            {
                Id = Campaign.StartMapId, Name = "Overworld", AssetPath = root + "Maps/overworld.tmx",
                EncounterAssetPath = root + "Data/Encounters/overworld_monsters.json"
            };
            var files = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [root + "campaign.json"] = JsonConvert.SerializeObject(Campaign, Formatting.Indented),
                [Campaign.MapsAssetPath] = JsonConvert.SerializeObject(new[] { map }, Formatting.Indented),
                [Campaign.ItemsAssetPath] = "[]\n", [Campaign.MonstersAssetPath] = "[]\n",
                [Campaign.QuestsAssetPath] = "[]\n", [Campaign.DialogsAssetPath] = "[]\n",
                [map.EncounterAssetPath] = "[]\n", [map.AssetPath] = StarterMap()
            };
            Files = files;
        }

        private static string StarterMap()
        {
            var map = new XElement("map", new XAttribute("version", "1.10"), new XAttribute("class", "Overworld"),
                new XAttribute("orientation", "orthogonal"), new XAttribute("renderorder", "right-down"),
                new XAttribute("width", 16), new XAttribute("height", 16), new XAttribute("tilewidth", 32), new XAttribute("tileheight", 32),
                new XAttribute("infinite", 0), new XAttribute("nextlayerid", 4), new XAttribute("nextobjectid", 2),
                new XElement("properties", new XElement("property", new XAttribute("name", "song"), new XAttribute("value", "not-in-vain"))),
                new XElement("tileset", new XAttribute("firstgid", 1), new XAttribute("source", "../../../Tilesets/overworld.tsx")),
                new XElement("tileset", new XAttribute("firstgid", 481), new XAttribute("source", "../../../Tilesets/biomes.tsx")),
                Layer(1, "floor", 191), Layer(2, "biomes", 481),
                new XElement("objectgroup", new XAttribute("id", 3), new XAttribute("name", "objects"),
                    new XElement("properties", new XElement("property", new XAttribute("name", "RenderRole"), new XAttribute("value", "Objects"))),
                    new XElement("object", new XAttribute("id", 1), new XAttribute("name", "start"), new XAttribute("class", "Spawn"),
                        new XAttribute("x", 256), new XAttribute("y", 256), new XAttribute("width", 32), new XAttribute("height", 32),
                        new XElement("properties", new XElement("property", new XAttribute("name", "DefaultSpawn"), new XAttribute("type", "bool"), new XAttribute("value", "true"))))));
            return new XDocument(new XDeclaration("1.0", "utf-8", null), map).ToString();
        }

        private static XElement Layer(int id, string name, int gid) => new XElement("layer", new XAttribute("id", id),
            new XAttribute("name", name), new XAttribute("width", 16), new XAttribute("height", 16),
            new XElement("data", new XAttribute("encoding", "csv"), string.Join(",", Enumerable.Repeat(gid.ToString(), 256))));
    }
}
