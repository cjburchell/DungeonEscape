using System.Text.Json;
using DungeonEscape.AgentOrchestrator;

var provider = Value(args, "--provider") ?? (args.Contains("--demo", StringComparer.Ordinal) ? "scripted" : null);
if (provider is null)
{
    Console.WriteLine("DungeonEscape agent orchestrator. Use --demo or --provider openai --task \"<task>\".");
    return 0;
}
if (provider is not ("scripted" or "openai"))
{
    Console.Error.WriteLine("Provider must be 'scripted' or 'openai'.");
    return 2;
}

IAgentExecutor executor;
if (provider == "openai")
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    var model = Environment.GetEnvironmentVariable("DUNGEONESCAPE_OPENAI_MODEL");
    if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
    {
        Console.Error.WriteLine("OpenAI mode requires OPENAI_API_KEY and DUNGEONESCAPE_OPENAI_MODEL.");
        return 2;
    }
    if (!TryEnvironmentInt("DUNGEONESCAPE_LLM_TIMEOUT_SECONDS", 120, out var timeoutSeconds)
        || !TryEnvironmentInt("DUNGEONESCAPE_LLM_MAX_TOOL_CALLS", 8, out var maximumToolCalls))
    {
        Console.Error.WriteLine("LLM timeout/tool-call configuration must be valid integers.");
        return 2;
    }
    try
    {
        executor = new LlmAgentExecutor(new OpenAiLlmClient(apiKey),
            LlmExecutionOptions.Create(model, timeoutSeconds, maximumToolCalls));
    }
    catch (ArgumentException exception)
    {
        Console.Error.WriteLine("Invalid LLM configuration: " + exception.Message);
        return 2;
    }
}
else
{
    executor = Scripted();
}

var repositoryRoot = Value(args, "--repository-root") is { } configuredRoot
    ? Path.GetFullPath(configuredRoot)
    : FindRepositoryRoot(AppContext.BaseDirectory);
var serverDll = Path.Combine(repositoryRoot, "tools", "DungeonEscape.Mcp", "bin", "Debug", "net10.0",
    "DungeonEscape.Mcp.dll");
if (!File.Exists(serverDll))
{
    Console.Error.WriteLine("Build tools/DungeonEscape.Mcp before running the orchestrator.");
    return 2;
}

var task = Value(args, "--task") ??
    "Inspect the inventory and interaction architecture and propose how a locked chest requiring a key should be implemented.";
var workflowId = "local-" + Guid.NewGuid().ToString("N");
var trace = new ConsoleWorkflowTraceSink();
using var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var mcp = await McpStdioToolClient.StartAsync(serverDll, repositoryRoot, startupTimeout.Token);
var gateway = new RoleBasedToolGateway(mcp, trace, workflowId);

var request = new WorkflowRequest(task,
    ["Provide an evidence-based, concrete technical analysis"],
    ["Do not modify project files", "Only QA may run bounded approved tests"],
    provider == "scripted"
        ? ["Inventory rules live in Core; Unity GameState coordinates interactions and persistence."]
        : []);
var result = await new OrchestrationWorkflow(workflowId, executor, gateway, trace).RunAsync(request);
Console.WriteLine($"RESULT status={result.Status} engineerAttempts={result.EngineerAttempts} " +
    $"reviewerAttempts={result.ReviewerAttempts} qaAttempts={result.QaAttempts}");
if (result.Proposal is not null) Console.WriteLine("PROPOSAL " + result.Proposal.ProposedSolution);
if (result.Review is not null) Console.WriteLine("REVIEW " + result.Review.Verdict);
if (result.QA is not null)
    Console.WriteLine($"QA {result.QA.Outcome} suites={string.Join(',', result.QA.TestSuitesRun)}");
return result.Status == WorkflowStatus.Completed ? 0 : 1;

static IAgentExecutor Scripted() => new ScriptedAgentExecutor(
    async (assignment, context, cancellationToken) =>
    {
        var metadata = await context.Tools.CallAsync(AgentRole.UnityEngineer, "get_project_info",
            JsonSerializer.SerializeToElement(new { }), cancellationToken);
        return AgentExecutionResult<EngineerToReviewer>.Success(AgentRole.UnityEngineer,
            new(assignment.WorkflowId, assignment.Attempt, "Analyze the supplied architecture context.",
                "Add a key requirement to the existing interaction path and preserve opened-object persistence.",
                ["Inventory rules", "Unity interaction path", "chest persistence"],
                ["The supplied context describes the relevant boundaries."],
                ["Core key-matching tests", "Unity locked/unlocked interaction test"],
                ["Exact source locations require bounded source inspection."],
                [new("get_project_info", metadata.Succeeded)]));
    },
    (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
        AgentRole.Reviewer, new(assignment.WorkflowId, assignment.Attempt, ReviewVerdict.Approve,
            ["Proposal respects the supplied boundary and identifies validation."], [],
            ["Verify exact source locations before implementation."]))),
    async (assignment, context, cancellationToken) =>
    {
        var tests = await context.Tools.CallAsync(AgentRole.QA, "run_tests",
            JsonSerializer.SerializeToElement(new { suite = "orchestrator", timeoutSeconds = 120 }),
            cancellationToken);
        return AgentExecutionResult<QaToManager>.Success(AgentRole.QA,
            new(assignment.WorkflowId, assignment.Attempt,
                tests.Succeeded ? QaOutcome.Pass : QaOutcome.Fail,
                ["orchestrator"], [new("orchestrator", tests.Succeeded ? "passed" : "failed",
                    tests.Succeeded ? 0 : 1, 0, [], false,
                    "Existing orchestrator baseline was executed through bounded run_tests.")],
                ["This validates the current baseline, not the unimplemented proposal."],
                ["Post-implementation validation remains required."],
                ["The proposal has not been implemented in this read-only workflow."],
                [new("run_tests", tests.Succeeded)]));
    });

static string? Value(string[] values, string name)
{
    var index = Array.IndexOf(values, name);
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
}

static bool TryEnvironmentInt(string name, int fallback, out int value)
{
    var raw = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(raw)) { value = fallback; return true; }
    return int.TryParse(raw, out value);
}

static string FindRepositoryRoot(string start)
{
    for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "DungeonEscape.sln"))) return directory.FullName;
    throw new InvalidOperationException("Could not locate the DungeonEscape repository root.");
}
