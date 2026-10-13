using System.Text.Json;
using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public async Task ApprovalCompletesAfterOneEngineerAndReviewer()
    {
        var run = CreateRun(ReviewVerdict.Approve);
        var result = await run.Workflow.RunAsync(Request());

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Equal(1, result.EngineerAttempts);
        Assert.Equal(1, result.ReviewerAttempts);
    }

    [Fact]
    public async Task OneRequestChangesRetriesThenApproves()
    {
        var run = CreateRun(ReviewVerdict.RequestChanges, ReviewVerdict.Approve);
        var result = await run.Workflow.RunAsync(Request());

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Equal(2, result.EngineerAttempts);
        Assert.Single(run.Trace.Events, e => e.EventType == "RetryConsumed");
        Assert.Contains("Clarify validation", run.EngineerAssignments[1].RequiredChanges);
    }

    [Fact]
    public async Task SecondRequestChangesStopsForHumanWithoutThirdAttempt()
    {
        var run = CreateRun(ReviewVerdict.RequestChanges, ReviewVerdict.RequestChanges);
        var result = await run.Workflow.RunAsync(Request());

        Assert.Equal(WorkflowStatus.NeedsHumanDecision, result.Status);
        Assert.Equal(OrchestrationWorkflow.MaximumEngineerAttempts, run.EngineerAssignments.Count);
        Assert.Single(run.Trace.Events, e => e.EventType == "HumanDecisionRequired");
    }

    [Fact]
    public async Task InconclusiveReturnsIncompleteResult()
    {
        var result = await CreateRun(ReviewVerdict.Inconclusive).Workflow.RunAsync(Request());
        Assert.Equal(WorkflowStatus.Incomplete, result.Status);
    }

    [Fact]
    public async Task MalformedEngineerResultFailsClosed()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment) with { ProposedSolution = "" })),
            (_, _, _) => throw new Xunit.Sdk.XunitException("Reviewer must not run."));
        var result = await Workflow(executor, trace).RunAsync(Request());
        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Equal(0, result.ReviewerAttempts);
    }

    [Fact]
    public async Task MalformedReviewerResultFailsClosed()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment, ReviewVerdict.RequestChanges) with
                { RequiredChanges = [] })));
        var result = await Workflow(executor, trace).RunAsync(Request());
        Assert.Equal(WorkflowStatus.Failed, result.Status);
    }

    [Fact]
    public async Task EngineerCannotSelfApproveOrReturnAnotherRole()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.Reviewer, Engineer(assignment))),
            (_, _, _) => throw new Xunit.Sdk.XunitException("Reviewer must not run."));
        var result = await Workflow(executor, trace).RunAsync(Request());
        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Contains("self-approve", result.Summary);
    }

    [Fact]
    public async Task UnauthorizedAndWritableToolsAreRejectedAndTraced()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var safeClient = new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(safeClient, trace, Id);

        var manager = await gateway.CallAsync(AgentRole.Manager, "get_project_info", EmptyJson(), default);
        var unknown = await gateway.CallAsync(AgentRole.UnityEngineer, "write_file", EmptyJson(), default);

        var unsafeClient = new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), false, true, false));
        var unsafeGateway = new RoleBasedToolGateway(unsafeClient, trace, Id);
        var writable = await unsafeGateway.CallAsync(AgentRole.UnityEngineer, "get_project_info",
            EmptyJson(), default);

        Assert.False(manager.Succeeded);
        Assert.False(unknown.Succeeded);
        Assert.False(writable.Succeeded);
        Assert.Equal(3, trace.Events.Count(e => e.EventType == "ToolCallRejected"));
        Assert.Empty(safeClient.Calls);
        Assert.Empty(unsafeClient.Calls);
    }

    [Fact]
    public async Task UnsupportedAgentRoleIsDeniedAndTracedWithoutConsultingClient()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("search_code", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync((AgentRole)int.MaxValue, "search_code",
            EmptyJson(), default);

        Assert.False(result.Succeeded);
        var rejected = Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Equal((AgentRole)int.MaxValue, rejected.Role);
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(AgentRole.UnityEngineer)]
    [InlineData(AgentRole.Reviewer)]
    public async Task SpecialistsCanInvokeSafelyAnnotatedSearchCode(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("search_code", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "search_code",
            JsonSerializer.SerializeToElement(new { query = "Inventory" }), default);

        Assert.True(result.Succeeded);
        Assert.Equal(["search_code"], client.Calls);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallStarted" && e.Role == role);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallCompleted" && e.Role == role);
    }

    [Fact]
    public async Task ManagerCannotInvokeSearchCode()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("search_code", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.Manager, "search_code", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task SearchCodeRequiresReadOnlyNonDestructiveClosedWorldMetadata(
        bool readOnly, bool destructive, bool openWorld)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("search_code", EmptyJson(), readOnly, destructive, openWorld));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "search_code",
            EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(AgentRole.UnityEngineer)]
    [InlineData(AgentRole.Reviewer)]
    public async Task SpecialistsCanInvokeSafelyAnnotatedReadScript(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("read_script", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "read_script",
            JsonSerializer.SerializeToElement(new { path = "DungeonEscape.Core/State/Hero.cs" }), default);

        Assert.True(result.Succeeded);
        Assert.Equal(["read_script"], client.Calls);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallStarted" && e.Role == role);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallCompleted" && e.Role == role);
    }

    [Theory]
    [InlineData(AgentRole.Manager)]
    [InlineData((AgentRole)int.MaxValue)]
    public async Task ManagerAndUnsupportedRolesCannotInvokeReadScript(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("read_script", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "read_script", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected" && e.Role == role);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task UnadvertisedReadScriptIsRejected()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "read_script", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task ReadScriptRequiresReadOnlyNonDestructiveClosedWorldMetadata(
        bool readOnly, bool destructive, bool openWorld)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("read_script", EmptyJson(), readOnly, destructive, openWorld));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.Reviewer, "read_script", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(AgentRole.UnityEngineer)]
    [InlineData(AgentRole.Reviewer)]
    public async Task SpecialistsCanInvokeSafelyAnnotatedListScenes(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("list_scenes", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "list_scenes", EmptyJson(), default);

        Assert.True(result.Succeeded);
        Assert.Equal(["list_scenes"], client.Calls);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallStarted" && e.Role == role);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallCompleted" && e.Role == role);
    }

    [Theory]
    [InlineData(AgentRole.Manager)]
    [InlineData((AgentRole)int.MaxValue)]
    public async Task ManagerAndUnsupportedRolesCannotInvokeListScenes(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("list_scenes", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "list_scenes", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected" && e.Role == role);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task UnadvertisedListScenesIsRejected()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "list_scenes", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task ListScenesRequiresReadOnlyNonDestructiveClosedWorldMetadata(
        bool readOnly, bool destructive, bool openWorld)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("list_scenes", EmptyJson(), readOnly, destructive, openWorld));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.Reviewer, "list_scenes", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(AgentRole.UnityEngineer)]
    [InlineData(AgentRole.Reviewer)]
    public async Task SpecialistsCanInvokeSafelyAnnotatedUnityConsoleWithoutTracingContent(AgentRole role)
    {
        const string sensitiveContent = "token=secret-value C:\\Users\\person\\Editor.log";
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_unity_console", EmptyJson(), true, false, false))
        {
            Content = sensitiveContent
        };
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "get_unity_console",
            JsonSerializer.SerializeToElement(new { maxEntries = 10, minimumSeverity = "warning" }), default);

        Assert.True(result.Succeeded);
        Assert.Equal(sensitiveContent, result.Content);
        Assert.Equal(["get_unity_console"], client.Calls);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallStarted" && e.Role == role);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallCompleted" && e.Role == role);
        Assert.DoesNotContain(trace.Events, e => e.Detail.Contains("secret-value", StringComparison.Ordinal));
        Assert.DoesNotContain(trace.Events, e => e.Detail.Contains("C:\\Users", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(AgentRole.Manager)]
    [InlineData((AgentRole)int.MaxValue)]
    public async Task ManagerAndUnsupportedRolesCannotInvokeUnityConsole(AgentRole role)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_unity_console", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(role, "get_unity_console", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected" && e.Role == role);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task UnadvertisedUnityConsoleIsRejected()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), true, false, false));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "get_unity_console", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task UnityConsoleRequiresReadOnlyNonDestructiveClosedWorldMetadata(
        bool readOnly, bool destructive, bool openWorld)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(
            new McpToolDefinition("get_unity_console", EmptyJson(), readOnly, destructive, openWorld));
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        var result = await gateway.CallAsync(AgentRole.Reviewer, "get_unity_console", EmptyJson(), default);

        Assert.False(result.Succeeded);
        Assert.Single(trace.Events, e => e.EventType == "ToolCallRejected");
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task TraceEventsAreCorrelatedAndCoverAgentAndWorkflowTransitions()
    {
        var run = CreateRun(ReviewVerdict.Approve);
        await run.Workflow.RunAsync(Request());

        Assert.All(run.Trace.Events, e => Assert.Equal(Id, e.WorkflowId));
        Assert.Contains(run.Trace.Events, e => e.EventType == "WorkflowStarted");
        Assert.Contains(run.Trace.Events, e => e.EventType == "WorkflowCompleted");
        Assert.Equal(2, run.Trace.Events.Count(e => e.EventType == "AgentDispatched"));
        Assert.Equal(2, run.Trace.Events.Count(e => e.EventType == "AgentCompleted"));
        Assert.All(run.Trace.Events.Where(e => e.EventType.StartsWith("Agent")),
            e => Assert.False(string.IsNullOrWhiteSpace(e.RunId)));
        Assert.Contains(run.Trace.Events, e => e.EventType == "Transition");
    }

    private const string Id = "workflow-test";

    private static WorkflowRequest Request() => new("Analyze locked chests", ["Produce a proposal"],
        ["Read-only"], ["Static inventory and interaction context"]);

    private static EngineerToReviewer Engineer(ManagerToEngineer assignment) =>
        new(assignment.WorkflowId, assignment.Attempt, "Interpretation", "Small proposal",
            ["Inventory", "Interaction"], ["Static context is accurate"], ["Unit tests"], [], []);

    private static ReviewerToManager Review(ReviewAssignment assignment, ReviewVerdict verdict) =>
        new(assignment.WorkflowId, assignment.Attempt, verdict, ["Finding"],
            verdict == ReviewVerdict.RequestChanges ? ["Clarify validation"] : [], []);

    private static (OrchestrationWorkflow Workflow, InMemoryWorkflowTraceSink Trace,
        List<ManagerToEngineer> EngineerAssignments) CreateRun(params ReviewVerdict[] verdicts)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var assignments = new List<ManagerToEngineer>();
        var reviewIndex = 0;
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) =>
            {
                assignments.Add(assignment);
                return Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                    AgentRole.UnityEngineer, Engineer(assignment)));
            },
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment, verdicts[reviewIndex++]))));
        return (Workflow(executor, trace), trace, assignments);
    }

    private static OrchestrationWorkflow Workflow(IAgentExecutor executor, InMemoryWorkflowTraceSink trace)
    {
        var gateway = new RoleBasedToolGateway(new FakeToolClient(
            new McpToolDefinition("get_project_info", EmptyJson(), true, false, false)), trace, Id);
        return new(Id, executor, gateway, trace);
    }

    private static JsonElement EmptyJson() => JsonSerializer.SerializeToElement(new { });

    private sealed class FakeToolClient(params McpToolDefinition[] tools) : IReadOnlyToolClient
    {
        public List<string> Calls { get; } = [];
        public string Content { get; init; } = "{}";
        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<McpToolDefinition>>(tools);
        public Task<ToolCallResult> CallAsync(string name, JsonElement arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add(name);
            return Task.FromResult(new ToolCallResult(true, Content));
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
