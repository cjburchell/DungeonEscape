using Redpoint.DungeonEscape.Data;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private sealed class QuestMenuScreen : MenuScreenController
        {
            public QuestMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                var party = Menu.GetParty();
                return party == null || party.ActiveQuests == null ? 0 : party.ActiveQuests.Count;
            }

            public override void Draw()
            {
                var party = Menu.GetParty();
                if (party == null)
                {
                    GUILayout.Label("No party loaded.", Menu.labelStyle);
                    return;
                }

                if (party.ActiveQuests == null || party.ActiveQuests.Count == 0)
                {
                    GUILayout.Label("No active quests.", Menu.labelStyle);
                    return;
                }

                Menu.viewModel.ClampSelectedRowIndex(party.ActiveQuests.Count);
                GUILayout.BeginHorizontal();
                var scale = Menu.GetPixelScale();
                GUILayout.BeginVertical(GUILayout.MinWidth(420f * scale));
                Menu.questScrollPosition = Menu.BeginThemedScroll(Menu.questScrollPosition, Menu.menuBodyHeight);
                for (var i = 0; i < party.ActiveQuests.Count; i++)
                {
                    var activeQuest = party.ActiveQuests[i];
                    Quest quest;
                    if (GameDataCache.Current == null ||
                        !GameDataCache.Current.TryGetQuest(activeQuest.Id, out quest))
                    {
                        Menu.BeginSelectableRow();
                        GUILayout.Label(activeQuest.Id + (activeQuest.Completed ? " (Finished)" : ""), Menu.labelStyle);
                        EndSelectableRow();
                        Menu.SelectRowOnMouseClick(i);
                        continue;
                    }

                    Menu.BeginSelectableRow();
                    GUILayout.BeginVertical();
                    GUILayout.Label(quest.Name + (activeQuest.Completed ? " (Finished)" : ""), Menu.labelStyle);
                    var currentStage = quest.Stages == null
                        ? null
                        : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                    if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                    {
                        GUILayout.Label(currentStage.Description, Menu.smallStyle);
                    }

                    GUILayout.EndVertical();
                    EndSelectableRow();
                    Menu.SelectRowOnMouseClick(i);
                }
                EndThemedScroll();
                GUILayout.EndVertical();
                GUILayout.Space(10f * scale);
                DrawQuestDetail(party.ActiveQuests[Menu.selectedRowIndex], Menu.menuBodyHeight);
                GUILayout.EndHorizontal();
            }

            private void DrawQuestDetail(ActiveQuest activeQuest, float height)
            {
                GUILayout.BeginVertical(Menu.panelStyle, GUILayout.Width(360f * Menu.GetPixelScale()), GUILayout.Height(height));
                if (activeQuest == null)
                {
                    GUILayout.Label("No quest selected.", Menu.labelStyle);
                    GUILayout.EndVertical();
                    return;
                }

                Quest quest = null;
                if (GameDataCache.Current != null)
                {
                    GameDataCache.Current.TryGetQuest(activeQuest.Id, out quest);
                }

                GUILayout.Label(quest == null ? activeQuest.Id : quest.Name, Menu.titleStyle);
                GUILayout.Label(activeQuest.Completed ? "Completed" : "Active", Menu.labelStyle);
                GUILayout.Label("Current Stage: " + activeQuest.CurrentStage, Menu.smallStyle);

                if (quest == null)
                {
                    GUILayout.Label("Quest data was not found.", Menu.smallStyle);
                    GUILayout.EndVertical();
                    return;
                }

                if (!string.IsNullOrEmpty(quest.Description))
                {
                    GUILayout.Space(8f * Menu.GetPixelScale());
                    GUILayout.Label(quest.Description, Menu.smallStyle);
                }

                var currentStage = quest.Stages == null
                    ? null
                    : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                {
                    GUILayout.Space(8f * Menu.GetPixelScale());
                    GUILayout.Label("Stage", Menu.labelStyle);
                    GUILayout.Label(currentStage.Description, Menu.smallStyle);
                }

                GUILayout.Space(8f * Menu.GetPixelScale());
                GUILayout.Label("Rewards", Menu.labelStyle);
                GUILayout.Label("XP: " + quest.Xp + "    Gold: " + quest.Gold, Menu.smallStyle);
                if (quest.Items != null && quest.Items.Count > 0)
                {
                    GUILayout.Label("Items: " + string.Join(", ", quest.Items.ToArray()), Menu.smallStyle);
                }

                if (quest.Stages != null && quest.Stages.Count > 0)
                {
                    GUILayout.Space(8f * Menu.GetPixelScale());
                    GUILayout.Label("Stages", Menu.labelStyle);
                    foreach (var stage in quest.Stages.OrderBy(stage => stage.Number))
                    {
                        var marker = stage.Number == activeQuest.CurrentStage ? "> " : "  ";
                        GUILayout.Label(marker + stage.Number + ": " + stage.Description, Menu.smallStyle);
                    }
                }

                GUILayout.EndVertical();
            }
        }
    }
}
