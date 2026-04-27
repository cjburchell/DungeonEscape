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

            if (itemDefinitionsJson != null)
            {
                Debug.Log("Loaded item definitions JSON: " + itemDefinitionsJson.name);
            }

            if (questsJson != null)
            {
                Debug.Log("Loaded quests JSON: " + questsJson.name);
            }

            if (dialogJson != null)
            {
                Debug.Log("Loaded dialog JSON: " + dialogJson.name);
            }
        }
    }
}
