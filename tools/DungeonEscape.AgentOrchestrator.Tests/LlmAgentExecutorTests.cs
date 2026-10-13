using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class LlmAgentExecutorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task EngineerMapsStructuredResultAndGetsOnlyAuthorizedSchemas()
    {
        var expected = Engineer();
        var run = CreateRun(new LlmResponse("resp-engineer", Json(expected), [], Usage: new(3, 2, 5)));

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Null(result.Error);
        Assert.NotNull(result.Output);
        Assert.Equal(expected.WorkflowId, result.Output.WorkflowId);
        Assert.Equal(expected.ProposedSolution, result.Output.ProposedSolution);
        Assert.Equal(expected.Components, result.Output.Components);
        var request = Assert.Single(run.Llm.Requests);
        Assert.Equal(AgentRole.UnityEngineer, request.Role);
        Assert.Equal(AgentPrompts.UnityEngineer, request.Instructions);
        Assert.Equal(SpecialistTools, request.Tools.Select(tool => tool.Name).OrderBy(name => name));
        Assert.DoesNotContain(request.Tools, tool => tool.Name == "run_tests");
        Assert.Contains(run.Trace.Events, e => e.EventType == "LlmResponseReceived"
            && e.Detail.Contains("totalTokens=5") && e.Detail.Contains("model=test-model"));
    }

    [Fact]
    public async Task ReviewerMapsStructuredResultAndUsesIndependentPrompt()
    {
        var expected = Review();
        var run = CreateRun(new LlmResponse("resp-review", Json(expected), []));

        var result = await run.Executor.ExecuteReviewerAsync(ReviewAssignment(), run.Context, default);

        Assert.NotNull(result.Output);
        Assert.Equal(expected.WorkflowId, result.Output.WorkflowId);
        Assert.Equal(expected.Verdict, result.Output.Verdict);
        Assert.Equal(expected.Findings, result.Output.Findings);
        var request = Assert.Single(run.Llm.Requests);
        Assert.Equal(AgentRole.Reviewer, request.Role);
        Assert.Equal(AgentPrompts.Reviewer, request.Instructions);
        Assert.DoesNotContain("hidden reasoning", request.Input, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ToolRequestRoutesThroughGatewayAndContentIsNotTraced()
    {
        const string sensitive = "source-content-secret";
        var run = CreateRun([
            new("resp-1", null, [new("call-1", "search_code", "{\"query\":\"Scene\"}")]),
            new("resp-2", Json(Engineer() with
                { ToolUses = [new("search_code", true)] }), [])], sensitive);

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Null(result.Error);
        Assert.Equal(["search_code"], run.Tools.Calls);
        Assert.Equal(sensitive, run.Llm.Outputs.Single().Single().Content);
        Assert.DoesNotContain(run.Trace.Events, e => e.Detail.Contains(sensitive, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnknownToolIsRejectedByGatewayAndReturnedSafelyToModel()
    {
        var run = CreateRun([
            new("resp-1", null, [new("call-1", "write_file", "{}")]),
            new("resp-2", Json(Engineer()), [])]);

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Null(result.Error);
        Assert.Empty(run.Tools.Calls);
        Assert.Contains(run.Trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Contains("error", run.Llm.Outputs.Single().Single().Content);
    }

    [Fact]
    public async Task ToolBudgetIsFiniteAndStopsBeforeExcessCalls()
    {
        var run = CreateRun(new LlmResponse("resp", null,
            [new("one", "get_project_info", "{}"), new("two", "list_scenes", "{}")]));
        run.Executor = new(run.Llm, LlmExecutionOptions.Create("test-model", 5, 1));

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Contains("budget", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(run.Tools.Calls);
        Assert.Contains(run.Trace.Events, e => e.EventType == "LlmToolBudgetExhausted");
    }

    [Fact]
    public async Task TimeoutFailsSafely()
    {
        var run = CreateRun(new LlmResponse("unused", null, []));
        run.Llm.DelayForever = true;
        run.Executor = new(run.Llm, LlmExecutionOptions.Create("test-model", 1, 8));

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(run.Trace.Events, e => e.Detail.Contains("status=timeout"));
    }

    [Fact]
    public async Task ExternalCancellationPropagates()
    {
        var run = CreateRun(new LlmResponse("unused", null, []));
        run.Llm.DelayForever = true;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, cancellation.Token));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"workflowId\":\"wf\"}")]
    [InlineData("{\"workflowId\":\"wf\",\"attempt\":1,\"verdict\":\"SelfApprove\",\"findings\":[],\"requiredChanges\":[],\"residualRisks\":[]}")]
    public async Task MalformedMissingOrUnknownReviewerOutputFailsClosed(string output)
    {
        var run = CreateRun(new LlmResponse("resp", output, []));

        var result = await run.Executor.ExecuteReviewerAsync(ReviewAssignment(), run.Context, default);

        Assert.Contains("structured output", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefusalFailsSafelyWithoutRecordingRefusalText()
    {
        var run = CreateRun(new LlmResponse("resp", "private refusal prose", [], Refused: true));

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Contains("refused", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(run.Trace.Events, e => e.Detail.Contains("private refusal prose"));
        Assert.Contains(run.Trace.Events, e => e.Detail.Contains("status=refused"));
    }

    [Theory]
    [InlineData(LlmProviderErrorKind.Authentication, "provider_authentication")]
    [InlineData(LlmProviderErrorKind.RateLimit, "provider_ratelimit")]
    [InlineData(LlmProviderErrorKind.Network, "provider_network")]
    public async Task ProviderFailuresAreClassifiedWithoutLeakingSecrets(
        LlmProviderErrorKind kind, string status)
    {
        var run = CreateRun(new LlmResponse("unused", null, []));
        run.Llm.Error = new(kind, "secret credential from provider");

        var result = await run.Executor.ExecuteEngineerAsync(EngineerAssignment(), run.Context, default);

        Assert.Equal("Provider request failed safely.", result.Error);
        Assert.DoesNotContain(run.Trace.Events, e => e.Detail.Contains("secret credential"));
        Assert.Contains(run.Trace.Events, e => e.Detail.Contains($"status={status}"));
    }

    [Fact]
    public void OptionsEnforceFiniteConfiguration()
    {
        Assert.Throws<ArgumentException>(() => LlmExecutionOptions.Create(""));
        Assert.Throws<ArgumentException>(() => LlmExecutionOptions.Create("bad\nmodel"));
        Assert.Throws<ArgumentOutOfRangeException>(() => LlmExecutionOptions.Create("model", 301));
        Assert.Throws<ArgumentOutOfRangeException>(() => LlmExecutionOptions.Create("model", 120, 21));
    }

    [Fact]
    public async Task ManagerRemainsToolFree()
    {
        var run = CreateRun(new LlmResponse("unused", null, []));

        var tools = await run.Gateway.ListAllowedToolsAsync(AgentRole.Manager, default);
        var unsupported = await run.Gateway.ListAllowedToolsAsync((AgentRole)999, default);

        Assert.Empty(tools);
        Assert.Empty(unsupported);
        Assert.Throws<ArgumentOutOfRangeException>(() => AgentPrompts.For(AgentRole.Manager));
    }

    private static TestRun CreateRun(LlmResponse response, string toolContent = "{}") =>
        CreateRun([response], toolContent);

    private static TestRun CreateRun(IEnumerable<LlmResponse> responses, string toolContent = "{}")
    {
        var trace = new InMemoryWorkflowTraceSink();
        var tools = new FakeToolClient(toolContent);
        var gateway = new RoleBasedToolGateway(tools, trace, "wf");
        var llm = new FakeLlmClient(responses);
        return new(llm, tools, gateway, trace,
            new LlmAgentExecutor(llm, LlmExecutionOptions.Create("test-model", 5, 8)),
            new("wf", 1, gateway, trace));
    }

    private static ManagerToEngineer EngineerAssignment() => new("wf", 1, "Analyze", ["Accurate"],
        ["Read-only"], [], SpecialistTools, []);
    private static ReviewAssignment ReviewAssignment() => new("wf", 1, "Analyze", ["Accurate"],
        ["Read-only"], Engineer(), SpecialistTools);
    private static EngineerToReviewer Engineer() => new("wf", 1, "Interpretation", "Proposal",
        ["Component"], [], ["Tests"], [], []);
    private static ReviewerToManager Review() => new("wf", 1, ReviewVerdict.Approve,
        ["Looks sound"], [], []);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static readonly string[] SpecialistTools =
        ["get_git_diff", "get_project_info", "get_unity_console", "list_scenes", "read_script", "search_code"];

    private sealed record TestRun(FakeLlmClient Llm, FakeToolClient Tools,
        RoleBasedToolGateway Gateway, InMemoryWorkflowTraceSink Trace,
        LlmAgentExecutor InitialExecutor, AgentExecutionContext Context)
    {
        public LlmAgentExecutor Executor { get; set; } = InitialExecutor;
    }

    private sealed class FakeLlmClient(IEnumerable<LlmResponse> responses) : ILlmClient
    {
        private readonly Queue<LlmResponse> responses = new(responses);
        public string ProviderName => "FakeOpenAI";
        public List<LlmConversationRequest> Requests { get; } = [];
        public List<IReadOnlyList<LlmToolOutput>> Outputs { get; } = [];
        public bool DelayForever { get; set; }
        public LlmProviderException? Error { get; set; }
        public ILlmConversation StartConversation(LlmConversationRequest request)
        {
            Requests.Add(request);
            return new Conversation(this);
        }

        private sealed class Conversation(FakeLlmClient owner) : ILlmConversation
        {
            public async Task<LlmResponse> RespondAsync(IReadOnlyList<LlmToolOutput> toolOutputs,
                CancellationToken cancellationToken)
            {
                if (owner.DelayForever) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                if (owner.Error is not null) throw owner.Error;
                if (toolOutputs.Count > 0) owner.Outputs.Add(toolOutputs);
                return owner.responses.Dequeue();
            }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeToolClient(string content) : IReadOnlyToolClient
    {
        public List<string> Calls { get; } = [];
        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<McpToolDefinition>>(
                SpecialistTools.Select(name => new McpToolDefinition(name,
                    JsonSerializer.SerializeToElement(new { type = "object", properties = new { } }),
                    true, false, false, name + " description"))
                .Append(new("run_tests", JsonSerializer.SerializeToElement(new { type = "object" }),
                    false, false, false, "tests")).ToArray());
        public Task<ToolCallResult> CallAsync(string name, JsonElement arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add(name);
            return Task.FromResult(new ToolCallResult(true, content));
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
