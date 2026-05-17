using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.Tests.EditMode
{
    public sealed class GameMenuToolkitViewEditModeTests
    {
        [Test]
        public void BuildSaveRowsCreatesSelectableSaveButtons()
        {
            var root = new VisualElement();
            var rows = CreateArray(
                "Redpoint.DungeonEscape.ViewModels.GameMenuSaveSlotRow, DungeonEscape.Core",
                CreateSaveRow(0, "Quest One", "Level 3"),
                CreateSaveRow(1, "New Save", "Save the current quest."));

            GetToolkitMethod("BuildSaveRows").Invoke(null, new object[] { root, rows, 1, null });

            var buttons = root.Query<Button>().ToList();
            Assert.That(buttons.Select(button => button.text).ToArray(), Is.EqualTo(new[] { "Quest One\nLevel 3", "New Save\nSave the current quest." }));
            Assert.That(buttons[0].ClassListContains("game-menu-save-list__row"), Is.True);
            Assert.That(buttons[1].ClassListContains("game-menu-save-list__row--selected"), Is.True);
        }

        [Test]
        public void BuildModalCreatesTitleMessageAndChoices()
        {
            var root = new VisualElement();

            GetToolkitMethod("BuildModal").Invoke(null, new object[] { root, "Save", "Choose an action.", new[] { "Save", "Cancel" }, 0, null });

            Assert.That(root.Q<Label>("GameMenuModalTitle").text, Is.EqualTo("Save"));
            Assert.That(root.Q<Label>("GameMenuModalMessage").text, Is.EqualTo("Choose an action."));
            var buttons = root.Query<Button>().ToList();
            Assert.That(buttons.Select(button => button.text).ToArray(), Is.EqualTo(new[] { "Save", "Cancel" }));
            Assert.That(buttons[0].ClassListContains("game-menu-modal__choice--selected"), Is.True);
            Assert.That(buttons[1].ClassListContains("game-menu-modal__choice"), Is.True);
        }

        private static System.Reflection.MethodInfo GetToolkitMethod(string methodName)
        {
            var type = Type.GetType("Redpoint.DungeonEscape.Unity.UI.GameMenuToolkitView, Assembly-CSharp", true);
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

        private static object CreateSaveRow(int slotIndex, string title, string summary)
        {
            var rowType = Type.GetType("Redpoint.DungeonEscape.ViewModels.GameMenuSaveSlotRow, DungeonEscape.Core", true);
            var row = Activator.CreateInstance(rowType);
            SetProperty(rowType, row, "SlotIndex", slotIndex);
            SetProperty(rowType, row, "Title", title);
            SetProperty(rowType, row, "Summary", summary);
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
