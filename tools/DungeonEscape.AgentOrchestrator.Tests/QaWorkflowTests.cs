using System.Text.Json;
using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class QaWorkflowTests
{
    [Fact]
    public async Task RolePermissionMatrixIsExactAndOnlyQaCanRunTests()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var client = new FakeToolClient(InspectionTools().Append(RunTests()).ToArray());
        var gateway = new RoleBasedToolGateway(client, trace, Id);

        Assert.Empty(gateway.AllowedToolsFor(AgentRole.Manager));
        Assert.Equal(6, gateway.AllowedToolsFor(AgentRole.UnityEngineer).Count);
        Assert.Equal(6, gateway.AllowedToolsFor(AgentRole.Reviewer).Count);
        Assert.Equal(7, gateway.AllowedToolsFor(AgentRole.QA).Count);
        Assert.Contains("run_tests", gateway.AllowedToolsFor(AgentRole.QA));
        Assert.DoesNotContain("run_tests", gateway.AllowedToolsFor(AgentRole.UnityEngineer));
        Assert.Empty(gateway.AllowedToolsFor((AgentRole)999));

        foreach (var role in new[] { AgentRole.Manager, AgentRole.UnityEngineer, AgentRole.Reviewer, (AgentRole)999 })
            Assert.False((await gateway.CallAsync(role, "run_tests", EmptyJson(), default)).Succeeded);
        Assert.True((await gateway.CallAsync(AgentRole.QA, "run_tests", EmptyJson(), default)).Succeeded);
        Assert.Equal(["run_tests"], client.Calls);
    }

    [Fact]
    public async Task MissingRunTestsAdvertisementFailsClosed()
    {
        var run = Gateway(InspectionTools().ToArray());
        Assert.False((await run.Gateway.CallAsync(AgentRole.QA, "run_tests", EmptyJson(), default)).Succeeded);
        Assert.Empty(run.Client.Calls);
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public async Task RunTestsMetadataDriftFailsClosed(bool readOnly, bool destructive,
        bool openWorld, bool idempotent)
    {
        var run = Gateway(new McpToolDefinition("run_tests", EmptyJson(), readOnly, destructive,
            openWorld, "tests", idempotent));
        Assert.False((await run.Gateway.CallAsync(AgentRole.QA, "run_tests", EmptyJson(), default)).Succeeded);
        Assert.Empty(run.Client.Calls);
    }

    [Fact]
    public async Task QaCannotInvokeArbitraryNonReadOnlyCapability()
    {
        var run = Gateway(new McpToolDefinition("deploy", EmptyJson(), false, false, false,
            "deploy", false));
        Assert.False((await run.Gateway.CallAsync(AgentRole.QA, "deploy", EmptyJson(), default)).Succeeded);
        Assert.Empty(run.Client.Calls);
    }

    [Fact]
    public async Task ReviewerApprovalDispatchesQaWithTypedResultsAndQaPassCompletes()
    {
        QaAssignment? captured = null;
        var run = Workflow(QaOutcome.Pass, qa => captured = qa);

        var result = await run.Workflow.RunAsync(Request());

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Equal(1, result.QaAttempts);
        Assert.Equal(QaOutcome.Pass, result.QA?.Outcome);
        Assert.NotNull(captured);
        Assert.Equal(ReviewVerdict.Approve, captured.ReviewerResult.Verdict);
        Assert.Equal("Proposal", captured.EngineerResult.ProposedSolution);
        Assert.Equal(7, captured.AllowedTools.Count);
        Assert.Contains(run.Trace.Events, e => e.EventType == "QaOutcome" && e.Detail == "Pass");
    }

    [Theory]
    [InlineData(QaOutcome.Fail)]
    [InlineData(QaOutcome.Inconclusive)]
    public async Task QaFailOrInconclusiveRequiresHumanWithoutEngineerRepair(QaOutcome outcome)
    {
        var run = Workflow(outcome);

        var result = await run.Workflow.RunAsync(Request());

        Assert.Equal(WorkflowStatus.NeedsHumanDecision, result.Status);
        Assert.Equal(1, result.EngineerAttempts);
        Assert.Equal(1, result.QaAttempts);
        Assert.Single(run.Trace.Events, e => e.EventType == "HumanDecisionRequired");
        Assert.DoesNotContain(run.Trace.Events, e => e.EventType == "RetryConsumed");
    }

    [Theory]
    [InlineData(ReviewVerdict.RequestChanges)]
    [InlineData(ReviewVerdict.Inconclusive)]
    public async Task QaDoesNotRunBeforeReviewerApproval(ReviewVerdict verdict)
    {
        var qaCalls = 0;
        var trace = new InMemoryWorkflowTraceSink();
        var reviewCalls = 0;
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment.WorkflowId, assignment.Attempt))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment.WorkflowId, assignment.Attempt,
                    verdict == ReviewVerdict.RequestChanges && reviewCalls++ > 0
                        ? ReviewVerdict.Inconclusive : verdict))),
            (_, _, _) => { qaCalls++; throw new Xunit.Sdk.XunitException("QA must not run."); });
        var workflow = new OrchestrationWorkflow(Id, executor, Gateway().Gateway, trace);

        var result = await workflow.RunAsync(Request());

        Assert.Equal(0, qaCalls);
        Assert.Equal(0, result.QaAttempts);
    }

    [Fact]
    public async Task QaMalformedOrExecutorFailureCannotBecomePass()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment.WorkflowId, assignment.Attempt))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment.WorkflowId, assignment.Attempt, ReviewVerdict.Approve))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<QaToManager>.Success(
                AgentRole.QA, QA("wrong", assignment.Attempt, QaOutcome.Pass))));

        var result = await new OrchestrationWorkflow(Id, executor, Gateway().Gateway, trace).RunAsync(Request());

        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Contains("malformed", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QaPassWithFailedEvidenceCannotCompleteWorkflow()
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment.WorkflowId, assignment.Attempt))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment.WorkflowId, assignment.Attempt, ReviewVerdict.Approve))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<QaToManager>.Success(
                AgentRole.QA, QA(assignment.WorkflowId, assignment.Attempt, QaOutcome.Pass) with
                {
                    TestResults = [new("core", "failed", 1, 20, ["BrokenTest"], false,
                        "Current baseline failed.")]
                })));

        var result = await new OrchestrationWorkflow(Id, executor, Gateway().Gateway, trace).RunAsync(Request());

        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.NotEqual(WorkflowStatus.Completed, result.Status);
        Assert.Contains("malformed", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    private static (OrchestrationWorkflow Workflow, InMemoryWorkflowTraceSink Trace) Workflow(
        QaOutcome outcome, Action<QaAssignment>? capture = null)
    {
        var trace = new InMemoryWorkflowTraceSink();
        var executor = new ScriptedAgentExecutor(
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<EngineerToReviewer>.Success(
                AgentRole.UnityEngineer, Engineer(assignment.WorkflowId, assignment.Attempt))),
            (assignment, _, _) => Task.FromResult(AgentExecutionResult<ReviewerToManager>.Success(
                AgentRole.Reviewer, Review(assignment.WorkflowId, assignment.Attempt, ReviewVerdict.Approve))),
            (assignment, _, _) =>
            {
                capture?.Invoke(assignment);
                return Task.FromResult(AgentExecutionResult<QaToManager>.Success(
                    AgentRole.QA, QA(assignment.WorkflowId, assignment.Attempt, outcome)));
            });
        return (new(Id, executor, Gateway().Gateway, trace), trace);
    }

    private static EngineerToReviewer Engineer(string id, int attempt) => new(id, attempt,
        "Interpretation", "Proposal", ["Component"], [], ["Post-change tests"], [], []);
    private static ReviewerToManager Review(string id, int attempt, ReviewVerdict verdict) => new(id,
        attempt, verdict, ["Finding"], verdict == ReviewVerdict.RequestChanges ? ["Change"] : [], []);
    private static QaToManager QA(string id, int attempt, QaOutcome outcome) => new(id, attempt,
        outcome, ["core"], [new("core", outcome == QaOutcome.Pass ? "passed" : "failed",
            outcome == QaOutcome.Pass ? 0 : 1, 20, [], false, "Current baseline only.")],
        ["Existing baseline checked."], ["Proposal is not implemented."],
        ["Post-implementation checks remain."], [new("run_tests", true)]);
    private static WorkflowRequest Request() => new("Analyze", ["Evidence"], ["No writes"], []);

    private static (RoleBasedToolGateway Gateway, FakeToolClient Client) Gateway(
        params McpToolDefinition[] definitions)
    {
        if (definitions.Length == 0) definitions = [.. InspectionTools(), RunTests()];
        var client = new FakeToolClient(definitions);
        return (new(client, new InMemoryWorkflowTraceSink(), Id), client);
    }

    private static IEnumerable<McpToolDefinition> InspectionTools() =>
        new[] { "get_project_info", "search_code", "read_script", "list_scenes", "get_unity_console", "get_git_diff" }
            .Select(name => new McpToolDefinition(name, EmptyJson(), true, false, false, name, true));
    private static McpToolDefinition RunTests() =>
        new("run_tests", EmptyJson(), false, false, false, "tests", false);
    private static JsonElement EmptyJson() => JsonSerializer.SerializeToElement(new { });
    private const string Id = "qa-workflow";

    private sealed class FakeToolClient(params McpToolDefinition[] definitions) : IReadOnlyToolClient
    {
        public List<string> Calls { get; } = [];
        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<McpToolDefinition>>(definitions);
        public Task<ToolCallResult> CallAsync(string name, JsonElement arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add(name);
            return Task.FromResult(new ToolCallResult(true, "bounded-test-output"));
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
