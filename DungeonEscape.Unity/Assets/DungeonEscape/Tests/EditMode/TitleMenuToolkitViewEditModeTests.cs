using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.Tests.EditMode
{
    public sealed class TitleMenuToolkitViewEditModeTests
    {
        [Test]
        public void BuildMainMenuCreatesButtonsFromRows()
        {
            var root = new VisualElement();
            var rows = CreateArray(
                "Redpoint.DungeonEscape.ViewModels.TitleRow, DungeonEscape.Core",
                CreateTitleRow("New Quest", true, "NewQuest"),
                CreateTitleRow("Quit", true, "Quit"));

            GetToolkitMethod("BuildMainMenu").Invoke(null, new object[] { root, rows, 1, null });

            var buttons = root.Query<Button>().ToList();
            Assert.That(buttons.Select(GetButtonLabelText).ToArray(), Is.EqualTo(new[] { "New Quest", "Quit" }));
            Assert.That(buttons[0].ClassListContains("title-menu__button"), Is.True);
            Assert.That(buttons[1].ClassListContains("title-menu__button--selected"), Is.True);
            Assert.That(root.Q("TitleMenuToolkitMainItems"), Is.Not.Null);
            Assert.That(root.Q(className: "title-menu__items"), Is.Not.Null);
        }

        [Test]
        public void BuildLoadMenuCreatesLoadDeleteAndBackButtons()
        {
            var root = new VisualElement();
            var rows = CreateArray(
                "Redpoint.DungeonEscape.ViewModels.TitleLoadSlotRow, DungeonEscape.Core",
                CreateLoadRow("Quest One\nLevel 3", "Delete", true, false),
                CreateLoadRow("Quest Two\nLevel 4", "Delete", false, true));

            GetToolkitMethod("BuildLoadMenu").Invoke(null, new object[] { root, rows, true, null, null, null });

            var buttons = root.Query<Button>().ToList();
            Assert.That(
                buttons.Select(GetButtonLabelText).ToArray(),
                Is.EqualTo(new[] { "Quest One\nLevel 3", "Delete", "Quest Two\nLevel 4", "Delete", "Back" }));
            Assert.That(buttons[0].ClassListContains("title-load-menu__load--selected"), Is.True);
            Assert.That(buttons[3].ClassListContains("title-load-menu__delete--selected"), Is.True);
            Assert.That(buttons[4].ClassListContains("title-load-menu__back--selected"), Is.True);
            Assert.That(root.Q("TitleMenuToolkitLoadPanel"), Is.Not.Null);
            Assert.That(root.Q(className: "title-load-menu__panel"), Is.Not.Null);
        }

        [Test]
        public void BuildCreateMenuCreatesCreatePanelAndControls()
        {
            var root = new VisualElement();
            var genderType = Type.GetType("Redpoint.DungeonEscape.State.Gender, DungeonEscape.Core", true);
            var classType = Type.GetType("Redpoint.DungeonEscape.State.Class, DungeonEscape.Core", true);

            GetToolkitMethod("BuildCreateMenu").Invoke(
                null,
                new[]
                {
                    root,
                    "Ada",
                    Enum.Parse(genderType, "Female"),
                    Enum.Parse(classType, "Hero"),
                    0,
                    null,
                    new[] { 10, 2, 3, 4, 5, 6 },
                    6,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                });

            Assert.That(root.Q("TitleMenuToolkitCreatePanel"), Is.Not.Null);
            Assert.That(root.Q("TitleMenuToolkitCreateName"), Is.Not.Null);
            Assert.That(root.Query<Button>().ToList().Select(GetButtonLabelText).ToArray(), Does.Contain("Start"));
            Assert.That(root.Query<Label>().ToList().Select(label => label.text).ToArray(), Does.Contain("Stats"));
        }

        private static string GetButtonLabelText(Button button)
        {
            return button.Q<Label>().text;
        }

        private static System.Reflection.MethodInfo GetToolkitMethod(string methodName)
        {
            var type = Type.GetType("Redpoint.DungeonEscape.Unity.UI.TitleMenuToolkitView, Assembly-CSharp", true);
            return type.GetMethod(methodName);
        }

        private static Array CreateArray(string typeName, params object[] values)
        {
            var elementType = Type.GetType(typeName, true);
            var array = Array.CreateInstance(elementType, values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        private static object CreateTitleRow(string label, bool enabled, string actionName)
        {
            var rowType = Type.GetType("Redpoint.DungeonEscape.ViewModels.TitleRow, DungeonEscape.Core", true);
            var actionType = Type.GetType("Redpoint.DungeonEscape.ViewModels.TitleMainAction, DungeonEscape.Core", true);
            var row = Activator.CreateInstance(rowType);
            SetProperty(rowType, row, "Label", label);
            SetProperty(rowType, row, "Enabled", enabled);
            SetProperty(rowType, row, "Action", Enum.Parse(actionType, actionName));
            return row;
        }

        private static object CreateLoadRow(string buttonText, string deleteButtonText, bool loadSelected, bool deleteSelected)
        {
            var rowType = Type.GetType("Redpoint.DungeonEscape.ViewModels.TitleLoadSlotRow, DungeonEscape.Core", true);
            var row = Activator.CreateInstance(rowType);
            SetProperty(rowType, row, "ButtonText", buttonText);
            SetProperty(rowType, row, "DeleteButtonText", deleteButtonText);
            SetProperty(rowType, row, "LoadSelected", loadSelected);
            SetProperty(rowType, row, "DeleteSelected", deleteSelected);
            return row;
        }

        private static void SetProperty(Type type, object target, string name, object value)
        {
            var property = type.GetProperty(name);
            Assert.That(property, Is.Not.Null, "Missing property " + name + " on " + type.FullName);
            property.SetValue(target, value);
        }
    }
}
