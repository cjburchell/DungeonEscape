using System;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private sealed class MemberDetailMenuScreen : MenuScreenController
        {
            public MemberDetailMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public void Draw(
                string title,
                Action<Hero> drawDetailList,
                Action<Hero> drawDetail)
            {
                var party = Menu.GetParty();
                if (party == null)
                {
                    GUILayout.Label("No party loaded.", Menu.labelStyle);
                    return;
                }

                var members = Menu.GetMenuMembers(party);
                if (members.Count == 0)
                {
                    GUILayout.Label("No party members.", Menu.labelStyle);
                    return;
                }

                Menu.viewModel.ClampSelectedRowIndex(members.Count);
                var hero = members[Menu.selectedRowIndex];
                var scale = Menu.GetPixelScale();
                var panelHeight = Mathf.Max(120f * scale, Menu.GetMenuContentHeight() - 42f * scale);
                var previousMenuBodyHeight = Menu.menuBodyHeight;
                Menu.menuBodyHeight = panelHeight;
                GUILayout.Label(title, Menu.titleStyle);
                GUILayout.Space(6f * scale);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(Menu.panelStyle, GUILayout.Width(230f * scale), GUILayout.Height(panelHeight));
                for (var i = 0; i < members.Count; i++)
                {
                    Menu.DrawMenuMemberRow(members[i], i);
                    Menu.SelectMenuRowOnMouseClick(i, Menu.ActivateCurrentMemberDetailScreen);
                }

                GUILayout.EndVertical();
                GUILayout.Space(10f * scale);

                if (drawDetailList != null)
                {
                    GUILayout.BeginVertical(Menu.panelStyle, GUILayout.Width(310f * scale), GUILayout.Height(panelHeight));
                    drawDetailList(hero);
                    GUILayout.EndVertical();
                    GUILayout.Space(10f * scale);
                }

                var showDetailPanel =
                    !(Menu.currentScreen == MenuScreen.Items && Menu.currentFocus != MenuFocus.Detail) &&
                    !(Menu.currentScreen == MenuScreen.Spells && Menu.currentFocus != MenuFocus.Detail) &&
                    !(Menu.currentScreen == MenuScreen.Abilities && Menu.currentFocus != MenuFocus.Detail) &&
                    !(Menu.currentScreen == MenuScreen.Equipment && Menu.currentFocus == MenuFocus.Primary);
                if (showDetailPanel)
                {
                    GUILayout.BeginVertical(Menu.panelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(panelHeight));
                    if (drawDetail != null)
                    {
                        drawDetail(hero);
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
                Menu.menuBodyHeight = previousMenuBodyHeight;
            }
        }
    }
}
