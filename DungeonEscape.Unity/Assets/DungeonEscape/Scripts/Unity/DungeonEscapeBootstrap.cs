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
        private TextAsset customItemsJson;

        [SerializeField]
        private TextAsset skillsJson;

        [SerializeField]
        private TextAsset spellsJson;

        [SerializeField]
        private TextAsset monstersJson;

        [SerializeField]
        private TextAsset questsJson;

        [SerializeField]
        private TextAsset dialogJson;

        [SerializeField]
        private TextAsset classLevelsJson;

        [SerializeField]
        private TextAsset namesJson;

        [SerializeField]
        private TextAsset statNamesJson;

        public DungeonEscapeDataSet Data { get; private set; }

        private void Awake()
        {
            EnsureCamera();

            Debug.Log("Dungeon Escape Unity bootstrap starting.");

            Data = new DungeonEscapeDataSet
            {
                ItemDefinitions = LoadJson<List<ItemDefinition>>(itemDefinitionsJson, "item definitions"),
                CustomItems = LoadJson<List<Item>>(customItemsJson, "custom items"),
                Skills = LoadJson<List<Skill>>(skillsJson, "skills"),
                Spells = LoadJson<List<Spell>>(spellsJson, "spells"),
                Monsters = LoadJson<List<Monster>>(monstersJson, "monsters"),
                Quests = LoadJson<List<Quest>>(questsJson, "quests"),
                Dialogs = LoadJson<List<Dialog>>(dialogJson, "dialog"),
                ClassLevels = LoadJson<List<ClassStats>>(classLevelsJson, "class levels"),
                Names = LoadJson<Names>(namesJson, "names"),
                StatNames = LoadJson<List<StatName>>(statNamesJson, "stat names")
            };

            Data.Link();

            Debug.Log("Dungeon Escape data loaded. Item definitions: " + Count(Data.ItemDefinitions) +
                      ", custom items: " + Count(Data.CustomItems) +
                      ", skills: " + Count(Data.Skills) +
                      ", spells: " + Count(Data.Spells) +
                      ", monsters: " + Count(Data.Monsters) +
                      ", quests: " + Count(Data.Quests) +
                      ", dialog sets: " + Count(Data.Dialogs) +
                      ", class levels: " + Count(Data.ClassLevels) +
                      ", stat names: " + Count(Data.StatNames));
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

        private static void EnsureCamera()
        {
            if (Camera.main != null || FindObjectOfType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
        }
    }
}
