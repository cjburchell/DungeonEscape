using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class BootFlowPlayModeTests
    {
        private const string BootSceneName = "Boot";
        private const float TimeoutSeconds = 10f;

        [UnityTest]
        public IEnumerator BootSceneCreatesRuntimeRoots()
        {
            yield return LoadBootScene();
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.Core.Bootstrap");

            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(FindObject("Redpoint.DungeonEscape.Unity.Core.Bootstrap"), Is.Not.Null);
            Assert.That(FindObject("Redpoint.DungeonEscape.Unity.Map.Tiled.View"), Is.Not.Null);
            Assert.That(FindObject("Redpoint.DungeonEscape.Unity.Map.PlayerGridController"), Is.Not.Null);
            Assert.That(FindObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TitleMenuOpensAfterSplashIsDismissed()
        {
            yield return LoadBootScene();
            yield return DismissSplash();
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");

            Assert.That(GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"), Is.True);
        }

        [UnityTest]
        public IEnumerator NewGameFlowCreatesPartyAndClosesTitleMenu()
        {
            yield return LoadBootScene();
            yield return OpenTitleMenu();

            var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            InvokePrivate(titleMenu, "ShowCreateMenu");
            yield return null;
            InvokePrivate(titleMenu, "StartCreatedGame");
            yield return WaitUntil(() => !GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));

            var gameState = FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var party = GetPropertyValue(gameState, "Party");

            Assert.That(gameState, Is.Not.Null);
            Assert.That(party, Is.Not.Null);
            Assert.That(GetPropertyValue(party, "CurrentMapId"), Is.EqualTo("overworld"));
            Assert.That(((IEnumerable)GetPropertyValue(party, "ActiveMembers")).Cast<object>().Count(), Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator GameMenuOpensAndClosesAfterNewGameStarts()
        {
            yield return LoadBootScene();
            yield return StartNewGame();

            var gameMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.GameMenu");
            var menuTab = GetNestedEnumValue("Redpoint.DungeonEscape.Unity.UI.GameMenu", "MenuTab", "Party");

            InvokePrivate(gameMenu, "Toggle", menuTab);
            Assert.That(GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.True);

            InvokePrivate(gameMenu, "Toggle", menuTab);
            Assert.That(GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.False);
        }

        private static IEnumerator StartNewGame()
        {
            yield return OpenTitleMenu();

            var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            InvokePrivate(titleMenu, "ShowCreateMenu");
            yield return null;
            InvokePrivate(titleMenu, "StartCreatedGame");
            yield return WaitUntil(() => !GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));
        }

        private static IEnumerator OpenTitleMenu()
        {
            yield return LoadBootScene();
            yield return DismissSplash();
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            yield return WaitUntil(() => GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));
        }

        private static IEnumerator LoadBootScene()
        {
            var operation = SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Single);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static IEnumerator DismissSplash()
        {
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen");

            var splash = FindObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen");
            if (splash != null)
            {
                UnityEngine.Object.Destroy(splash);
            }

            yield return WaitUntil(() => !GetStaticBool("Redpoint.DungeonEscape.Unity.UI.SplashScreen", "IsVisible"));
        }

        private static IEnumerator WaitForObject(string typeName)
        {
            yield return WaitUntil(() => FindObject(typeName) != null);
        }

        private static IEnumerator WaitUntil(Func<bool> predicate)
        {
            var start = Time.realtimeSinceStartup;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup - start > TimeoutSeconds)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }

                yield return null;
            }
        }

        private static UnityEngine.Object FindObject(string typeName)
        {
            var type = GetType(typeName);
            return type == null ? null : Resources.FindObjectsOfTypeAll(type).FirstOrDefault(IsSceneObject);
        }

        private static bool IsSceneObject(UnityEngine.Object item)
        {
            var component = item as Component;
            if (component != null)
            {
                return component.gameObject.scene.IsValid();
            }

            var gameObject = item as GameObject;
            return gameObject != null && gameObject.scene.IsValid();
        }

        private static Type GetType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static bool GetStaticBool(string typeName, string propertyName)
        {
            var type = GetType(typeName);
            var property = type == null ? null : type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            return property != null && (bool)property.GetValue(null);
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            Assert.That(instance, Is.Not.Null);
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing property " + propertyName + " on " + instance.GetType().FullName + ".");
            return property.GetValue(instance);
        }

        private static object GetNestedEnumValue(string typeName, string enumName, string valueName)
        {
            var ownerType = GetType(typeName);
            Assert.That(ownerType, Is.Not.Null, "Missing type " + typeName + ".");
            var enumType = ownerType.GetNestedType(enumName, BindingFlags.NonPublic);
            Assert.That(enumType, Is.Not.Null, "Missing nested enum " + enumName + ".");
            return Enum.Parse(enumType, valueName);
        }

        private static void InvokePrivate(object instance, string methodName, params object[] arguments)
        {
            Assert.That(instance, Is.Not.Null);
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing method " + methodName + " on " + instance.GetType().FullName + ".");
            method.Invoke(instance, arguments);
        }
    }
}
