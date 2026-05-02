using Newtonsoft.Json;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public static class UnityJsonLoader
    {
        public static T LoadFromResources<T>(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);

            if (asset == null)
            {
                Debug.LogError("Could not load JSON resource: " + resourcePath);
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(asset.text);
        }

        public static T LoadFromTextAsset<T>(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("Cannot load JSON from null TextAsset.");
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(asset.text);
        }
    }
}
