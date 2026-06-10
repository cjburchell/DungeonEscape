namespace DungeonEscape.Tools.GameEditor.Services;

public static class MapReferenceOptions
{
    private const string MapsPrefix = "maps/";

    public static IReadOnlyList<string> GetMapOptions(IEnumerable<MapDocument> maps)
    {
        return maps
            .Select(ToMapReference)
            .OrderBy(map => map, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> GetSpawnOptions(IEnumerable<MapDocument> maps, string? mapReference)
    {
        var map = ResolveMap(maps, mapReference);
        return map?.Objects
            .Where(mapObject => string.Equals(mapObject.Class, "Spawn", StringComparison.OrdinalIgnoreCase))
            .Select(mapObject => mapObject.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    public static MapDocument? ResolveMap(IEnumerable<MapDocument> maps, string? mapReference)
    {
        if (string.IsNullOrWhiteSpace(mapReference))
        {
            return null;
        }

        var id = NormalizeMapReference(mapReference);
        return maps.FirstOrDefault(map => string.Equals(map.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static string ToMapReference(MapDocument map) => MapsPrefix + map.Id;

    public static string NormalizeMapReference(string mapReference) =>
        mapReference.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase)
            ? mapReference[MapsPrefix.Length..]
            : mapReference;
}