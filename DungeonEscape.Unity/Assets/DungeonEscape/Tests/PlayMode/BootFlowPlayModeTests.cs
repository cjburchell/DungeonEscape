using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class BootFlowPlayModeTests
    {
        private const string BootSceneName = "Boot";
        private const float TimeoutSeconds = 10f;
        private float _previousAudioVolume;

        [SetUp]
        public void MuteAudio()
        {
            _previousAudioVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [TearDown]
        public void RestoreAudio()
        {
            AudioListener.volume = _previousAudioVolume;
        }

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

        [UnityTest]
        public IEnumerator TitleMenuToolkitPreviewCanBeEnabledWithoutReplacingImguiFlow()
        {
            try
            {
                yield return OpenTitleMenu();
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitPreview", true);
                yield return null;

                var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu") as Component;
                Assert.That(titleMenu, Is.Not.Null);
                Assert.That(GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"), Is.True);
                InvokePrivate(titleMenu, "DrawToolkitPreview");
                yield return null;

                var document = titleMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var preview = document.rootVisualElement.Q("TitleMenuToolkitPreview");
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(preview.Query<Button>().ToList().Count, Is.GreaterThan(0));
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitPreview", false);
            }
        }

        [UnityTest]
        public IEnumerator TitleMenuToolkitMainRendererCanBeEnabledForMainMenu()
        {
            SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitMainMenuRenderer", true);
            try
            {
                yield return OpenTitleMenu();

                var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu") as Component;
                Assert.That(titleMenu, Is.Not.Null);
                InvokePrivate(titleMenu, "DrawToolkitMainMenuRenderer");
                yield return null;

                var document = titleMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var root = document.rootVisualElement.Q("TitleMenuToolkitPreview");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.ClassListContains("title-menu-toolkit-active"), Is.True);
                Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Position));
                Assert.That(root.Query<Button>().ToList().Count, Is.GreaterThan(0));
                Assert.That(root.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(root.style.height.value.value, Is.GreaterThan(0f));
                var menuItems = root.Q("TitleMenuToolkitMainItems");
                Assert.That(menuItems, Is.Not.Null);
                Assert.That(menuItems.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(menuItems.style.height.value.value, Is.GreaterThan(0f));
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitMainMenuRenderer", false);
            }
        }

        [UnityTest]
        public IEnumerator TitleMenuToolkitLoadRendererCanBeEnabledForLoadMenu()
        {
            SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitLoadRenderer", true);
            try
            {
                yield return OpenTitleMenu();

                var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu") as Component;
                Assert.That(titleMenu, Is.Not.Null);
                InvokePrivate(titleMenu, "ShowLoadMenu");
                InvokePrivate(titleMenu, "DrawToolkitLoadMenuRenderer");
                yield return null;

                var document = titleMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var root = document.rootVisualElement.Q("TitleMenuToolkitPreview");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.ClassListContains("title-menu-toolkit-load-active"), Is.True);
                Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Position));
                Assert.That(root.Query<Button>().ToList().Any(button => GetButtonLabelText(button) == "Back"), Is.True);
                Assert.That(root.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(root.style.height.value.value, Is.GreaterThan(0f));
                var loadPanel = root.Q("TitleMenuToolkitLoadPanel");
                Assert.That(loadPanel, Is.Not.Null);
                Assert.That(loadPanel.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(loadPanel.style.height.value.value, Is.GreaterThan(0f));
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitLoadRenderer", false);
            }
        }

        [UnityTest]
        public IEnumerator TitleMenuToolkitCreateRendererCanBeEnabledForNewQuest()
        {
            SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitCreateRenderer", true);
            try
            {
                yield return OpenTitleMenu();

                var titleMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu") as Component;
                Assert.That(titleMenu, Is.Not.Null);
                InvokePrivate(titleMenu, "ShowCreateMenu");
                InvokePrivate(titleMenu, "DrawToolkitCreateMenuRenderer");
                yield return null;

                var document = titleMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var root = document.rootVisualElement.Q("TitleMenuToolkitPreview");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.ClassListContains("title-menu-toolkit-active"), Is.True);
                Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Position));
                Assert.That(root.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(root.style.height.value.value, Is.GreaterThan(0f));
                var createPanel = root.Q("TitleMenuToolkitCreatePanel");
                Assert.That(createPanel, Is.Not.Null);
                Assert.That(createPanel.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(createPanel.style.height.value.value, Is.GreaterThan(0f));
                Assert.That(root.Query<Button>().ToList().Any(button => GetButtonLabelText(button) == "Start"), Is.True);
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "UseToolkitCreateRenderer", false);
            }
        }

        [UnityTest]
        public IEnumerator GameMenuToolkitPreviewDoesNotRenderPlaceholderForMainMenu()
        {
            try
            {
                yield return LoadBootScene();
                yield return StartNewGame();
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "UseToolkitPreview", true);

                var gameMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.GameMenu") as Component;
                Assert.That(gameMenu, Is.Not.Null);
                var menuTab = GetNestedEnumValue("Redpoint.DungeonEscape.Unity.UI.GameMenu", "MenuTab", "Party");
                InvokePrivate(gameMenu, "Toggle", menuTab);
                InvokePrivate(gameMenu, "DrawToolkitPreview");
                yield return null;

                Assert.That(GetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "IsOpen"), Is.True);
                var document = gameMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var preview = document.rootVisualElement.Q("GameMenuToolkitPreview");
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "UseToolkitPreview", false);
            }
        }

        [UnityTest]
        public IEnumerator GameMenuToolkitModalRendererCanBeEnabledForMenuModal()
        {
            try
            {
                yield return LoadBootScene();
                yield return StartNewGame();
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "UseToolkitModalRenderer", true);

                var gameMenu = FindObject("Redpoint.DungeonEscape.Unity.UI.GameMenu") as Component;
                Assert.That(gameMenu, Is.Not.Null);
                var viewModel = GetFieldValue(gameMenu, "viewModel");
                InvokePublic(
                    viewModel,
                    "ShowModal",
                    "Confirm",
                    "Use the selected item?",
                    new[] { "Use", "Cancel" },
                    null,
                    false);
                InvokePrivate(gameMenu, "DrawToolkitMenuModalOverlay");
                yield return null;

                var document = gameMenu.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var root = document.rootVisualElement.Q("GameMenuToolkitPreview");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.ClassListContains("game-menu-toolkit-modal-active"), Is.True);
                Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Position));
                Assert.That(root.Query<Button>().ToList().Select(GetButtonLabelText).ToArray(), Is.EqualTo(new[] { "Use", "Cancel" }));
            }
            finally
            {
                SetStaticBool("Redpoint.DungeonEscape.Unity.UI.GameMenu", "UseToolkitModalRenderer", false);
            }
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

        private static void SetStaticBool(string typeName, string propertyName, bool value)
        {
            var type = GetType(typeName);
            var property = type == null ? null : type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null, "Missing static property " + propertyName + " on " + typeName + ".");
            property.SetValue(null, value);
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            Assert.That(instance, Is.Not.Null);
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing property " + propertyName + " on " + instance.GetType().FullName + ".");
            return property.GetValue(instance);
        }

        private static object GetFieldValue(object instance, string fieldName)
        {
            Assert.That(instance, Is.Not.Null);
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "Missing field " + fieldName + " on " + instance.GetType().FullName + ".");
            return field.GetValue(instance);
        }

        private static object GetNestedEnumValue(string typeName, string enumName, string valueName)
        {
            var ownerType = GetType(typeName);
            Assert.That(ownerType, Is.Not.Null, "Missing type " + typeName + ".");
            var enumType = ownerType.GetNestedType(enumName, BindingFlags.NonPublic);
            Assert.That(enumType, Is.Not.Null, "Missing nested enum " + enumName + ".");
            return Enum.Parse(enumType, valueName);
        }

        private static string GetButtonLabelText(Button button)
        {
            var label = button == null ? null : button.Q<Label>();
            return label == null ? string.Empty : label.text;
        }

        private static void InvokePrivate(object instance, string methodName, params object[] arguments)
        {
            Assert.That(instance, Is.Not.Null);
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing method " + methodName + " on " + instance.GetType().FullName + ".");
            method.Invoke(instance, arguments);
        }

        private static void InvokePublic(object instance, string methodName, params object[] arguments)
        {
            Assert.That(instance, Is.Not.Null);
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing method " + methodName + " on " + instance.GetType().FullName + ".");
            method.Invoke(instance, arguments);
        }
    }
}
