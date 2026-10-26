using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class CampaignFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratedCampaignStartsOnItsOwnMapWithoutOriginalStoryContent()
        {
            yield return UiPlayModeTestHelper.StartNewGame();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            var originalSave = state.GetType().GetMethod("CloneGameSave", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new[] { UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave") });
            var catalogField = RuntimeType().GetField("catalog", BindingFlags.NonPublic | BindingFlags.Static);
            var originalCatalog = catalogField.GetValue(null);
            var templateType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("Redpoint.DungeonEscape.Rules.CampaignProjectTemplate")).First(type => type != null);
            var key = "playtest-" + Guid.NewGuid().ToString("N");
            var project = Activator.CreateInstance(templateType, new object[] { key, "Test Adventure", "Generated starter" });
            var campaign = templateType.GetProperty("Campaign").GetValue(project);
            var parent = Path.GetFullPath(Path.Combine(Application.dataPath, "DungeonEscape", "Campaigns")) + Path.DirectorySeparatorChar;
            var directory = Path.GetFullPath(Path.Combine(parent, key));
            Assert.That(directory.StartsWith(parent, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Directory.Exists(directory), Is.False);
            try
            {
                foreach (var entry in (IEnumerable)templateType.GetProperty("Files").GetValue(project))
                {
                    var asset = (string)entry.GetType().GetProperty("Key").GetValue(entry);
                    var path = Path.GetFullPath(Path.Combine(Application.dataPath, asset.Substring("Assets/".Length)));
                    Assert.That(path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, (string)entry.GetType().GetProperty("Value").GetValue(entry));
                }
                var existing = ((IEnumerable)originalCatalog.GetType().GetProperty("Campaigns").GetValue(originalCatalog)).Cast<object>().ToList();
                var installed = Array.CreateInstance(campaign.GetType(), existing.Count + 1);
                for (var index = 0; index < existing.Count; index++) installed.SetValue(existing[index], index);
                installed.SetValue(campaign, existing.Count);
                var defaultCampaign = originalCatalog.GetType().GetProperty("Default").GetValue(originalCatalog);
                catalogField.SetValue(null, Activator.CreateInstance(catalogField.FieldType,
                    new object[] { installed, defaultCampaign.GetType().GetProperty("Id").GetValue(defaultCampaign) }));
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
                UiPlayModeTestHelper.InvokePrivate(title, "ChooseCampaign", existing.Count);
                UiPlayModeTestHelper.InvokePrivate(title, "StartCreatedGame");
                yield return null;
                Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "titleError"), Is.Null);
                var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(save, "CampaignId"), Is.EqualTo("campaign/" + key + "/main"));
                var party = UiPlayModeTestHelper.GetPropertyValue(save, "Party");
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(party, "CurrentMapId"), Is.EqualTo("map/" + key + "/overworld"));
                var position = UiPlayModeTestHelper.GetPropertyValue(party, "CurrentPosition");
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(position, "X"), Is.EqualTo(8f));
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(position, "Y"), Is.EqualTo(8f));
                var context = RuntimeType().GetProperty("Current").GetValue(null);
                var content = UiPlayModeTestHelper.GetPropertyValue(context, "Data");
                Assert.That(((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(content, "Quests")).Cast<object>(), Is.Empty);
                Assert.That(((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(content, "Dialogs")).Cast<object>(), Is.Empty);
                var encounters = state.GetType().GetMethod("GetRandomMonstersForMap", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(state, new object[] { "map/" + key + "/overworld" });
                Assert.That(((IEnumerable)encounters).Cast<object>(), Is.Empty);
            }
            finally
            {
                catalogField.SetValue(null, originalCatalog);
                state.GetType().GetMethod("TryRestoreSave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(state, new[] { originalSave });
                if (directory.StartsWith(parent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
                if (File.Exists(directory + ".meta")) File.Delete(directory + ".meta");
            }
        }

        [UnityTest]
        public IEnumerator EmptyCampaignListCannotEnterCharacterCreation()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            var runtime = RuntimeType();
            var context = runtime.GetProperty("Current").GetValue(null);
            var field = runtime.GetField("catalog", BindingFlags.NonPublic | BindingFlags.Static);
            var original = field.GetValue(null);
            var campaign = ((IEnumerable)original.GetType().GetProperty("Campaigns").GetValue(original)).Cast<object>().First();
            var empty = Activator.CreateInstance(field.FieldType, new object[] { Array.CreateInstance(campaign.GetType(), 0), null });
            try
            {
                field.SetValue(null, empty);
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
                yield return null;
                Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(title, "mode").ToString(), Is.EqualTo("Campaign"));
                Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign"), Is.Null);
                UiPlayModeTestHelper.InvokePrivate(title, "ActivateSelected");
                Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(title, "mode").ToString(), Is.EqualTo("Main"));
                Assert.That(runtime.GetProperty("Current").GetValue(null), Is.SameAs(context));
            }
            finally
            {
                field.SetValue(null, original);
                UiPlayModeTestHelper.InvokePrivate(title, "ShowMainMenu");
            }
        }

        [UnityTest]
        public IEnumerator CancelingCharacterCreationPreservesTheActiveContextAndSave()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var runtime = RuntimeType();
            var context = runtime.GetProperty("Current").GetValue(null);
            var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            var catalogField = runtime.GetField("catalog", BindingFlags.NonPublic | BindingFlags.Static);
            var originalCatalog = catalogField.GetValue(null);
            var campaign = ((IEnumerable)originalCatalog.GetType().GetProperty("Campaigns").GetValue(originalCatalog)).Cast<object>().First();
            var other = Activator.CreateInstance(campaign.GetType());
            foreach (var property in campaign.GetType().GetProperties().Where(property => property.CanWrite))
                property.SetValue(other, property.GetValue(campaign));
            Set(other, "Id", "campaign/second/main");
            Set(other, "Name", "Other Adventure");
            Set(other, "StartMapId", "map/second/world");
            Set(other, "OverworldMapId", "map/second/world");
            foreach (var property in new[] { "ItemsAssetPath", "MapsAssetPath", "MonstersAssetPath", "QuestsAssetPath", "DialogsAssetPath" })
                Set(other, property, "Assets/DungeonEscape/Campaigns/second/Data/" + property + ".json");
            var installed = Array.CreateInstance(campaign.GetType(), 2);
            installed.SetValue(campaign, 0);
            installed.SetValue(other, 1);
            var twoCampaigns = Activator.CreateInstance(catalogField.FieldType, new object[] { installed, campaign.GetType().GetProperty("Id").GetValue(campaign) });
            try
            {
                catalogField.SetValue(null, twoCampaigns);
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
                Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(title, "mode").ToString(), Is.EqualTo("Campaign"));
                Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign"), Is.Null);
                UiPlayModeTestHelper.InvokePrivate(title, "ChooseCampaign", 0);
                Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign"), Is.Not.Null);
                Assert.That(runtime.GetProperty("Current").GetValue(null), Is.SameAs(context));
                UiPlayModeTestHelper.InvokePrivate(title, "ShowMainMenu");
                Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign"), Is.Null);
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave"), Is.SameAs(save));
                Assert.That(runtime.GetProperty("Current").GetValue(null), Is.SameAs(context));
            }
            finally
            {
                catalogField.SetValue(null, originalCatalog);
                UiPlayModeTestHelper.InvokePrivate(title, "ShowMainMenu");
            }
        }

        [UnityTest]
        public IEnumerator MissingCampaignAndIncompatibleSaveDoNotReplaceTheSession()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var context = RuntimeType().GetProperty("Current").GetValue(null);
            var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            var broken = Activator.CreateInstance(save.GetType());
            Set(broken, "CampaignId", "campaign/missing/main");
            Set(broken, "CampaignName", "Missing adventure");
            Set(broken, "CampaignContentVersion", "incompatible-version");
            Set(broken, "Party", UiPlayModeTestHelper.GetPropertyValue(save, "Party"));
            var restore = state.GetType().GetMethod("TryRestoreSave", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(restore.Invoke(state, new[] { broken }), Is.False);
            Assert.That((string)UiPlayModeTestHelper.GetPropertyValue(state, "LastLoadError"), Does.Contain("Missing adventure"));
            Set(broken, "CampaignId", UiPlayModeTestHelper.GetPropertyValue(save, "CampaignId"));
            Assert.That(restore.Invoke(state, new[] { broken }), Is.False);
            Assert.That((string)UiPlayModeTestHelper.GetPropertyValue(state, "LastLoadError"), Does.Contain("version"));
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave"), Is.SameAs(save));
            Assert.That(RuntimeType().GetProperty("Current").GetValue(null), Is.SameAs(context));
        }

        [UnityTest]
        public IEnumerator RestoringASaveActivatesItsContextAndKeepsTheSnapshotIndependent()
        {
            yield return UiPlayModeTestHelper.StartNewGame();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var original = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            var context = RuntimeType().GetProperty("Current").GetValue(null);
            var clone = state.GetType().GetMethod("CloneGameSave", BindingFlags.NonPublic | BindingFlags.Static);
            var snapshot = clone.Invoke(null, new[] { original });
            var restore = state.GetType().GetMethod("TryRestoreSave", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(restore.Invoke(state, new[] { snapshot }), Is.True);
            var restored = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            Assert.That(restored, Is.Not.SameAs(snapshot));
            Assert.That(RuntimeType().GetProperty("Current").GetValue(null), Is.Not.SameAs(context));
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(restored, "CampaignId"), Is.EqualTo("campaign/original/main"));
            var party = UiPlayModeTestHelper.GetPropertyValue(restored, "Party");
            Set(party, "PlayerName", "Changed after loading");
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(UiPlayModeTestHelper.GetPropertyValue(snapshot, "Party"), "PlayerName"), Is.Not.EqualTo("Changed after loading"));
        }

        private static Type RuntimeType() => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Redpoint.DungeonEscape.Unity.Core.CampaignRuntime"))
            .First(type => type != null);

        private static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);
    }
}
