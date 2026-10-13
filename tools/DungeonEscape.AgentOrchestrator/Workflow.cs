namespace DungeonEscape.AgentOrchestrator;

public sealed class OrchestrationWorkflow(string workflowId, IAgentExecutor executor,
    IAgentToolGateway tools, IWorkflowTraceSink trace)
{
    public const int MaximumEngineerAttempts = 2;
    public const int MaximumReviewerAttempts = 2;

    public async Task<WorkflowResult> RunAsync(WorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        await Emit("WorkflowStarted", "Read-only deterministic orchestration started.");
        var stage = WorkflowStage.Created;
        EngineerToReviewer? proposal = null;
        ReviewerToManager? review = null;
        IReadOnlyList<string> requiredChanges = [];

        for (var attempt = 1; attempt <= MaximumEngineerAttempts; attempt++)
        {
            await TransitionAsync(stage, WorkflowStage.Engineer, attempt == 1
                ? "Manager dispatched initial analysis."
                : "Manager dispatched the single allowed revision.");
            stage = WorkflowStage.Engineer;
            if (attempt > 1)
            {
                await Emit("RetryConsumed", "Engineer retry budget consumed.", AgentRole.Manager, attempt);
            }

            var managerToEngineer = new ManagerToEngineer(workflowId, attempt, request.Task,
                request.AcceptanceCriteria, request.Constraints, request.SuppliedContext,
                tools.AllowedTools, requiredChanges);
            var engineerRun = Guid.NewGuid().ToString("N");
            await Emit("AgentDispatched", "Unity Engineer received a typed assignment.",
                AgentRole.UnityEngineer, attempt, engineerRun);
            AgentExecutionResult<EngineerToReviewer> engineerResult;
            try
            {
                engineerResult = await executor.ExecuteEngineerAsync(managerToEngineer,
                    new(workflowId, attempt, tools, trace), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await Emit("AgentCompleted", "Unity Engineer execution failed.",
                    AgentRole.UnityEngineer, attempt, engineerRun);
                return await Fail("Unity Engineer execution failed: " + exception.GetType().Name,
                    attempt, attempt - 1, proposal, review);
            }
            await Emit("AgentCompleted", engineerResult.Error ?? "Unity Engineer completed.",
                engineerResult.Role, attempt, engineerRun);

            if (engineerResult.Role != AgentRole.UnityEngineer)
                return await Fail("Engineer executor attempted to act as or self-approve through another role.",
                    attempt, attempt - 1, proposal, review);
            if (engineerResult.Error is not null)
                return await Fail("Unity Engineer execution failed: " + engineerResult.Error,
                    attempt, attempt - 1, proposal, review);
            proposal = engineerResult.Output;
            if (!Valid(proposal, workflowId, attempt))
                return await Fail("Unity Engineer returned malformed output.", attempt, attempt - 1,
                    proposal, review);
            var validProposal = proposal!;

            await TransitionAsync(stage, WorkflowStage.Reviewer,
                "Engineer result sent for independent review.");
            stage = WorkflowStage.Reviewer;
            var assignment = new ReviewAssignment(workflowId, attempt, request.Task,
                request.AcceptanceCriteria, request.Constraints, validProposal, tools.AllowedTools);
            var reviewerRun = Guid.NewGuid().ToString("N");
            await Emit("AgentDispatched", "Reviewer received requirements and the typed Engineer result.",
                AgentRole.Reviewer, attempt, reviewerRun);
            AgentExecutionResult<ReviewerToManager> reviewerResult;
            try
            {
                reviewerResult = await executor.ExecuteReviewerAsync(assignment,
                    new(workflowId, attempt, tools, trace), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await Emit("AgentCompleted", "Reviewer execution failed.",
                    AgentRole.Reviewer, attempt, reviewerRun);
                return await Fail("Reviewer execution failed: " + exception.GetType().Name,
                    attempt, attempt, proposal, review);
            }
            await Emit("AgentCompleted", reviewerResult.Error ?? "Reviewer completed.",
                reviewerResult.Role, attempt, reviewerRun);

            if (reviewerResult.Role != AgentRole.Reviewer)
                return await Fail("Reviewer executor returned an unauthorized role.", attempt, attempt,
                    proposal, review);
            if (reviewerResult.Error is not null)
                return await Fail("Reviewer execution failed: " + reviewerResult.Error,
                    attempt, attempt, proposal, review);
            review = reviewerResult.Output;
            if (!Valid(review, workflowId, attempt))
                return await Fail("Reviewer returned malformed output.", attempt, attempt, proposal, review);
            var validReview = review!;

            switch (validReview.Verdict)
            {
                case ReviewVerdict.Approve:
                    return await RunQa(request, attempt, validProposal, validReview, stage, cancellationToken);
                case ReviewVerdict.Inconclusive:
                    await TransitionAsync(stage, WorkflowStage.Completed,
                        "Review was inconclusive; returning to human.");
                    return await Complete(WorkflowStatus.Incomplete, attempt, attempt, validProposal, validReview,
                        "Review was inconclusive; human evaluation is required.");
                case ReviewVerdict.RequestChanges when attempt < MaximumEngineerAttempts:
                    requiredChanges = validReview.RequiredChanges;
                    await Emit("ChangeRequested", "Reviewer requested one bounded Engineer revision.",
                        AgentRole.Reviewer, attempt);
                    break;
                case ReviewVerdict.RequestChanges:
                    await Emit("ChangeRequested", "Reviewer requested changes after the retry limit.",
                        AgentRole.Reviewer, attempt);
                    await Emit("HumanDecisionRequired", "Second REQUEST_CHANGES cannot trigger another retry.",
                        AgentRole.Manager, attempt);
                    await TransitionAsync(stage, WorkflowStage.Completed,
                        "Retry limit reached; human decision required.");
                    return await Complete(WorkflowStatus.NeedsHumanDecision, attempt, attempt, validProposal, validReview,
                        "The bounded revision was not approved; no further automatic retry is allowed.");
            }
        }

        return await Fail("Workflow exhausted its bounded attempts.", MaximumEngineerAttempts,
            MaximumReviewerAttempts, proposal, review);
    }

    private async Task<WorkflowResult> RunQa(WorkflowRequest request, int attempt,
        EngineerToReviewer proposal, ReviewerToManager review, WorkflowStage stage,
        CancellationToken cancellationToken)
    {
        await TransitionAsync(stage, WorkflowStage.QA, "Reviewer approved; independent QA validation dispatched.");
        var assignment = new QaAssignment(workflowId, attempt, request.Task, request.AcceptanceCriteria,
            request.Constraints, proposal, review, tools.AllowedToolsFor(AgentRole.QA));
        var runId = Guid.NewGuid().ToString("N");
        await Emit("AgentDispatched", "QA received requirements and typed Engineer/Reviewer results.",
            AgentRole.QA, attempt, runId);
        AgentExecutionResult<QaToManager> qaResult;
        try
        {
            qaResult = await executor.ExecuteQaAsync(assignment,
                new(workflowId, attempt, tools, trace), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await Emit("AgentCompleted", "QA execution failed.", AgentRole.QA, attempt, runId);
            return await Fail("QA execution failed: " + exception.GetType().Name,
                attempt, attempt, proposal, review, null, 1);
        }
        await Emit("AgentCompleted", qaResult.Error ?? "QA completed.", qaResult.Role, attempt, runId);
        if (qaResult.Role != AgentRole.QA)
            return await Fail("QA executor returned an unauthorized role.", attempt, attempt,
                proposal, review, qaResult.Output, 1);
        if (qaResult.Error is not null)
            return await Fail("QA execution failed: " + qaResult.Error, attempt, attempt,
                proposal, review, qaResult.Output, 1);
        var qa = qaResult.Output;
        if (!Valid(qa, workflowId, attempt))
            return await Fail("QA returned malformed output.", attempt, attempt, proposal, review, qa, 1);
        var validQa = qa!;
        await Emit("QaOutcome", validQa.Outcome.ToString(), AgentRole.QA, attempt);
        switch (validQa.Outcome)
        {
            case QaOutcome.Pass:
                await TransitionAsync(WorkflowStage.QA, WorkflowStage.Completed,
                    "QA passed available baseline validation.");
                return await Complete(WorkflowStatus.Completed, attempt, attempt, proposal, review,
                    "Reviewer approved and QA passed available baseline validation.", 1, validQa);
            case QaOutcome.Fail:
            case QaOutcome.Inconclusive:
                await Emit("HumanDecisionRequired",
                    $"QA returned {validQa.Outcome}; no automatic repair loop is allowed.",
                    AgentRole.Manager, attempt);
                await TransitionAsync(WorkflowStage.QA, WorkflowStage.Completed,
                    "QA requires human evaluation.");
                return await Complete(WorkflowStatus.NeedsHumanDecision, attempt, attempt, proposal, review,
                    $"QA returned {validQa.Outcome}; human evaluation is required.", 1, validQa);
            default:
                return await Fail("QA returned an unsupported outcome.", attempt, attempt,
                    proposal, review, validQa, 1);
        }
    }

    private async ValueTask TransitionAsync(WorkflowStage current, WorkflowStage next, string reason)
    {
        var allowed = (current, next) switch
        {
            (WorkflowStage.Created, WorkflowStage.Engineer) => true,
            (WorkflowStage.Engineer, WorkflowStage.Reviewer) => true,
            (WorkflowStage.Reviewer, WorkflowStage.Engineer) => true,
            (WorkflowStage.Reviewer, WorkflowStage.Completed) => true,
            (WorkflowStage.Reviewer, WorkflowStage.QA) => true,
            (WorkflowStage.QA, WorkflowStage.Completed) => true,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException($"Invalid workflow transition: {current} -> {next}.");
        await trace.EmitAsync(WorkflowTraceEvent.Create(workflowId, "Transition",
            $"{current} -> {next}: {reason}"));
    }

    private async Task<WorkflowResult> Complete(WorkflowStatus status, int engineers, int reviewers,
        EngineerToReviewer proposal, ReviewerToManager review, string summary, int qaAttempts = 0,
        QaToManager? qa = null)
    {
        await Emit("WorkflowCompleted", $"{status}: {summary}");
        return new(workflowId, status, engineers, reviewers, qaAttempts, proposal, review, qa, summary);
    }

    private async Task<WorkflowResult> Fail(string summary, int engineers, int reviewers,
        EngineerToReviewer? proposal, ReviewerToManager? review, QaToManager? qa = null,
        int qaAttempts = 0)
    {
        await Emit("WorkflowCompleted", $"Failed: {summary}");
        return new(workflowId, WorkflowStatus.Failed, engineers, reviewers, qaAttempts,
            proposal, review, qa, summary);
    }

    private ValueTask Emit(string type, string detail, AgentRole? role = null, int? attempt = null,
        string? runId = null) => trace.EmitAsync(WorkflowTraceEvent.Create(workflowId, type, detail,
            role, attempt, runId));

    private static bool Valid(EngineerToReviewer? value, string id, int attempt) =>
        value is not null && value.WorkflowId == id && value.Attempt == attempt
        && !string.IsNullOrWhiteSpace(value.Interpretation)
        && !string.IsNullOrWhiteSpace(value.ProposedSolution);

    private static bool Valid(ReviewerToManager? value, string id, int attempt) =>
        value is not null && value.WorkflowId == id && value.Attempt == attempt
        && (value.Verdict != ReviewVerdict.RequestChanges || value.RequiredChanges.Count > 0);

    private static bool Valid(QaToManager? value, string id, int attempt) =>
        value is not null && value.WorkflowId == id && value.Attempt == attempt
        && QaResultValidator.IsValid(value);
}
