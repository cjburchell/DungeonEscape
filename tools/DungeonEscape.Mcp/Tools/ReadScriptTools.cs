using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class ReadScriptTools(ReadScriptService service)
{
    [McpServerTool(Name = "read_script", ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Reads a bounded line range from one approved DungeonEscape source or documentation file.")]
    public ReadScriptResult ReadScript(
        [Description("Required repository-relative approved source/documentation file path.")] string path,
        [Description("Optional 1-based start line; defaults to 1.")] int? startLine = null,
        [Description("Optional line count from 1 to 200; defaults to 100.")] int? lineCount = null) =>
        service.Read(path, startLine, lineCount);
}
