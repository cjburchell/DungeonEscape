using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonEscape.AgentOrchestrator;

public sealed class LlmAgentExecutor(ILlmClient client, LlmExecutionOptions options) : IAgentExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<AgentExecutionResult<EngineerToReviewer>> ExecuteEngineerAsync(
        ManagerToEngineer assignment, AgentExecutionContext context, CancellationToken cancellationToken) =>
        ExecuteAsync<EngineerToReviewer>(AgentRole.UnityEngineer, assignment, context, EngineerSchema, "engineer_result",
            ValidateEngineer, cancellationToken);

    public Task<AgentExecutionResult<ReviewerToManager>> ExecuteReviewerAsync(
        ReviewAssignment assignment, AgentExecutionContext context, CancellationToken cancellationToken) =>
        ExecuteAsync<ReviewerToManager>(AgentRole.Reviewer, assignment, context, ReviewerSchema, "reviewer_result",
            ValidateReviewer, cancellationToken);

    private async Task<AgentExecutionResult<T>> ExecuteAsync<T>(AgentRole role, object assignment,
        AgentExecutionContext context, string schemaJson, string schemaName, Func<T, bool> validate,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var toolCallCount = 0;
        await Trace(context, "LlmExecutionStarted",
            $"provider={client.ProviderName};model={options.Model};status=started", role, cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            var advertised = await context.Tools.ListAllowedToolsAsync(role, timeout.Token);
            var tools = advertised.Select(tool => new LlmToolDefinition(tool.Name, tool.Description,
                tool.InputSchema)).ToArray();
            using var schema = JsonDocument.Parse(schemaJson);
            var instructions = AgentPrompts.For(role) + $"""

                You have at most {options.MaximumToolCalls} tool calls for this execution. Conserve them,
                use paths exactly as returned by tools, and finalize with the required structured result
                when the tool-result envelope reports zero remaining calls. Tool results are JSON envelopes
                containing succeeded, content/error, remainingToolCalls, and instruction fields.
                """;
            var request = new LlmConversationRequest(role, options.Model, instructions,
                JsonSerializer.Serialize(assignment, JsonOptions), tools, schemaName,
                schema.RootElement.Clone());
            await using var conversation = client.StartConversation(request);
            IReadOnlyList<LlmToolOutput> outputs = [];
            while (true)
            {
                var response = await conversation.RespondAsync(outputs, timeout.Token);
                await TraceUsage(context, role, response, toolCallCount, cancellationToken);
                if (response.Refused)
                    return await Failure<T>(context, role, "refused", stopwatch, toolCallCount,
                        "Model refused the specialist assignment.", cancellationToken);

                if (response.ToolCalls.Count > 0)
                {
                    if (toolCallCount + response.ToolCalls.Count > options.MaximumToolCalls)
                    {
                        await Trace(context, "LlmToolBudgetExhausted",
                            $"provider={client.ProviderName};model={options.Model};limit={options.MaximumToolCalls}",
                            role, cancellationToken);
                        return await Failure<T>(context, role, "tool_budget_exhausted", stopwatch,
                            toolCallCount, "Specialist tool-call budget exhausted.", cancellationToken);
                    }
                    var next = new List<LlmToolOutput>();
                    foreach (var call in response.ToolCalls)
                    {
                        toolCallCount++;
                        await Trace(context, "LlmToolRequested", $"name={call.Name};count={toolCallCount}",
                            role, cancellationToken);
                        ToolCallResult toolResult;
                        try
                        {
                            using var arguments = JsonDocument.Parse(call.ArgumentsJson);
                            if (arguments.RootElement.ValueKind != JsonValueKind.Object)
                                throw new JsonException("Tool arguments must be an object.");
                            toolResult = await context.Tools.CallAsync(role, call.Name,
                                arguments.RootElement.Clone(), timeout.Token);
                        }
                        catch (JsonException)
                        {
                            toolResult = new(false, "", "Tool arguments were invalid JSON.");
                        }
                        var remaining = options.MaximumToolCalls - toolCallCount;
                        next.Add(new(call.CallId, JsonSerializer.Serialize(new
                        {
                            succeeded = toolResult.Succeeded,
                            content = toolResult.Succeeded ? toolResult.Content : null,
                            error = toolResult.Succeeded ? null : toolResult.Error ?? "Tool call rejected.",
                            remainingToolCalls = remaining,
                            instruction = remaining == 0
                                ? "No tool calls remain. Return the final structured result now."
                                : "Conserve the remaining tool budget and finalize once sufficient evidence is available."
                        })));
                    }
                    outputs = next;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(response.OutputText))
                    return await Failure<T>(context, role, "malformed_output", stopwatch, toolCallCount,
                        "Model returned no structured result.", cancellationToken);
                T? result;
                try { result = JsonSerializer.Deserialize<T>(response.OutputText, JsonOptions); }
                catch (JsonException)
                {
                    return await Failure<T>(context, role, "malformed_output", stopwatch, toolCallCount,
                        "Model returned malformed structured output.", cancellationToken);
                }
                if (result is null || !validate(result))
                    return await Failure<T>(context, role, "malformed_output", stopwatch, toolCallCount,
                        "Model returned incomplete structured output.", cancellationToken);
                await Trace(context, "LlmExecutionCompleted",
                    $"provider={client.ProviderName};model={options.Model};status=success;toolCalls={toolCallCount};durationMs={stopwatch.ElapsedMilliseconds}",
                    role, cancellationToken);
                return AgentExecutionResult<T>.Success(role, result);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await Failure<T>(context, role, "timeout", stopwatch, toolCallCount,
                "Specialist execution timed out.", CancellationToken.None);
        }
        catch (LlmProviderException exception)
        {
            return await Failure<T>(context, role, $"provider_{exception.Kind.ToString().ToLowerInvariant()}",
                stopwatch, toolCallCount, ProviderFailureMessage(exception.Kind), cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            return await Failure<T>(context, role, "provider_unknown", stopwatch, toolCallCount,
                "Provider request failed safely.", cancellationToken);
        }
    }

    private async Task<AgentExecutionResult<T>> Failure<T>(AgentExecutionContext context, AgentRole role,
        string status, Stopwatch stopwatch, int toolCalls, string error, CancellationToken cancellationToken)
    {
        await Trace(context, "LlmExecutionCompleted",
            $"provider={client.ProviderName};model={options.Model};status={status};toolCalls={toolCalls};durationMs={stopwatch.ElapsedMilliseconds}",
            role, cancellationToken);
        return AgentExecutionResult<T>.Failure(role, error);
    }

    private async Task TraceUsage(AgentExecutionContext context, AgentRole role, LlmResponse response,
        int toolCalls, CancellationToken cancellationToken)
    {
        var usage = response.Usage is null ? "usage=unavailable"
            : $"inputTokens={response.Usage.InputTokens};outputTokens={response.Usage.OutputTokens};totalTokens={response.Usage.TotalTokens}";
        var id = string.IsNullOrWhiteSpace(response.ResponseId) ? "unavailable" : response.ResponseId;
        await Trace(context, "LlmResponseReceived",
            $"provider={client.ProviderName};model={options.Model};responseId={id};toolCalls={toolCalls};{usage}",
            role, cancellationToken);
    }

    private static ValueTask Trace(AgentExecutionContext context, string eventType, string detail,
        AgentRole role, CancellationToken cancellationToken) => context.Trace.EmitAsync(
        WorkflowTraceEvent.Create(context.WorkflowId, eventType, detail, role, context.Attempt),
        cancellationToken);

    private static bool ValidateEngineer(EngineerToReviewer value) =>
        !string.IsNullOrWhiteSpace(value.WorkflowId) && value.Attempt > 0
        && !string.IsNullOrWhiteSpace(value.Interpretation)
        && !string.IsNullOrWhiteSpace(value.ProposedSolution)
        && value.Components is not null && value.Assumptions is not null
        && value.ProposedValidation is not null && value.KnownRisks is not null && value.ToolUses is not null;

    private static bool ValidateReviewer(ReviewerToManager value) =>
        !string.IsNullOrWhiteSpace(value.WorkflowId) && value.Attempt > 0
        && Enum.IsDefined(value.Verdict) && value.Findings is not null && value.RequiredChanges is not null
        && value.ResidualRisks is not null
        && (value.Verdict != ReviewVerdict.RequestChanges || value.RequiredChanges.Count > 0);

    private static string ProviderFailureMessage(LlmProviderErrorKind kind) => kind switch
    {
        LlmProviderErrorKind.Authentication => "Provider authentication failed.",
        LlmProviderErrorKind.RateLimit => "Provider rate limit was reached.",
        LlmProviderErrorKind.Network => "Provider network request failed.",
        LlmProviderErrorKind.Server => "Provider service failed.",
        LlmProviderErrorKind.InvalidResponse => "Provider returned an invalid response.",
        _ => "Provider request failed safely."
    };

    private const string EngineerSchema = """
        {"type":"object","properties":{"workflowId":{"type":"string"},"attempt":{"type":"integer"},"interpretation":{"type":"string"},"proposedSolution":{"type":"string"},"components":{"type":"array","items":{"type":"string"}},"assumptions":{"type":"array","items":{"type":"string"}},"proposedValidation":{"type":"array","items":{"type":"string"}},"knownRisks":{"type":"array","items":{"type":"string"}},"toolUses":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"succeeded":{"type":"boolean"}},"required":["name","succeeded"],"additionalProperties":false}}},"required":["workflowId","attempt","interpretation","proposedSolution","components","assumptions","proposedValidation","knownRisks","toolUses"],"additionalProperties":false}
        """;

    private const string ReviewerSchema = """
        {"type":"object","properties":{"workflowId":{"type":"string"},"attempt":{"type":"integer"},"verdict":{"type":"string","enum":["Approve","RequestChanges","Inconclusive"]},"findings":{"type":"array","items":{"type":"string"}},"requiredChanges":{"type":"array","items":{"type":"string"}},"residualRisks":{"type":"array","items":{"type":"string"}}},"required":["workflowId","attempt","verdict","findings","requiredChanges","residualRisks"],"additionalProperties":false}
        """;
}
