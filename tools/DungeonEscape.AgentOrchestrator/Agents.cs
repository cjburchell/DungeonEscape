namespace DungeonEscape.AgentOrchestrator;

public interface IAgentExecutor
{
    Task<AgentExecutionResult<EngineerToReviewer>> ExecuteEngineerAsync(
        ManagerToEngineer assignment, AgentExecutionContext context, CancellationToken cancellationToken);

    Task<AgentExecutionResult<ReviewerToManager>> ExecuteReviewerAsync(
        ReviewAssignment assignment, AgentExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Executes caller-supplied deterministic scripts. This proves orchestration behavior only; it does not
/// perform autonomous reasoning and is the seam where a future LLM-backed executor can be introduced.
/// </summary>
public sealed class ScriptedAgentExecutor(
    Func<ManagerToEngineer, AgentExecutionContext, CancellationToken,
        Task<AgentExecutionResult<EngineerToReviewer>>> engineer,
    Func<ReviewAssignment, AgentExecutionContext, CancellationToken,
        Task<AgentExecutionResult<ReviewerToManager>>> reviewer) : IAgentExecutor
{
    public Task<AgentExecutionResult<EngineerToReviewer>> ExecuteEngineerAsync(
        ManagerToEngineer assignment, AgentExecutionContext context, CancellationToken cancellationToken) =>
        engineer(assignment, context, cancellationToken);

    public Task<AgentExecutionResult<ReviewerToManager>> ExecuteReviewerAsync(
        ReviewAssignment assignment, AgentExecutionContext context, CancellationToken cancellationToken) =>
        reviewer(assignment, context, cancellationToken);
}
