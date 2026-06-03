using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class TitleMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator TitleMenuOpensAfterSplashIsDismissed()
        {
            yield return UiPlayModeTestHelper.LoadBootScene();
            yield return UiPlayModeTestHelper.DismissSplash();
            yield return UiPlayModeTestHelper.WaitForObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");

            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"), Is.True);
        }

        [UnityTest]
        public IEnumerator ShowCreateMenuSwitchesTitleModeToCreate()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();

            var titleMenu = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            UiPlayModeTestHelper.InvokePrivate(titleMenu, "ShowCreateMenu");
            yield return null;

            var mode = UiPlayModeTestHelper.GetNonPublicPropertyValue(titleMenu, "mode");
            Assert.That(mode.ToString(), Is.EqualTo("Create"));
        }

        [UnityTest]
        public IEnumerator NewGameFlowCreatesPartyAndClosesTitleMenu()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();

            var titleMenu = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            UiPlayModeTestHelper.InvokePrivate(titleMenu, "ShowCreateMenu");
            yield return null;
            UiPlayModeTestHelper.InvokePrivate(titleMenu, "StartCreatedGame");
            yield return UiPlayModeTestHelper.WaitUntil(() => !UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));

            var gameState = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var party = UiPlayModeTestHelper.GetPropertyValue(gameState, "Party");

            Assert.That(gameState, Is.Not.Null);
            Assert.That(party, Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(party, "CurrentMapId"), Is.EqualTo("overworld"));
            Assert.That(((System.Collections.IEnumerable)UiPlayModeTestHelper.GetPropertyValue(party, "ActiveMembers")).Cast<object>().Count(), Is.GreaterThanOrEqualTo(1));
        }
    }
}