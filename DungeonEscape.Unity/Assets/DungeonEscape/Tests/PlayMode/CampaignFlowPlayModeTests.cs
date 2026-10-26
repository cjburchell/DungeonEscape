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
        public IEnumerator StartingPartyCreatesDistinctEquippedHeroesAndSurvivesSaveRestore()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
            UiPlayModeTestHelper.SelectOriginalCampaign(title);
            var context = UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign");
            var campaign = UiPlayModeTestHelper.GetPropertyValue(context, "Campaign");
            var originalLimit = UiPlayModeTestHelper.GetPropertyValue(campaign, "MaxStartingPartyMembers");
            try
            {
                Set(campaign, "MaxStartingPartyMembers", 2);
                SetPrivateProperty(title, "createPlayerName", "First hero");
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateReviewPage");
                UiPlayModeTestHelper.InvokePrivate(title, "AddStartingMember");
                SetPrivateProperty(title, "createPlayerName", "Second hero");
                SetPrivateProperty(title, "createPlayerClass", UiPlayModeTestHelper.GetEnumValue("Redpoint.DungeonEscape.State.Class", "Wizard"));
                UiPlayModeTestHelper.InvokePrivate(title, "ResetCreateBuildChoices");
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateReviewPage");
                UiPlayModeTestHelper.InvokePrivate(title, "AddStartingMember");
                var completed = (IList)UiPlayModeTestHelper.GetNonPublicFieldValue(title, "completedCharacters");
                Assert.That(completed.Count, Is.EqualTo(1), "Cannot queue another hero at the campaign limit.");
                UiPlayModeTestHelper.InvokePrivate(title, "StartCreatedGame");
                yield return null;
                var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
                AssertStartingParty(save);
                var clone = state.GetType().GetMethod("CloneGameSave", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { save });
                var restored = state.GetType().GetMethod("TryRestoreSave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(state, new[] { clone });
                Assert.That(restored, Is.EqualTo(true));
                AssertStartingParty(UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave"));
            }
            finally { Set(campaign, "MaxStartingPartyMembers", originalLimit); }
        }

        [UnityTest]
        public IEnumerator StartingPartyBackRestoresPreviousDraftAndCancelPreservesSave()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            var active = RuntimeType().GetProperty("Current").GetValue(null);
            UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
            UiPlayModeTestHelper.SelectOriginalCampaign(title);
            var campaign = UiPlayModeTestHelper.GetPropertyValue(UiPlayModeTestHelper.GetNonPublicFieldValue(title, "pendingCampaign"), "Campaign");
            var originalLimit = UiPlayModeTestHelper.GetPropertyValue(campaign, "MaxStartingPartyMembers");
            try
            {
                Set(campaign, "MaxStartingPartyMembers", 3);
                SetPrivateProperty(title, "createPlayerName", "Keep my choices");
                UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateReviewPage");
                var scores = ((int[])UiPlayModeTestHelper.GetNonPublicFieldValue(title, "createAbilityScores")).ToArray();
                UiPlayModeTestHelper.InvokePrivate(title, "AddStartingMember");
                var currentScores = (int[])UiPlayModeTestHelper.GetNonPublicFieldValue(title, "createAbilityScores");
                currentScores[0] = 8;
                UiPlayModeTestHelper.InvokePrivate(title, "ActivateCreateBackAction");
                Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(title, "createPlayerName"), Is.EqualTo("Keep my choices"));
                Assert.That((int[])UiPlayModeTestHelper.GetNonPublicFieldValue(title, "createAbilityScores"), Is.EqualTo(scores));
                UiPlayModeTestHelper.InvokePrivate(title, "ActivateCreateBackAction");
                UiPlayModeTestHelper.InvokePrivate(title, "ActivateCreateBackAction");
                UiPlayModeTestHelper.InvokePrivate(title, "ActivateCreateBackAction");
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave"), Is.SameAs(save));
                Assert.That(RuntimeType().GetProperty("Current").GetValue(null), Is.SameAs(active));
            }
            finally { Set(campaign, "MaxStartingPartyMembers", originalLimit); }
        }

        private static void SetPrivateProperty(object instance, string name, object value) =>
            instance.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);

        private static void AssertStartingParty(object save)
        {
            var party = UiPlayModeTestHelper.GetPropertyValue(save, "Party");
            var heroes = ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(party, "Members")).Cast<object>().ToList();
            Assert.That(heroes.Count, Is.EqualTo(2));
            Assert.That(heroes.Select(hero => UiPlayModeTestHelper.GetPropertyValue(hero, "Name")), Is.EqualTo(new[] { "First hero", "Second hero" }));
            Assert.That(heroes.Select(hero => UiPlayModeTestHelper.GetPropertyValue(hero, "Order")), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(heroes[1], "Class").ToString(), Is.EqualTo("Wizard"));
            foreach (var hero in heroes)
            {
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(hero, "IsActive"), Is.EqualTo(true));
                Assert.That(((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(hero, "Items")).Cast<object>(), Is.Not.Empty);
            }
        }

        [UnityTest]
        public IEnumerator TiledSavedCampaignStartsOnItsOwnSpawnWithoutOriginalStoryContent()
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
                    var contents = (string)entry.GetType().GetProperty("Value").GetValue(entry);
                    if (path.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
                    {
                        contents = contents.Replace("class=\"Spawn\"", "type=\"Spawn\"");
                        Assert.That(contents, Does.Contain("type=\"Spawn\""), "Exercise Tiled's saved object-class spelling.");
                    }
                    File.WriteAllText(path, contents);
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
