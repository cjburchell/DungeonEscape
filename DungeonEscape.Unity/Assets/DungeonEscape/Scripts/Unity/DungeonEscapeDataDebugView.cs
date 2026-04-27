using System.Linq;
using System.Text;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeDataDebugView : MonoBehaviour
    {
        [SerializeField]
        private DungeonEscapeBootstrap bootstrap;

        private string outputText;
        private GUIStyle textStyle;

        private void Start()
        {
            if (bootstrap == null)
            {
                bootstrap = FindObjectOfType<DungeonEscapeBootstrap>();
            }

            if (bootstrap == null || bootstrap.Data == null)
            {
                outputText = "Dungeon Escape data has not loaded.";
                return;
            }

            outputText = BuildSummary(bootstrap.Data);
        }

        private void OnGUI()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    normal = { textColor = Color.white },
                    wordWrap = true
                };
            }

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.Label(new Rect(24, 24, Screen.width - 48, Screen.height - 48), outputText ?? "Loading Dungeon Escape data...", textStyle);
        }

        private static string BuildSummary(DungeonEscapeDataSet data)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Dungeon Escape Data");
            builder.AppendLine();
            builder.AppendLine("Item definitions: " + Count(data.ItemDefinitions));
            builder.AppendLine("Custom items: " + Count(data.CustomItems));
            builder.AppendLine("Skills: " + Count(data.Skills));
            builder.AppendLine("Spells: " + Count(data.Spells));
            builder.AppendLine("Monsters: " + Count(data.Monsters));
            builder.AppendLine("Quests: " + Count(data.Quests));
            builder.AppendLine("Dialog sets: " + Count(data.Dialogs));
            builder.AppendLine("Class levels: " + Count(data.ClassLevels));
            builder.AppendLine("Stat names: " + Count(data.StatNames));
            builder.AppendLine();

            AppendSamples(builder, "Items", data.CustomItems == null ? null : data.CustomItems.Select(item => item.Name).Take(5));
            AppendSamples(builder, "Quests", data.Quests == null ? null : data.Quests.Select(quest => quest.Name).Take(5));
            AppendSamples(builder, "Monsters", data.Monsters == null ? null : data.Monsters.Select(monster => monster.Name).Take(5));

            return builder.ToString();
        }

        private static void AppendSamples(StringBuilder builder, string label, System.Collections.Generic.IEnumerable<string> values)
        {
            builder.AppendLine(label + ":");

            if (values == null)
            {
                builder.AppendLine("  none");
                return;
            }

            var any = false;
            foreach (var value in values)
            {
                any = true;
                builder.AppendLine("  " + value);
            }

            if (!any)
            {
                builder.AppendLine("  none");
            }
        }

        private static int Count<T>(System.Collections.Generic.ICollection<T> values)
        {
            return values == null ? 0 : values.Count;
        }
    }
}
