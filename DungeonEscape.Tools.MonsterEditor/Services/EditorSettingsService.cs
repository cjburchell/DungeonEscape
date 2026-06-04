using System.IO;
using Newtonsoft.Json;

namespace DungeonEscape.Tools.MonsterEditor.Services;

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
    private EditorSettings settings;

    public EditorSettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DungeonEscape.MonsterEditor");
        settingsPath = Path.Combine(dir, "settings.json");
        settings = Load();
    }

    /// <summary>The absolute path of the last file that was opened or saved.</summary>
    public string? LastFilePath
    {
        get => settings.LastFilePath;
        set
        {
            if (string.Equals(settings.LastFilePath, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            settings.LastFilePath = value;
            Save();
        }
    }

    private EditorSettings Load()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
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
        public string? LastFilePath { get; set; }
    }
}
