using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class ProjectInfoTools(ProjectInfoService service)
{
    [McpServerTool(Name = "get_project_info", ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Returns DungeonEscape repository metadata; unavailable values are explicitly marked.")]
    public ProjectInfo GetProjectInfo() => service.GetProjectInfo();
}
