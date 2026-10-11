using System.Text.Json;

namespace DungeonEscape.AgentOrchestrator;

public enum AgentRole { Manager, UnityEngineer, Reviewer }
public enum ReviewVerdict { Approve, RequestChanges, Inconclusive }
public enum WorkflowStatus { Completed, Incomplete, NeedsHumanDecision, Failed }
public enum WorkflowStage { Created, Engineer, Reviewer, Completed }

public sealed record WorkflowRequest(string Task, IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> Constraints, IReadOnlyList<string> SuppliedContext);

public sealed record ManagerToEngineer(string WorkflowId, int Attempt, string Task,
    IReadOnlyList<string> AcceptanceCriteria, IReadOnlyList<string> Constraints,
    IReadOnlyList<string> RelevantContext, IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredChanges);

public sealed record EngineerToReviewer(string WorkflowId, int Attempt, string Interpretation,
    string ProposedSolution, IReadOnlyList<string> Components, IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ProposedValidation, IReadOnlyList<string> KnownRisks,
    IReadOnlyList<ToolUse> ToolUses);

public sealed record ReviewAssignment(string WorkflowId, int Attempt, string Task,
    IReadOnlyList<string> AcceptanceCriteria, IReadOnlyList<string> Constraints,
    EngineerToReviewer EngineerResult, IReadOnlyList<string> AllowedTools);

public sealed record ReviewerToManager(string WorkflowId, int Attempt, ReviewVerdict Verdict,
    IReadOnlyList<string> Findings, IReadOnlyList<string> RequiredChanges,
    IReadOnlyList<string> ResidualRisks);

public sealed record ToolUse(string Name, bool Succeeded);

public sealed record AgentExecutionResult<T>(AgentRole Role, T? Output, string? Error)
{
    public static AgentExecutionResult<T> Success(AgentRole role, T output) => new(role, output, null);
    public static AgentExecutionResult<T> Failure(AgentRole role, string error) => new(role, default, error);
}

public sealed record WorkflowResult(string WorkflowId, WorkflowStatus Status, int EngineerAttempts,
    int ReviewerAttempts, EngineerToReviewer? Proposal, ReviewerToManager? Review, string Summary);

public sealed record ToolDescriptor(string Name, bool ReadOnly, bool Destructive, bool OpenWorld);
public sealed record ToolCallResult(bool Succeeded, string Content, string? Error = null);

public sealed record AgentExecutionContext(string WorkflowId, int Attempt, IAgentToolGateway Tools,
    IWorkflowTraceSink Trace);

public sealed record WorkflowTraceEvent(string WorkflowId, string? RunId, string EventType,
    AgentRole? Role, int? Attempt, string Detail, DateTimeOffset Timestamp)
{
    public static WorkflowTraceEvent Create(string workflowId, string eventType, string detail,
        AgentRole? role = null, int? attempt = null, string? runId = null) =>
        new(workflowId, runId, eventType, role, attempt, detail, DateTimeOffset.UtcNow);
}

public sealed record McpToolDefinition(string Name, JsonElement InputSchema, bool ReadOnly,
    bool Destructive, bool OpenWorld);
