using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class SearchCodeTools(SearchCodeService service)
{
    [McpServerTool(Name = "search_code", ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Searches approved DungeonEscape source and documentation roots for bounded literal text matches.")]
    public SearchCodeResult SearchCode(
        [Description("Required literal text, at most 128 characters.")] string query,
        [Description("Optional repository-relative file or subtree within an approved root.")] string? path = null,
        [Description("Optional result limit from 1 to 50; defaults to 20.")] int? maxResults = null) =>
        service.Search(query, path, maxResults);
}
