using System;
using System.Linq;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class GameSaveFormatter
    {
        public static string GetTitle(GameSave save)
        {
            return IsUsableSave(save) ? save.Name : "Empty";
        }

        public static string GetSummary(GameSave save)
        {
            if (!IsUsableSave(save))
            {
                return "No save data.";
            }

            var time = save.Time.HasValue ? save.Time.Value.ToString("g") : "Unknown time";
            var level = save.Level.HasValue ? "Level " + save.Level.Value : "No level";
            return time + "    " + level;
        }

        public static string FormatLocationName(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return "Unknown";
            }

            var name = mapId.Replace('\\', '/');
            var slashIndex = name.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < name.Length - 1)
            {
                name = name.Substring(slashIndex + 1);
            }

            return string.Join(
                " ",
                name.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part.Substring(1))
                    .ToArray());
        }

        public static bool IsUsableSave(GameSave save)
        {
            return save != null &&
                   save.Party != null &&
                   !string.IsNullOrEmpty(save.Party.CurrentMapId) &&
                   save.Party.CurrentPosition.HasValue;
        }
    }
}
