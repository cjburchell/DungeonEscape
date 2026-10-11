namespace DungeonEscape.AgentOrchestrator;

public interface IWorkflowTraceSink
{
    ValueTask EmitAsync(WorkflowTraceEvent traceEvent, CancellationToken cancellationToken = default);
}

public sealed class InMemoryWorkflowTraceSink : IWorkflowTraceSink
{
    private readonly List<WorkflowTraceEvent> events = [];
    public IReadOnlyList<WorkflowTraceEvent> Events => events;

    public ValueTask EmitAsync(WorkflowTraceEvent traceEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Add(traceEvent);
        return ValueTask.CompletedTask;
    }
}

public sealed class ConsoleWorkflowTraceSink : IWorkflowTraceSink
{
    public ValueTask EmitAsync(WorkflowTraceEvent traceEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"TRACE {traceEvent.EventType} role={traceEvent.Role?.ToString() ?? "system"} " +
            $"attempt={traceEvent.Attempt?.ToString() ?? "-"} detail={traceEvent.Detail}");
        return ValueTask.CompletedTask;
    }
}
