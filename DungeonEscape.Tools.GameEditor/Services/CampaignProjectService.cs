#nullable enable
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;

namespace DungeonEscape.Tools.GameEditor.Services;

public sealed class CampaignProjectService(AssetContext assets)
{
    public CampaignDefinition Create(string key, string name, string description, DungeonEscapeDataSet shared)
    {
        var template = new CampaignProjectTemplate(key, name, description);
        var indexPath = assets.ResolveAssetPath("Assets/DungeonEscape/Data/campaigns.json");
        var index = JsonConvert.DeserializeObject<CampaignIndex>(File.ReadAllText(indexPath))
            ?? throw new InvalidDataException("Invalid campaign index.");
        if (index.SchemaVersion != 1 || index.Manifests == null) throw new InvalidDataException("Unsupported campaign index.");
        var definitions = index.Manifests.Select(path =>
        {
            if (!CampaignCatalog.IsDataAssetPath(path)) throw new InvalidDataException("Invalid manifest path.");
            return JsonConvert.DeserializeObject<CampaignDefinition>(File.ReadAllText(assets.ResolveAssetPath(path)))!;
        }).ToList();
        _ = new CampaignCatalog(definitions, index.DefaultCampaignId);
        if (definitions.Any(campaign => CampaignCatalog.GetScope(campaign).Equals(key, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("That campaign key is already in use.");

        var assetRoot = ContentPaths.AssetPrefix + "Campaigns/" + key + "/";
        var target = Path.GetDirectoryName(assets.ResolveAssetPath(assetRoot + "campaign.json"))!;
        if (Directory.Exists(target) || File.Exists(target)) throw new InvalidOperationException("That campaign directory already exists.");
        string Read(string path) => template.Files.TryGetValue(path, out var value) ? value :
            File.Exists(assets.ResolveAssetPath(path)) ? File.ReadAllText(assets.ResolveAssetPath(path)) : null!;
        _ = CampaignContentLoader.Load(template.Campaign, CampaignContentResolver.ExtractBase(shared), Read);
        foreach (var tileset in new[] { "overworld.tsx", "biomes.tsx" })
            if (!File.Exists(assets.ResolveAssetPath(ContentPaths.AssetPrefix + "Tilesets/" + tileset)))
                throw new FileNotFoundException("The starter map requires the shared " + tileset + " tileset.");

        var parent = Directory.GetParent(target)!.FullName;
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".creating-" + Guid.NewGuid().ToString("N"));
        var indexTemp = indexPath + ".tmp-" + Guid.NewGuid().ToString("N");
        var moved = false;
        try
        {
            foreach (var file in template.Files)
            {
                var path = Path.GetFullPath(Path.Combine(staging, file.Key[assetRoot.Length..]));
                EnsureInside(path, staging);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, file.Value);
            }
            Directory.Move(staging, target);
            moved = true;
            index.Manifests.Add(assetRoot + "campaign.json");
            if (definitions.Count == 0) index.DefaultCampaignId = template.Campaign.Id;
            File.WriteAllText(indexTemp, JsonConvert.SerializeObject(index, Formatting.Indented));
            File.Move(indexTemp, indexPath, true); // Register only after the complete project is in place.
            return template.Campaign;
        }
        catch
        {
            if (moved && Directory.Exists(target)) { EnsureInside(target, parent); Directory.Delete(target, true); }
            throw;
        }
        finally
        {
            if (Directory.Exists(staging)) { EnsureInside(staging, parent); Directory.Delete(staging, true); }
            if (File.Exists(indexTemp)) File.Delete(indexTemp);
        }
    }

    private static void EnsureInside(string path, string root)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Campaign path is outside its project directory.");
    }
}
