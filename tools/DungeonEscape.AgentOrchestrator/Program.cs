using System.Text.Json;
using DungeonEscape.AgentOrchestrator;

if (!args.Contains("--demo", StringComparer.Ordinal))
{
    Console.WriteLine("DungeonEscape deterministic agent orchestrator. Run with --demo [--repository-root PATH].");
    return 0;
}

var rootIndex = Array.IndexOf(args, "--repository-root");
var repositoryRoot = rootIndex >= 0 && rootIndex + 1 < args.Length
    ? Path.GetFullPath(args[rootIndex + 1])
    : FindRepositoryRoot(AppContext.BaseDirectory);
var serverDll = Path.Combine(repositoryRoot, "tools", "DungeonEscape.Mcp", "bin", "Debug", "net10.0",
    "DungeonEscape.Mcp.dll");
if (!File.Exists(serverDll))
{
    Console.Error.WriteLine("Build tools/DungeonEscape.Mcp before running the demonstration.");
    return 2;
}

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var mcp = await McpStdioToolClient.StartAsync(serverDll, repositoryRoot, timeout.Token);
var workflowId = "demo-locked-chest";
var trace = new ConsoleWorkflowTraceSink();
var gateway = new RoleBasedToolGateway(mcp, trace, workflowId);
var executor = new ScriptedAgentExecutor(
    async (assignment, context, cancellationToken) =>
    {
        var metadata = await context.Tools.CallAsync(AgentRole.UnityEngineer, "get_project_info",
            JsonSerializer.SerializeToElement(new { }), cancellationToken);
        return AgentExecutionResult<EngineerToReviewer>.Success(AgentRole.UnityEngineer,
            new(assignment.WorkflowId, assignment.Attempt,
                "Use the explicitly supplied inventory and interaction context; MCP supplies metadata only.",
                "Add a key requirement to the existing interaction decision path, check inventory before opening, " +
                "and preserve the current opened-object persistence path after successful unlock.",
                ["Inventory rules", "Unity interaction path", "chest persistence"],
                ["The supplied context accurately describes the relevant source boundaries."],
                ["Core unit tests for key matching", "Unity interaction test for locked/unlocked outcomes"],
                ["Exact source locations require a separately authorized source-inspection capability."],
                [new("get_project_info", metadata.Succeeded)]));
    },
    (assignment, _, _) => Task.FromResult(
        AgentExecutionResult<ReviewerToManager>.Success(AgentRole.Reviewer,
            new(assignment.WorkflowId, assignment.Attempt, ReviewVerdict.Approve,
                ["Proposal uses supplied context, preserves the read-only boundary, and identifies validation."],
                [], ["Source locations remain to be verified before implementation."]))));

var request = new WorkflowRequest(
    "Inspect the inventory and interaction architecture and propose how a locked chest requiring a key should be implemented.",
    ["Use existing inventory and interaction boundaries", "Do not modify project files"],
    ["Read-only analysis", "Do not infer source access from get_project_info"],
    [
        "Inventory ownership and portable rules live in DungeonEscape.Core.",
        "Unity GameState coordinates map interactions and opened-object persistence.",
        "Current MCP exposes repository metadata only and cannot inspect source files."
    ]);
var result = await new OrchestrationWorkflow(workflowId, executor, gateway, trace)
    .RunAsync(request, timeout.Token);
Console.WriteLine($"RESULT status={result.Status} engineerAttempts={result.EngineerAttempts} " +
    $"reviewerAttempts={result.ReviewerAttempts}");
Console.WriteLine("RECOMMENDATION " + result.Proposal?.ProposedSolution);
return result.Status == WorkflowStatus.Completed ? 0 : 1;

static string FindRepositoryRoot(string start)
{
    for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "DungeonEscape.sln"))) return directory.FullName;
    throw new InvalidOperationException("Could not locate the DungeonEscape repository root.");
}
