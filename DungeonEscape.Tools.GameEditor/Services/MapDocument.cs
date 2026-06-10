using System.Globalization;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;

namespace DungeonEscape.Tools.GameEditor.Services;

public sealed class MapDocument
{
    public string Id { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MapObjectDocument> Objects { get; set; } = new();
    public List<RandomMonster> RandomMonsters { get; set; } = new();
    public bool RandomMonstersFileExists { get; set; }
    public bool IsDirty { get; set; }
    public bool IsOverworld => string.Equals(Id, "overworld", StringComparison.OrdinalIgnoreCase);

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

public sealed class MapDocumentService
{
    private const uint TiledGidMask = 0x1FFFFFFF;

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public List<MapDocument> LoadMaps(string? mapsDirectory, string? mapDataDirectory)
    {
        if (string.IsNullOrEmpty(mapsDirectory) || !Directory.Exists(mapsDirectory))
        {
            return new List<MapDocument>();
        }

        return Directory
            .EnumerateFiles(mapsDirectory, "*.tmx", SearchOption.AllDirectories)
            .OrderBy(path => GetMapId(mapsDirectory, path), StringComparer.OrdinalIgnoreCase)
            .Select(path => LoadMap(mapsDirectory, path, mapDataDirectory))
            .ToList();
    }

    public void SaveMaps(IEnumerable<MapDocument> maps, string? mapDataDirectory)
    {
        foreach (var map in maps ?? Array.Empty<MapDocument>())
        {
            if (!map.IsDirty)
            {
                continue;
            }

            SaveMap(map);
            SaveRandomMonsters(map, mapDataDirectory);
            map.IsDirty = false;
        }
    }

    private static MapDocument LoadMap(string mapsDirectory, string path, string? mapDataDirectory)
    {
        var xml = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = xml.Root ?? new XElement("map");
        var id = GetMapId(mapsDirectory, path);
        var map = new MapDocument
        {
            Id = id,
            FilePath = path,
            Class = GetString(root, "class"),
            Properties = ReadProperties(root),
            Xml = xml
        };

        foreach (var objectGroup in root.Elements("objectgroup"))
        {
            var groupName = GetString(objectGroup, "name");
            foreach (var mapObject in objectGroup.Elements("object"))
            {
                map.Objects.Add(new MapObjectDocument
                {
                    MapId = id,
                    GroupName = groupName,
                    Id = GetInt(mapObject, "id"),
                    Name = GetString(mapObject, "name"),
                    Class = GetString(mapObject, "class"),
                    Gid = GetGid(mapObject, "gid"),
                    X = GetFloat(mapObject, "x"),
                    Y = GetFloat(mapObject, "y"),
                    Width = GetFloat(mapObject, "width"),
                    Height = GetFloat(mapObject, "height"),
                    Properties = ReadProperties(mapObject),
                    Element = mapObject
                });
            }
        }

        LoadRandomMonsters(map, mapDataDirectory);
        return map;
    }

    private static void SaveMap(MapDocument map)
    {
        if (map.Xml?.Root == null || string.IsNullOrEmpty(map.FilePath))
        {
            return;
        }

        SetOptionalAttribute(map.Xml.Root, "class", map.Class);
        WriteProperties(map.Xml.Root, map.Properties);

        foreach (var mapObject in map.Objects)
        {
            if (mapObject.Element == null)
            {
                continue;
            }

            SetOptionalAttribute(mapObject.Element, "name", mapObject.Name);
            SetOptionalAttribute(mapObject.Element, "class", mapObject.Class);
            WriteProperties(mapObject.Element, mapObject.Properties);
        }

        map.Xml.Save(map.FilePath, SaveOptions.DisableFormatting);
    }

    private static void LoadRandomMonsters(MapDocument map, string? mapDataDirectory)
    {
        var path = GetRandomMonstersPath(map, mapDataDirectory);
        map.RandomMonstersFileExists = path != null && File.Exists(path);
        if (!map.RandomMonstersFileExists || path == null)
        {
            map.RandomMonsters = new List<RandomMonster>();
            return;
        }

        var json = File.ReadAllText(path);
        map.RandomMonsters = JsonConvert.DeserializeObject<List<RandomMonster>>(json) ?? new List<RandomMonster>();
    }

    private static void SaveRandomMonsters(MapDocument map, string? mapDataDirectory)
    {
        if (map.IsOverworld || (!map.RandomMonstersFileExists && map.RandomMonsters.Count == 0))
        {
            return;
        }

        var path = GetRandomMonstersPath(map, mapDataDirectory);
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

    private static string? GetRandomMonstersPath(MapDocument map, string? mapDataDirectory)
    {
        return string.IsNullOrEmpty(mapDataDirectory)
            ? null
            : Path.Combine(mapDataDirectory, map.Id.Replace('/', Path.DirectorySeparatorChar) + "_monsters.json");
    }

    private static string GetMapId(string mapsDirectory, string path)
    {
        var relative = Path.GetRelativePath(mapsDirectory, path).Replace('\\', '/');
        return relative.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
            ? relative[..^4]
            : relative;
    }

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