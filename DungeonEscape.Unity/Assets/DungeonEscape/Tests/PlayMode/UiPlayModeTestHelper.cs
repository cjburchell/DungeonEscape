using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    internal static class UiPlayModeTestHelper
    {
        private const string BootSceneName = "Boot";
        private const float TimeoutSeconds = 10f;

        public static IEnumerator LoadBootScene()
        {
            var operation = SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Single);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        public static IEnumerator OpenTitleMenu()
        {
            yield return LoadBootScene();
            yield return DismissSplash();
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            yield return WaitUntil(() => GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));
        }

        public static IEnumerator StartNewGame()
        {
            yield return OpenTitleMenu();

            var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            InvokePrivate(titleMenu, "ShowCreateMenu");
            yield return null;
            InvokePrivate(titleMenu, "StartCreatedGame");
            yield return WaitUntil(() => !GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"));
        }

        public static IEnumerator DismissSplash()
        {
            yield return WaitForObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen");

            var splash = FindObject("Redpoint.DungeonEscape.Unity.UI.SplashScreen");
            if (splash != null)
            {
                UnityEngine.Object.Destroy(splash);
            }

            yield return WaitUntil(() => !GetStaticBool("Redpoint.DungeonEscape.Unity.UI.SplashScreen", "IsVisible"));
        }

        public static IEnumerator WaitForObject(string typeName)
        {
            yield return WaitUntil(() => FindObject(typeName) != null);
        }

        public static IEnumerator WaitUntil(Func<bool> predicate)
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

        public static UnityEngine.Object FindObject(string typeName)
        {
            var type = GetType(typeName);
            return type == null ? null : Resources.FindObjectsOfTypeAll(type).FirstOrDefault(IsSceneObject);
        }

        public static bool GetStaticBool(string typeName, string propertyName)
        {
            var type = GetType(typeName);
            var property = type == null ? null : type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            return property != null && (bool)property.GetValue(null);
        }

        public static object GetPropertyValue(object instance, string propertyName)
        {
            Assert.That(instance, Is.Not.Null);
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing property " + propertyName + " on " + instance.GetType().FullName + ".");
            return property.GetValue(instance);
        }

        public static object GetNonPublicPropertyValue(object instance, string propertyName)
        {
            Assert.That(instance, Is.Not.Null);
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing non-public property " + propertyName + " on " + instance.GetType().FullName + ".");
            return property.GetValue(instance);
        }

        public static object GetNestedEnumValue(string typeName, string enumName, string valueName)
        {
            var ownerType = GetType(typeName);
            Assert.That(ownerType, Is.Not.Null, "Missing type " + typeName + ".");
            var enumType = ownerType.GetNestedType(enumName, BindingFlags.NonPublic);
            Assert.That(enumType, Is.Not.Null, "Missing nested enum " + enumName + ".");
            return Enum.Parse(enumType, valueName);
        }

        public static void InvokePrivate(object instance, string methodName, params object[] arguments)
        {
            Assert.That(instance, Is.Not.Null);
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing method " + methodName + " on " + instance.GetType().FullName + ".");
            method.Invoke(instance, arguments);
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
    }
}