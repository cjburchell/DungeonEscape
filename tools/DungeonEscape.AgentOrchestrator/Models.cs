using System.Text.Json;

namespace DungeonEscape.AgentOrchestrator;

public enum AgentRole { Manager, UnityEngineer, Reviewer, QA }
public enum ReviewVerdict { Approve, RequestChanges, Inconclusive }
public enum QaOutcome { Pass, Fail, Inconclusive }
public enum WorkflowStatus { Completed, Incomplete, NeedsHumanDecision, Failed }
public enum WorkflowStage { Created, Engineer, Reviewer, QA, Completed }

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

public sealed record QaAssignment(string WorkflowId, int Attempt, string Task,
    IReadOnlyList<string> AcceptanceCriteria, IReadOnlyList<string> Constraints,
    EngineerToReviewer EngineerResult, ReviewerToManager ReviewerResult,
    IReadOnlyList<string> AllowedTools);

public sealed record QaTestEvidence(string Suite, string Status, int? ExitCode,
    long DurationMilliseconds, IReadOnlyList<string> FailedTests, bool Truncated, string Summary);

public sealed record QaToManager(string WorkflowId, int Attempt, QaOutcome Outcome,
    IReadOnlyList<string> TestSuitesRun, IReadOnlyList<QaTestEvidence> TestResults,
    IReadOnlyList<string> ValidationFindings, IReadOnlyList<string> SkippedChecks,
    IReadOnlyList<string> ResidualRisks, IReadOnlyList<ToolUse> ToolUses);

public sealed record ToolUse(string Name, bool Succeeded);

public sealed record AgentExecutionResult<T>(AgentRole Role, T? Output, string? Error)
{
    public static AgentExecutionResult<T> Success(AgentRole role, T output) => new(role, output, null);
    public static AgentExecutionResult<T> Failure(AgentRole role, string error) => new(role, default, error);
}

public sealed record WorkflowResult(string WorkflowId, WorkflowStatus Status, int EngineerAttempts,
    int ReviewerAttempts, int QaAttempts, EngineerToReviewer? Proposal, ReviewerToManager? Review,
    QaToManager? QA, string Summary);

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
    bool Destructive, bool OpenWorld, string Description = "", bool Idempotent = true);

public sealed record LlmExecutionOptions(string Model, TimeSpan Timeout, int MaximumToolCalls)
{
    public static LlmExecutionOptions Create(string model, int timeoutSeconds = 120,
        int maximumToolCalls = 8)
    {
        if (string.IsNullOrWhiteSpace(model) || model.Length > 128 || model.Any(char.IsControl))
            throw new ArgumentException("A valid model identifier is required.", nameof(model));
        if (timeoutSeconds is < 1 or > 300) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        if (maximumToolCalls is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(maximumToolCalls));
        return new(model, TimeSpan.FromSeconds(timeoutSeconds), maximumToolCalls);
    }
}

public sealed record LlmToolDefinition(string Name, string Description, JsonElement InputSchema);
public sealed record LlmToolCall(string CallId, string Name, string ArgumentsJson);
public sealed record LlmToolOutput(string CallId, string Content);
public sealed record LlmUsage(int InputTokens, int OutputTokens, int TotalTokens);
public sealed record LlmResponse(string? ResponseId, string? OutputText,
    IReadOnlyList<LlmToolCall> ToolCalls, bool Refused = false, LlmUsage? Usage = null);

public sealed record LlmConversationRequest(AgentRole Role, string Model, string Instructions,
    string Input, IReadOnlyList<LlmToolDefinition> Tools, string OutputSchemaName,
    JsonElement OutputSchema);

public interface ILlmConversation : IAsyncDisposable
{
    Task<LlmResponse> RespondAsync(IReadOnlyList<LlmToolOutput> toolOutputs,
        CancellationToken cancellationToken);
}

public interface ILlmClient
{
    string ProviderName { get; }
    ILlmConversation StartConversation(LlmConversationRequest request);
}

public enum LlmProviderErrorKind { Authentication, RateLimit, Network, Server, InvalidResponse, Unknown }

public sealed class LlmProviderException(LlmProviderErrorKind kind, string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public LlmProviderErrorKind Kind { get; } = kind;
}
