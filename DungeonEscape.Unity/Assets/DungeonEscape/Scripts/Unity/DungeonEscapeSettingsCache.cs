using System;
using System.IO;
using Newtonsoft.Json;
using Redpoint.DungeonEscape;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class DungeonEscapeSettingsCache
    {
        private const string SettingsFileVersion = "1.0";
        private const string DefaultSettingsAssetPath = "Assets/DungeonEscape/Data/default_settings.json";

        private static Settings cachedSettings;

        public static Settings Current
        {
            get
            {
                if (cachedSettings == null)
                {
                    Load();
                }

                return cachedSettings;
            }
        }

        public static Settings Load()
        {
            cachedSettings = LoadAppDataSettings();
            if (cachedSettings == null || cachedSettings.Version != SettingsFileVersion)
            {
                cachedSettings = LoadDefaultSettings();
            }

            if (cachedSettings == null)
            {
                cachedSettings = new Settings { Version = SettingsFileVersion };
            }

            DungeonEscapeInput.EnsureBindings(cachedSettings);
            return cachedSettings;
        }

        public static void Set(Settings settings)
        {
            cachedSettings = settings ?? new Settings { Version = SettingsFileVersion };
        }

        public static void Save()
        {
            if (cachedSettings == null)
            {
                Load();
            }

            if (cachedSettings == null)
            {
                return;
            }

            cachedSettings.Version = SettingsFileVersion;
            var path = GetSettingsFilePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(
                    cachedSettings,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        private static Settings LoadAppDataSettings()
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load settings from " + path + ": " + exception.Message);
                return null;
            }
        }

        private static Settings LoadDefaultSettings()
        {
            var path = UnityAssetPath.ToRuntimePath(DefaultSettingsAssetPath);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load default settings from " + path + ": " + exception.Message);
                return null;
            }
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Redpoint",
                "DungeonEscape",
                "settings.json");
        }
    }
}
