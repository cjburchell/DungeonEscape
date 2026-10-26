#nullable enable
using System.Globalization;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;

namespace DungeonEscape.Tools.GameEditor.Services;

public sealed class MapDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EncounterAssetPath { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MapObjectDocument> Objects { get; set; } = new();
    public List<RandomMonster> RandomMonsters { get; set; } = new();
    public bool RandomMonstersFileExists { get; set; }
    public bool IsDirty { get; set; }
    public bool IsOverworld { get; set; }

    internal XDocument? Xml { get; set; }
}

public sealed class MapObjectDocument
{
    public string MapId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int Gid { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    internal XElement? Element { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Object {Id}" : Name;
}

public sealed class MapDocumentService(AssetContext assets)
{
    private const uint TiledGidMask = 0x1FFFFFFF;
    private const string OverworldMapClass = "Overworld";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public List<MapDocument> LoadMaps(CampaignDefinition? campaign)
    {
        if (campaign == null) return new List<MapDocument>();
        var catalog = new MapCatalog(JsonConvert.DeserializeObject<List<MapDefinition>>(File.ReadAllText(assets.ResolveAssetPath(campaign.MapsAssetPath))));
        var scope = CampaignCatalog.GetScope(campaign);
        if (catalog.Maps.Any(map => !map.Id.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
            !ContentPaths.IsCampaignPath(map.AssetPath, scope, ".tmx") ||
            (!string.IsNullOrEmpty(map.EncounterAssetPath) && !ContentPaths.IsCampaignPath(map.EncounterAssetPath, scope, ".json"))))
            throw new InvalidDataException("The map catalog references files outside this campaign.");
        return catalog.Maps.OrderBy(map => map.Name, StringComparer.Ordinal).Select(definition => LoadMap(definition, campaign)).ToList();
    }

    public void SaveMaps(IEnumerable<MapDocument> maps)
    {
        foreach (var map in maps)
        {
            if (!map.IsDirty) continue;
            SaveMap(map);
            SaveRandomMonsters(map);
            map.IsDirty = false;
        }
    }

    private MapDocument LoadMap(MapDefinition definition, CampaignDefinition campaign)
    {
        var path = assets.ResolveAssetPath(definition.AssetPath);
        var xml = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = xml.Root ?? new XElement("map");
        var map = new MapDocument
        {
            Id = definition.Id, Name = definition.Name, EncounterAssetPath = definition.EncounterAssetPath,
            IsOverworld = definition.Id == campaign.OverworldMapId, FilePath = path,
            Class = NormalizeMapClass(GetString(root, "class")), Properties = ReadProperties(root), Xml = xml
        };
        foreach (var objectGroup in root.Elements("objectgroup"))
        foreach (var mapObject in objectGroup.Elements("object"))
            map.Objects.Add(new MapObjectDocument
            {
                MapId = map.Id, GroupName = GetString(objectGroup, "name"), Id = GetInt(mapObject, "id"),
                Name = GetString(mapObject, "name"), Class = (string?)mapObject.Attribute("class") ?? (string?)mapObject.Attribute("type") ?? string.Empty, Gid = GetGid(mapObject, "gid"),
                X = GetFloat(mapObject, "x"), Y = GetFloat(mapObject, "y"), Width = GetFloat(mapObject, "width"), Height = GetFloat(mapObject, "height"),
                Properties = ReadProperties(mapObject), Element = mapObject
            });
        LoadRandomMonsters(map);
        return map;
    }

    private static void SaveMap(MapDocument map)
    {
        if (map.Xml?.Root == null || string.IsNullOrEmpty(map.FilePath))
        {
            return;
        }

        SetOptionalAttribute(map.Xml.Root, "class", NormalizeMapClass(map.Class));
        WriteProperties(map.Xml.Root, map.Properties);

        foreach (var mapObject in map.Objects)
        {
            if (mapObject.Element == null)
            {
                continue;
            }

            SetOptionalAttribute(mapObject.Element, "name", mapObject.Name);
            SetOptionalAttribute(mapObject.Element, "class", mapObject.Class);
            mapObject.Element.Attribute("type")?.Remove();
            WriteProperties(mapObject.Element, mapObject.Properties);
        }

        map.Xml.Save(map.FilePath, SaveOptions.DisableFormatting);
    }

    private void LoadRandomMonsters(MapDocument map)
    {
        var path = GetRandomMonstersPath(map);
        map.RandomMonstersFileExists = path != null && File.Exists(path);
        if (!map.RandomMonstersFileExists || path == null)
        {
            map.RandomMonsters = new List<RandomMonster>();
            return;
        }

        var json = File.ReadAllText(path);
        map.RandomMonsters = JsonConvert.DeserializeObject<List<RandomMonster>>(json) ?? new List<RandomMonster>();
    }

    private void SaveRandomMonsters(MapDocument map)
    {
        if (map.IsOverworld || (!map.RandomMonstersFileExists && map.RandomMonsters.Count == 0))
        {
            return;
        }

        if (string.IsNullOrEmpty(map.EncounterAssetPath) && map.RandomMonsters.Count > 0)
        {
            throw new InvalidOperationException("Add EncounterAssetPath to the map catalog before adding encounters to " + map.Id);
        }
        var path = GetRandomMonstersPath(map);
        if (path == null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonConvert.SerializeObject(map.RandomMonsters, SerializerSettings);
        var token = JToken.Parse(json);
        PruneDefaultValues(token, true);
        File.WriteAllText(path, token.ToString(Formatting.Indented));
        map.RandomMonstersFileExists = true;
    }

    private string? GetRandomMonstersPath(MapDocument map) =>
        string.IsNullOrEmpty(map.EncounterAssetPath) ? null : assets.ResolveAssetPath(map.EncounterAssetPath);

    private static Dictionary<string, string> ReadProperties(XElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var properties = element.Element("properties");
        if (properties == null)
        {
            return result;
        }

        foreach (var property in properties.Elements("property"))
        {
            var name = GetString(property, "name");
            if (!string.IsNullOrEmpty(name))
            {
                result[name] = GetString(property, "value") ?? property.Value;
            }
        }

        return result;
    }

    private static void WriteProperties(XElement element, IReadOnlyDictionary<string, string> values)
    {
        var properties = element.Element("properties");
        var cleanValues = values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (cleanValues.Count == 0)
        {
            properties?.Remove();
            return;
        }

        if (properties == null)
        {
            properties = new XElement("properties");
            element.AddFirst(properties);
        }

        foreach (var property in properties.Elements("property").ToList())
        {
            var name = GetString(property, "name");
            if (string.IsNullOrEmpty(name) || !cleanValues.ContainsKey(name))
            {
                property.Remove();
            }
        }

        foreach (var pair in cleanValues.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var property = properties.Elements("property")
                .FirstOrDefault(item => string.Equals(GetString(item, "name"), pair.Key, StringComparison.OrdinalIgnoreCase));
            if (property == null)
            {
                property = new XElement("property", new XAttribute("name", pair.Key));
                properties.Add(property);
            }

            property.SetAttributeValue("name", pair.Key);
            SetPropertyValue(property, pair.Value);
        }
    }

    private static void SetPropertyValue(XElement property, string value)
    {
        property.Value = string.Empty;
        property.SetAttributeValue("value", value);
        property.SetAttributeValue("type", null);

        if (bool.TryParse(value, out _))
        {
            property.SetAttributeValue("type", "bool");
        }
        else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            property.SetAttributeValue("type", "int");
        }
    }

    private static void SetOptionalAttribute(XElement element, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            element.Attribute(name)?.Remove();
        }
        else
        {
            element.SetAttributeValue(name, value);
        }
    }

    private static string NormalizeMapClass(string? value)
    {
        return string.Equals(value, OverworldMapClass, StringComparison.OrdinalIgnoreCase)
            ? OverworldMapClass
            : string.Empty;
    }

    private static string GetString(XElement element, string name) => element.Attribute(name)?.Value ?? string.Empty;

    private static int GetInt(XElement element, string name) => int.TryParse(GetString(element, name), out var value) ? value : 0;

    private static int GetGid(XElement element, string name) =>
        uint.TryParse(GetString(element, name), out var value) ? (int)(value & TiledGidMask) : 0;

    private static float GetFloat(XElement element, string name) =>
        float.TryParse(GetString(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0f;

    private static bool PruneDefaultValues(JToken token, bool isRoot = false)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>().ToList())
                {
                    if (PruneDefaultValues(property.Value))
                    {
                        property.Remove();
                    }
                }

                return !isRoot && !token.Children<JProperty>().Any();
            case JTokenType.Array:
                foreach (var child in token.Children().ToList())
                {
                    if (PruneDefaultValues(child))
                    {
                        child.Remove();
                    }
                }

                return !isRoot && !token.Children().Any();
            case JTokenType.Null:
            case JTokenType.Undefined:
                return !isRoot;
            case JTokenType.String:
                return !isRoot && string.IsNullOrEmpty(token.Value<string>());
            case JTokenType.Boolean:
                return !isRoot && token.Value<bool>() == false;
            case JTokenType.Integer:
                return !isRoot && token.Value<long>() == 0;
            case JTokenType.Float:
                return !isRoot && token.Value<double>() == 0d;
            default:
                return false;
        }
    }
}
