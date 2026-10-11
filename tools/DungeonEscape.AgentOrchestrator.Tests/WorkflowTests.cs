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
        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<McpToolDefinition>>(tools);
        public Task<ToolCallResult> CallAsync(string name, JsonElement arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add(name);
            return Task.FromResult(new ToolCallResult(true, "{}"));
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
