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
            Assert.That(buttons.Select(button => button.text).ToArray(), Is.EqualTo(new[] { "New Quest", "Quit" }));
            Assert.That(buttons[0].ClassListContains("title-menu__button"), Is.True);
            Assert.That(buttons[1].ClassListContains("title-menu__button--selected"), Is.True);
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
                buttons.Select(button => button.text).ToArray(),
                Is.EqualTo(new[] { "Quest One\nLevel 3", "Delete", "Quest Two\nLevel 4", "Delete", "Back" }));
            Assert.That(buttons[0].ClassListContains("title-load-menu__load--selected"), Is.True);
            Assert.That(buttons[3].ClassListContains("title-load-menu__delete--selected"), Is.True);
            Assert.That(buttons[4].ClassListContains("title-load-menu__back--selected"), Is.True);
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
