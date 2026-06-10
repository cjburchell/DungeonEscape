using System.IO;
using Newtonsoft.Json;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Persists small user preferences for the editor (such as the most recently
/// opened file) to a JSON file in the user's application-data folder so they
/// survive between launches.
/// </summary>
public sealed class EditorSettingsService
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly string settingsPath;
    private readonly string legacySettingsPath;
    private EditorSettings settings;

    public EditorSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "DungeonEscape.GameEditor");
        settingsPath = Path.Combine(dir, "settings.json");
        legacySettingsPath = Path.Combine(appData, "DungeonEscape.MonsterEditor", "settings.json");
        settings = Load();
    }

    /// <summary>The absolute path of the last Data folder that was opened.</summary>
    public string? LastDataFolder
    {
        get => settings.LastDataFolder;
        set
        {
            if (string.Equals(settings.LastDataFolder, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            settings.LastDataFolder = value;
            Save();
        }
    }


    private EditorSettings Load()
    {
        try
        {
            var path = File.Exists(settingsPath) ? settingsPath : legacySettingsPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<EditorSettings>(json) ?? new EditorSettings();
            }
        }
        catch
        {
            // Ignore corrupt/unreadable settings and start fresh.
        }

        return new EditorSettings();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonConvert.SerializeObject(settings, SerializerSettings);
            File.WriteAllText(settingsPath, json);
        }
        catch
        {
            // Best-effort persistence; never let a settings write crash the app.
        }
    }

    private sealed class EditorSettings
    {
        public string? LastDataFolder { get; set; }
    }

}
