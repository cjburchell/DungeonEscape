using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.Tests.EditMode
{
    public sealed class ProjectSmokeEditModeTests
    {
        [TestCase("allmonsters.json")]
        [TestCase("../Campaigns/original/Data/maps.json") ]
        [TestCase("class.json")]
        [TestCase("customitems.json")]
        [TestCase("default_settings.json")]
        [TestCase("../Campaigns/original/Data/dialog.json") ]
        [TestCase("magicitems.json")]
        [TestCase("names.json")]
        [TestCase("nonmagicitems.json")]
        [TestCase("../Campaigns/original/Data/quests.json") ]
        [TestCase("../Campaigns/original/Data/items.json") ]
        [TestCase("skills.json")]
        [TestCase("spells.json")]
        [TestCase("species.json")]
        [TestCase("weaponitems.json")]
        [TestCase("armoritems.json")]
        public void RequiredDataFileExists(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "DungeonEscape", "Data", fileName);

            Assert.That(File.Exists(path), Is.True, "Missing required data file: " + path);
        }

        [Test]
        public void BootSceneIsEnabledInBuildSettings()
        {
            Assert.That(
                EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled &&
                    scene.path == "Assets/DungeonEscape/Scenes/Boot.unity"),
                Is.True);
        }
    }
}
