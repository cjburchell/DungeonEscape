using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeBootstrap : MonoBehaviour
    {
        [SerializeField]
        private TextAsset itemDefinitionsJson;

        [SerializeField]
        private TextAsset questsJson;

        [SerializeField]
        private TextAsset dialogJson;

        private void Awake()
        {
            Debug.Log("Dungeon Escape Unity bootstrap starting.");

            var itemDefinitions = LoadJson<List<ItemDefinition>>(itemDefinitionsJson, "item definitions");
            var quests = LoadJson<List<Quest>>(questsJson, "quests");
            var dialogs = LoadJson<List<Dialog>>(dialogJson, "dialog");

            Debug.Log("Dungeon Escape data loaded. Items: " + Count(itemDefinitions) +
                      ", quests: " + Count(quests) +
                      ", dialog sets: " + Count(dialogs));
        }

        private static T LoadJson<T>(TextAsset asset, string label)
        {
            if (asset == null)
            {
                Debug.LogError("Missing " + label + " JSON TextAsset.");
                return default(T);
            }

            try
            {
                var data = UnityJsonLoader.LoadFromTextAsset<T>(asset);
                Debug.Log("Loaded " + label + " JSON: " + asset.name);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to deserialize " + label + " JSON from " + asset.name + ": " + exception.Message);
                return default(T);
            }
        }

        private static int Count<T>(ICollection<T> values)
        {
            return values == null ? 0 : values.Count;
        }
    }
}
