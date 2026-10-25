using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class CampaignFlowPlayModeTests
    {
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
