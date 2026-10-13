using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using DungeonEscape.Mcp.Security;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class RunTestsTools(RunTestsService service)
{
    [McpServerTool(Name = McpCapabilityNames.RunTests, ReadOnly = false, Destructive = false,
        Idempotent = false, OpenWorld = false)]
    [Description("Runs one fixed approved DungeonEscape test suite with bounded output and timeout.")]
    public Task<RunTestsResult> RunTests(
        [Description("Required fixed suite: core, mcp, or orchestrator.")] ApprovedTestSuite suite,
        [Description("Optional timeout in seconds from 10 to 300; defaults to 120.")] int? timeoutSeconds = null,
        CancellationToken cancellationToken = default) =>
        service.RunAsync(suite, timeoutSeconds, cancellationToken);
}
