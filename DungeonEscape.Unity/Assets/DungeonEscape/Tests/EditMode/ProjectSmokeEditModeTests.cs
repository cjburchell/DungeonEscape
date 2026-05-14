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
        [TestCase("classlevels.json")]
        [TestCase("customitems.json")]
        [TestCase("default_settings.json")]
        [TestCase("dialog.json")]
        [TestCase("itemdef.json")]
        [TestCase("names.json")]
        [TestCase("quests.json")]
        [TestCase("skills.json")]
        [TestCase("spells.json")]
        [TestCase("statnames.json")]
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
