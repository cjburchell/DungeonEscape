using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class BootFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator BootSceneCreatesRuntimeRoots()
        {
            yield return UiPlayModeTestHelper.LoadBootScene();
            yield return UiPlayModeTestHelper.WaitForObject("Redpoint.DungeonEscape.Unity.Core.Bootstrap");

            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.Bootstrap"), Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.Tiled.View"), Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.PlayerGridController"), Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen"), Is.Not.Null);
        }
    }
}
