using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Redpoint.DungeonEscape.Unity.Tests.PlayMode
{
    public sealed class StaticEncounterPlayModeTests
    {
        [UnityTest]
        public IEnumerator AllElevenCavesCanBeVisitedAndReturnedFromThroughAuthoredWarps()
        {
            yield return StartBorderlands();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var controller = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.PlayerGridController");
            var view = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.Tiled.View");
            foreach (var cave in new[] { "cave", "cave-b", "cave-c", "cave-d", "cave-e", "cave-f", "cave-g", "cave-h", "cave-i", "cave-j", "cave-k" })
            {
                Travel(controller, cave);
                yield return null;
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentMapId"), Is.EqualTo("map/borderlands/" + cave));
                var position = UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentPosition");
                Assert.That(view.GetType().GetMethod("CanMoveTo").Invoke(view, new object[] {
                    (int)(float)UiPlayModeTestHelper.GetPropertyValue(position, "X"),
                    (int)(float)UiPlayModeTestHelper.GetPropertyValue(position, "Y") }), Is.EqualTo(true));
                Travel(controller, "cave-entrance");
                Assert.That(UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentMapId"), Is.EqualTo("map/borderlands/cave-entrance"));
            }
            Travel(controller, "overworld");
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentMapIsOverWorld"), Is.EqualTo(true));
            RestoreSnapshot(state);
            Travel(controller, "keep");
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentMapIsOverWorld"), Is.EqualTo(false));
        }

        [UnityTest]
        public IEnumerator EntranceGroupFirstPersistsProgressAndReturnsOneReward()
        {
            yield return CompleteTwoGroups(false);
        }

        [UnityTest]
        public IEnumerator StoreGroupFirstPersistsProgressAndReturnsOneReward()
        {
            yield return CompleteTwoGroups(true);
        }

        private static IEnumerator CompleteTwoGroups(bool reverse)
        {
            yield return StartBorderlands();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var controller = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.PlayerGridController");
            var view = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.Tiled.View");
            var warden = ReadObject("keep", "Npc");
            Interact(controller, warden, 0);
            Assert.That(QuestStage(state), Is.EqualTo(1));
            Travel(controller, "cave");
            var entrance = ReadObject("cave", "Monster", "Four kobolds");
            var store = ReadObject("cave", "Monster", "Store-room kobolds");
            var first = reverse ? store : entrance;
            var second = reverse ? entrance : store;
            Assert.That(GroupSprites(view, first).Length, Is.EqualTo(4));
            Assert.That(GroupSprites(view, second).Length, Is.EqualTo(4));
            foreach (var sprite in GroupSprites(view, first).Concat(GroupSprites(view, second)))
                Assert.That(sprite.sprite.bounds.size.x * sprite.transform.localScale.x, Is.LessThanOrEqualTo(1.001f));
            Interact(controller, first, 1);
            Assert.That(CombatOpen(), Is.False);
            DefeatGroup(controller, first);
            Assert.That(IsRemoved(state, first), Is.True);
            Assert.That(IsRemoved(state, second), Is.False);
            Assert.That(GroupSprites(view, first), Is.Empty);
            Assert.That(QuestStage(state), Is.EqualTo(1), "One group is not enough to report success.");
            Assert.That(Objectives(state).Count, Is.EqualTo(1));
            RestoreSnapshot(state);
            Assert.That(Objectives(state).Count, Is.EqualTo(1));
            Assert.That(IsRemoved(state, first), Is.True);
            Assert.That(IsRemoved(state, second), Is.False);
            Travel(controller, "keep");
            Assert.That(WardenText(state), Does.Contain(reverse ? "entrance guards remain" : "store-room guards remain"));
            var partialGold = UiPlayModeTestHelper.GetPropertyValue(Party(state), "Gold");
            Interact(controller, warden, 0);
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(Party(state), "Gold"), Is.EqualTo(partialGold));
            Assert.That(QuestStage(state), Is.EqualTo(1));
            Travel(controller, "cave");
            DefeatGroup(controller, second);
            Assert.That(QuestStage(state), Is.EqualTo(2));
            Assert.That(Objectives(state).Count, Is.EqualTo(2));
            Assert.That(IsRemoved(state, second), Is.True);
            Assert.That(CanMove(view), Is.True);
            RestoreSnapshot(state);
            Assert.That(Objectives(state).Count, Is.EqualTo(2));
            Travel(controller, "keep");
            Assert.That(WardenText(state), Does.Contain("Both kobold groups are cleared"));
            Interact(controller, warden, 1); // Defer collecting the reward.
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(Quest(state), "Completed"), Is.EqualTo(false));
            var gold = Convert.ToInt32(UiPlayModeTestHelper.GetPropertyValue(Party(state), "Gold"));
            var xp = PartyXp(state);
            var heroes = ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(Party(state), "ActiveMembers")).Cast<object>().Count();
            Interact(controller, warden, 0);
            Assert.That(UiPlayModeTestHelper.GetPropertyValue(Quest(state), "Completed"), Is.EqualTo(true));
            Assert.That(Convert.ToInt32(UiPlayModeTestHelper.GetPropertyValue(Party(state), "Gold")), Is.EqualTo(gold + 20));
            Assert.That(PartyXp(state), Is.EqualTo(xp + (ulong)(50 * heroes)));
            Assert.That(WardenText(state), Does.Contain("already received"));
            Interact(controller, warden, 0);
            RestoreSnapshot(state);
            Interact(controller, warden, 0);
            Assert.That(Convert.ToInt32(UiPlayModeTestHelper.GetPropertyValue(Party(state), "Gold")), Is.EqualTo(gold + 20));
            Assert.That(PartyXp(state), Is.EqualTo(xp + (ulong)(50 * heroes)));
            Travel(controller, "cave");
            UiPlayModeTestHelper.InvokePrivate(controller, "StartStaticEncounter", first);
            Assert.That(CombatOpen(), Is.False);
            Assert.That(IsRemoved(state, first) && IsRemoved(state, second), Is.True);
        }

        private static void DefeatGroup(object controller, object group)
        {
            Interact(controller, group, 0);
            var combat = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");
            var monsters = ((IEnumerable)UiPlayModeTestHelper.GetNonPublicFieldValue(combat, "monsters")).Cast<object>().ToList();
            Assert.That(monsters.Count, Is.EqualTo(4));
            foreach (var monster in monsters) Set(UiPlayModeTestHelper.GetPropertyValue(monster, "Instance"), "Health", 0);
            UiPlayModeTestHelper.InvokePrivate(combat, "ChooseNextHeroAction");
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var xp = PartyXp(state);
            UiPlayModeTestHelper.InvokePrivate(combat, "ShowVictoryMessage");
            Assert.That(PartyXp(state), Is.EqualTo(xp));
            UiPlayModeTestHelper.InvokeStatic("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "CloseCurrent");
        }

        private static IList Objectives(object state) => (IList)UiPlayModeTestHelper.GetPropertyValue(Quest(state), "CompletedObjectiveIds");

        private static string WardenText(object state)
        {
            var cache = TypeOf("Redpoint.DungeonEscape.Unity.Core.GameDataCache").GetProperty("Current").GetValue(null);
            var args = new object[] { "dialog/borderlands/warden", Party(state), null };
            Assert.That(cache.GetType().GetMethod("TryGetDialog").Invoke(cache, args), Is.EqualTo(true));
            return (string)UiPlayModeTestHelper.GetPropertyValue(args[2], "Text");
        }

        [UnityTest]
        public IEnumerator EscapedAbortedOrLostFightDoesNotRemoveGroupOrAdvanceQuest()
        {
            yield return StartBorderlands();
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var controller = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Map.PlayerGridController");
            Interact(controller, ReadObject("keep", "Npc"), 0);
            Travel(controller, "cave");
            var group = ReadObject("cave", "Monster");
            Interact(controller, group, 0);
            UiPlayModeTestHelper.InvokeStatic("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "CloseCurrent");
            Assert.That(IsRemoved(state, group), Is.False);
            Assert.That(Objectives(state), Is.Empty);
            Assert.That(QuestStage(state), Is.EqualTo(1));
            Interact(controller, group, 0);
            var combat = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.CombatWindow");
            // Queue a guaranteed escape (no opposing targets for the run check), exercising the normal end-fight message path.
            var action = Activator.CreateInstance(TypeOf("Redpoint.DungeonEscape.Rules.CombatRoundAction"));
            var hero = ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(Party(state), "ActiveMembers")).Cast<object>().First();
            Set(action, "Source", hero);
            Set(action, "State", UiPlayModeTestHelper.GetEnumValue("Redpoint.DungeonEscape.Rules.CombatRoundActionState", "Run"));
            var actions = (IList)UiPlayModeTestHelper.GetNonPublicFieldValue(combat, "roundActions");
            actions.Clear(); actions.Add(action);
            UiPlayModeTestHelper.InvokePrivate(combat, "ResolveNextRoundAction");
            Assert.That((string)UiPlayModeTestHelper.GetNonPublicFieldValue(combat, "messageText"), Does.Contain("got away"));
            UiPlayModeTestHelper.InvokePrivate(combat, "FinishTextReveal");
            UiPlayModeTestHelper.InvokePrivate(combat, "ContinueMessage");
            Assert.That(CombatOpen(), Is.False);
            Assert.That(IsRemoved(state, group), Is.False);
            Assert.That(QuestStage(state), Is.EqualTo(1));
            Interact(controller, group, 0);
            UiPlayModeTestHelper.InvokePrivate(combat, "ShowDefeatMessage");
            UiPlayModeTestHelper.InvokeStatic("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "CloseCurrent");
            Assert.That(IsRemoved(state, group), Is.False);
            RestoreSnapshot(state);
            Assert.That(IsRemoved(state, group), Is.False);
            Assert.That(Objectives(state), Is.Empty);
            Assert.That(QuestStage(state), Is.EqualTo(1));
        }

        private static IEnumerator StartBorderlands()
        {
            yield return UiPlayModeTestHelper.OpenTitleMenu();
            var title = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.TitleMenu");
            UiPlayModeTestHelper.InvokePrivate(title, "ShowCreateMenu");
            var catalog = TypeOf("Redpoint.DungeonEscape.Unity.Core.CampaignRuntime").GetProperty("Catalog").GetValue(null);
            var campaigns = ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(catalog, "Campaigns")).Cast<object>().ToList();
            var index = campaigns.FindIndex(item => (string)UiPlayModeTestHelper.GetPropertyValue(item, "Id") == "campaign/borderlands/main");
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            UiPlayModeTestHelper.InvokePrivate(title, "ChooseCampaign", index);
            UiPlayModeTestHelper.InvokePrivate(title, "StartCreatedGame");
            yield return null;
            Assert.That(UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.TitleMenu", "IsOpen"), Is.False);
        }

        private static void Interact(object controller, object mapObject, int choice)
        {
            UiPlayModeTestHelper.InvokePrivate(controller, "ShowInteractionMessage", mapObject);
            var message = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.UI.MessageBox");
            UiPlayModeTestHelper.InvokePrivate(message, "SelectChoice", choice);
        }

        private static void Travel(object controller, string map)
        {
            var state = UiPlayModeTestHelper.FindObject("Redpoint.DungeonEscape.Unity.Core.GameState");
            var current = ((string)UiPlayModeTestHelper.GetPropertyValue(Party(state), "CurrentMapId")).Split('/').Last();
            var directory = Path.Combine(Application.dataPath, "DungeonEscape/Campaigns/borderlands/Maps");
            var maps = Directory.GetFiles(directory, "*.tmx").ToDictionary(Path.GetFileNameWithoutExtension, XDocument.Load);
            var previous = new Dictionary<string, string> { [current] = null };
            var queue = new Queue<string>(); queue.Enqueue(current);
            while (queue.Count > 0 && !previous.ContainsKey(map))
            {
                var source = queue.Dequeue();
                foreach (var warpObject in maps[source].Descendants("object").Where(item => ((string)item.Attribute("class") ?? (string)item.Attribute("type")) == "Warp"))
                {
                    var target = Property(warpObject, "WarpMap").Split('/').Last();
                    if (!previous.ContainsKey(target)) { previous[target] = source; queue.Enqueue(target); }
                }
            }
            Assert.That(previous.ContainsKey(map), Is.True, "No authored route to " + map);
            var route = new List<string>();
            for (var target = map; previous[target] != null; target = previous[target]) route.Insert(0, target);
            var method = controller.GetType().GetMethod("ApplyMapTransitionImmediate", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var target in route)
            {
                var authored = maps[current].Descendants("object").Single(item => ((string)item.Attribute("class") ?? (string)item.Attribute("type")) == "Warp" && Property(item, "WarpMap") == "map/borderlands/" + target);
                var warp = Activator.CreateInstance(method.GetParameters()[0].ParameterType);
                Set(warp, "MapId", Property(authored, "WarpMap")); Set(warp, "SpawnId", Property(authored, "SpawnId"));
                method.Invoke(controller, new[] { warp });
                current = target;
            }
        }

        private static string Property(XElement mapObject, string name) => (string)mapObject.Element("properties").Elements("property").First(item => (string)item.Attribute("name") == name).Attribute("value");

        private static object ReadObject(string map, string objectClass, string name = null)
        {
            var info = TypeOf("Redpoint.DungeonEscape.Data.TiledMapInfo").GetMethod("Parse").Invoke(null,
                new object[] { File.ReadAllText(Path.Combine(Application.dataPath, "DungeonEscape/Campaigns/borderlands/Maps/" + map + ".tmx")) });
            return ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(info, "ObjectGroups")).Cast<object>()
                .SelectMany(group => ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(group, "Objects")).Cast<object>())
                .First(item => (string)UiPlayModeTestHelper.GetPropertyValue(item, "Class") == objectClass && (name == null || (string)UiPlayModeTestHelper.GetPropertyValue(item, "Name") == name));
        }

        private static void RestoreSnapshot(object state)
        {
            var save = UiPlayModeTestHelper.GetPropertyValue(state, "CurrentSave");
            var snapshot = state.GetType().GetMethod("CloneGameSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { save });
            Assert.That(state.GetType().GetMethod("TryRestoreSave", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(state, new[] { snapshot }), Is.EqualTo(true));
        }

        private static object Party(object state) => UiPlayModeTestHelper.GetPropertyValue(state, "Party");
        private static object Quest(object state) => ((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(Party(state), "ActiveQuests")).Cast<object>().Single();
        private static int QuestStage(object state) => (int)UiPlayModeTestHelper.GetPropertyValue(Quest(state), "CurrentStage");
        private static ulong PartyXp(object state) => (ulong)((IEnumerable)UiPlayModeTestHelper.GetPropertyValue(Party(state), "ActiveMembers")).Cast<object>().Sum(hero => Convert.ToInt64(UiPlayModeTestHelper.GetPropertyValue(hero, "Xp")));
        private static bool IsRemoved(object state, object group) => (bool)state.GetType().GetMethod("IsMapObjectRemoved", new[] { typeof(string), group.GetType() }).Invoke(state, new[] { (object)"map/borderlands/cave", group });
        private static bool CanMove(object view) => (bool)view.GetType().GetMethod("CanMoveTo").Invoke(view, new object[] { 7, 4 });
        private static SpriteRenderer[] GroupSprites(object view, object group) => ((Component)view).GetComponentsInChildren<SpriteRenderer>()
            .Where(sprite => sprite.gameObject.activeInHierarchy && sprite.gameObject.name == "Object_objects_" + (string)UiPlayModeTestHelper.GetPropertyValue(group, "Name")).ToArray();
        private static bool CombatOpen() => UiPlayModeTestHelper.GetStaticBool("Redpoint.DungeonEscape.Unity.UI.CombatWindow", "IsOpen");
        private static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);
        private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
    }
}
