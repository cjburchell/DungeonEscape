using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class OpenAiLiveIntegrationTests
{
    [OpenAiFact]
    [Trait("Category", "LiveOpenAI")]
    public async Task RealProviderProducesTypedSpecialistWorkflowResult()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var model = Environment.GetEnvironmentVariable("DUNGEONESCAPE_OPENAI_MODEL")!;
        var testProject = FindAncestor(AppContext.BaseDirectory, "DungeonEscape.AgentOrchestrator.Tests");
        var toolsDirectory = testProject.Parent!;
        var root = toolsDirectory.Parent!.FullName;
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(toolsDirectory.FullName, "DungeonEscape.Mcp", "bin", configuration,
            "net10.0", "DungeonEscape.Mcp.dll");
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await using var mcp = await McpStdioToolClient.StartAsync(dll, root, timeout.Token);
        var trace = new InMemoryWorkflowTraceSink();
        var gateway = new RoleBasedToolGateway(mcp, trace, "live-openai");
        var executor = new LlmAgentExecutor(new OpenAiLlmClient(apiKey),
            LlmExecutionOptions.Create(model, 120, 8));
        var workflow = new OrchestrationWorkflow("live-openai", executor, gateway, trace);
        var result = await workflow.RunAsync(new(
            "Review the current MCP architecture and identify which files would likely need modification to add a new read-only project metadata tool.",
            ["Use repository evidence", "Return a concrete proposal"],
            ["Read-only", "Do not modify files", "Do not run tests"], []), timeout.Token);

        Assert.NotEqual(WorkflowStatus.Failed, result.Status);
        Assert.NotNull(result.Proposal);
        Assert.NotNull(result.Review);
        Assert.Contains(trace.Events, item => item.EventType == "ToolCallStarted"
            && item.Role == AgentRole.UnityEngineer);
        Assert.DoesNotContain(trace.Events, item => item.Detail.Contains(apiKey, StringComparison.Ordinal));
    }

    private static DirectoryInfo FindAncestor(string start, string name)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            if (directory.Name == name) return directory;
        throw new InvalidOperationException($"Could not find {name} from {start}.");
    }

    private sealed class OpenAiFactAttribute : FactAttribute
    {
        public OpenAiFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DUNGEONESCAPE_OPENAI_MODEL")))
                Skip = "Set OPENAI_API_KEY and DUNGEONESCAPE_OPENAI_MODEL to run the live provider test.";
        }
    }
}
