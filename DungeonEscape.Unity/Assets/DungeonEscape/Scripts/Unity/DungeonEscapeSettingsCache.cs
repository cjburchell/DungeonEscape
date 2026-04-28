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

            return cachedSettings;
        }

        public static void Set(Settings settings)
        {
            cachedSettings = settings ?? new Settings { Version = SettingsFileVersion };
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
            var path = Path.Combine(Application.dataPath, DefaultSettingsAssetPath.Replace("Assets/", ""));
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
