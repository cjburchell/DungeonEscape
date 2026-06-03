using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class CombatWindowPlayModeTests
    {
        [UnityTest]
        public IEnumerator CombatOpenShowsEncounterMessageAndBlocksAutosave()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            OpenCombatWithLoadedMonsters(1);
            yield return null;

            var combatWindow = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");
            Assert.That(combatWindow, Is.Not.Null);
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "IsOpen"), Is.True);
            Assert.That((bool)UiPlayModeTestHelper.GetStaticPropertyValue("Redpoint.DungeonEscape.Unity.Core.GameState", "AutoSaveBlocked"), Is.True);
            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(combatWindow, "state").ToString(), Is.EqualTo("Message"));
            Assert.That((string)UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "messageText"), Does.StartWith("You have encountered"));
        }

        [UnityTest]
        public IEnumerator CombatEncounterMessageContinuesToActionSelection()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            OpenCombatWithLoadedMonsters(1);
            var combatWindow = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");

            UiPlayModeTestHelper.InvokePrivate(combatWindow, "FinishTextReveal");
            UiPlayModeTestHelper.InvokePrivate(combatWindow, "ContinueMessage");
            yield return null;

            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(combatWindow, "state").ToString(), Is.EqualTo("ChooseAction"));
            Assert.That((string)UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "messageText"), Does.EndWith("'s action."));
            Assert.That(UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "actingHero"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CombatTargetSelectionCanReturnToActionMenu()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            OpenCombatWithLoadedMonsters(2);
            var combatWindow = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");
            AdvanceEncounterMessageToActionSelection(combatWindow);

            UiPlayModeTestHelper.InvokePrivate(combatWindow, "BeginTargetSelection");
            yield return null;

            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(combatWindow, "state").ToString(), Is.EqualTo("ChooseTarget"));
            Assert.That(((ICollection)UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "targetSelectionCandidates")).Count, Is.GreaterThan(1));

            UiPlayModeTestHelper.InvokePrivate(combatWindow, "ReturnToActionMenu");
            yield return null;

            Assert.That(UiPlayModeTestHelper.GetNonPublicPropertyValue(combatWindow, "state").ToString(), Is.EqualTo("ChooseAction"));
            Assert.That(((ICollection)UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "targetSelectionCandidates")).Count, Is.EqualTo(0));
            Assert.That((string)UiPlayModeTestHelper.GetNonPublicFieldValue(combatWindow, "messageText"), Does.EndWith("'s turn."));
        }

        [UnityTest]
        public IEnumerator CombatCloseClearsOpenStateAndAutosaveBlock()
        {
            yield return UiPlayModeTestHelper.StartNewGame();

            OpenCombatWithLoadedMonsters(1);
            var combatWindow = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "IsOpen"), Is.True);
            Assert.That((bool)UiPlayModeTestHelper.GetStaticPropertyValue("Redpoint.DungeonEscape.Unity.Core.GameState", "AutoSaveBlocked"), Is.True);

            UiPlayModeTestHelper.InvokePrivate(combatWindow, "Close", false);
            yield return null;

            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "IsOpen"), Is.False);
            Assert.That((bool)UiPlayModeTestHelper.GetStaticPropertyValue("Redpoint.DungeonEscape.Unity.Core.GameState", "AutoSaveBlocked"), Is.False);
        }

        private static void AdvanceEncounterMessageToActionSelection(UnityEngine.Object combatWindow)
        {
            UiPlayModeTestHelper.InvokePrivate(combatWindow, "FinishTextReveal");
            UiPlayModeTestHelper.InvokePrivate(combatWindow, "ContinueMessage");
        }

        private static void OpenCombatWithLoadedMonsters(int count)
        {
            var monsters = GetLoadedMonsters(count);
            var biome = UiPlayModeTestHelper.GetEnumValue("Redpoint.DungeonEscape.State.Biome", "Grassland");
            UiPlayModeTestHelper.InvokeStatic("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "Open", monsters, biome);
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "IsOpen"), Is.True, "Combat did not open. Ensure the boot data includes encounter monsters.");
        }

        private static object GetLoadedMonsters(int count)
        {
            var bootstrap = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.Bootstrap");
            var data = UiPlayModeTestHelper.GetPropertyValue(bootstrap, "Data");
            Assert.That(data, Is.Not.Null);

            var monsters = ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(data, "Monsters"))
                .Cast<object>()
                .Where(monster => monster != null && !string.IsNullOrEmpty((string)UiPlayModeTestHelper.GetPropertyValue(monster, "Name")))
                .Take(count)
                .ToList();
            Assert.That(monsters.Count, Is.EqualTo(count), "Not enough loaded monsters for combat regression setup.");
            return CreateTypedList(monsters);
        }

        private static object CreateTypedList(IList<object> values)
        {
            Assert.That(values.Count, Is.GreaterThan(0), "Cannot create a typed list without values.");
            var listType = typeof(List<>).MakeGenericType(values[0].GetType());
            var list = (IList)System.Activator.CreateInstance(listType);
            foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }
    }
}