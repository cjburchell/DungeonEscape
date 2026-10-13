using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using DungeonEscape.Mcp.Security;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class ListScenesTools(ListScenesService service)
{
    [McpServerTool(Name = McpCapabilityNames.ListScenes, ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Lists bounded Unity scene paths and their build-settings status without reading scene contents.")]
    public ListScenesResult ListScenes() => service.List();
}
