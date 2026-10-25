using System;
using System.Text.RegularExpressions;

namespace Redpoint.DungeonEscape.Rules
{
    public static class ContentId
    {
        public const string OriginalOverworld = "map/original/overworld";

        public static bool IsValid(string id, string kind = null)
        {
            return id != null && Regex.IsMatch(id, @"^[a-z]+/[a-z0-9]+(?:-[a-z0-9]+)*/[a-z0-9]+(?:-[a-z0-9]+)*$") &&
                   (kind == null || id.StartsWith(kind + "/", StringComparison.Ordinal));
        }

        public static string Create(string kind, string scope)
        {
            var id = kind + "/" + scope + "/" + Guid.NewGuid().ToString("N");
            if (!IsValid(id, kind))
            {
                throw new ArgumentException("Invalid content kind or scope.");
            }
            return id;
        }
    }
}
