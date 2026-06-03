using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class GameMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameMenuOpensIntoMainScreenAfterNewGameStarts()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            var gameMenu = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.GameMenu");
            var menuTab = UiPlayModeTestHelper.GetNestedEnumValue("Redpoint.DungeonEscape.Unity.UI.GameMenu", "MenuTab", "Party");

            UiPlayModeTestHelper.InvokePrivate(gameMenu, "Toggle", menuTab);
            yield return null;

            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.True);
            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(gameMenu, "currentScreen").ToString(), Is.EqualTo("Main"));
            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(gameMenu, "currentTab").ToString(), Is.EqualTo("Party"));
            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(gameMenu, "currentFocus").ToString(), Is.EqualTo("Primary"));
        }

        [UnityTest]
        public IEnumerator GameMenuToggleClosesAfterOpening()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            var gameMenu = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.GameMenu");
            var menuTab = UiPlayModeTestHelper.GetNestedEnumValue("Redpoint.DungeonEscape.Unity.UI.GameMenu", "MenuTab", "Party");

            UiPlayModeTestHelper.InvokePrivate(gameMenu, "Toggle", menuTab);
            yield return null;
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.True);

            UiPlayModeTestHelper.InvokePrivate(gameMenu, "Toggle", menuTab);
            yield return null;
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.False);
        }
    }
}