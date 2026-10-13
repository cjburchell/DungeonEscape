using System.Diagnostics;
using System.Text.Json;
using DungeonEscape.WriteApproval;

namespace DungeonEscape.AgentOrchestrator;

public interface IReadOnlyToolClient : IAsyncDisposable
{
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken);
    Task<ToolCallResult> CallAsync(string name, JsonElement arguments, CancellationToken cancellationToken);
}

public interface IAgentToolGateway
{
    IReadOnlyList<string> AllowedTools { get; }
    IReadOnlyList<string> AllowedToolsFor(AgentRole role);
    Task<IReadOnlyList<McpToolDefinition>> ListAllowedToolsAsync(AgentRole role,
        CancellationToken cancellationToken);
    Task<ToolCallResult> CallAsync(AgentRole role, string name, JsonElement arguments,
        CancellationToken cancellationToken);
}

public sealed class RoleBasedToolGateway(IReadOnlyToolClient client, IWorkflowTraceSink trace,
    string workflowId) : IAgentToolGateway
{
    private sealed record CapabilityPolicy(string Name, bool ReadOnly, bool Destructive,
        bool OpenWorld, bool Idempotent);

    private static readonly CapabilityPolicy[] InspectionPolicy =
        new[] { "get_project_info", "search_code", "read_script", "list_scenes", "get_unity_console", "get_git_diff" }
        .Select(name => new CapabilityPolicy(name, true, false, false, true)).ToArray();
    private static readonly CapabilityPolicy RunTestsPolicy =
        new("run_tests", false, false, false, false);
    private static readonly IReadOnlyDictionary<AgentRole, IReadOnlyList<CapabilityPolicy>> Policy =
        new Dictionary<AgentRole, IReadOnlyList<CapabilityPolicy>>
        {
            [AgentRole.Manager] = [],
            [AgentRole.UnityEngineer] = InspectionPolicy,
            [AgentRole.Reviewer] = InspectionPolicy,
            [AgentRole.QA] = [.. InspectionPolicy, RunTestsPolicy]
        };

    public IReadOnlyList<string> AllowedTools =>
        ["get_project_info", "search_code", "read_script", "list_scenes", "get_unity_console", "get_git_diff"];

    public IReadOnlyList<string> AllowedToolsFor(AgentRole role) => Policy.TryGetValue(role, out var policy)
        ? policy.Select(item => item.Name).ToArray() : [];

    public async Task<IReadOnlyList<McpToolDefinition>> ListAllowedToolsAsync(AgentRole role,
        CancellationToken cancellationToken)
    {
        if (!Policy.TryGetValue(role, out var allowedTools)) return [];
        var definitions = await client.ListToolsAsync(cancellationToken);
        return definitions.Where(tool => allowedTools.Any(expected => Matches(expected, tool))).ToArray();
    }

    public async Task<ToolCallResult> CallAsync(AgentRole role, string name, JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var roleSupported = Policy.TryGetValue(role, out var allowedTools);
        var expected = roleSupported ? allowedTools!.SingleOrDefault(item => item.Name == name) : null;
        var allowed = expected is not null;
        var definitions = allowed ? await client.ListToolsAsync(cancellationToken) : [];
        var definition = definitions.SingleOrDefault(tool => tool.Name == name);
        if (!allowed || definition is null || !Matches(expected!, definition))
        {
            await trace.EmitAsync(WorkflowTraceEvent.Create(workflowId, "ToolCallRejected",
                $"{name}: not allowlisted or advertised metadata does not match policy", role), cancellationToken);
            return new(false, "", "Tool is not authorized for this role or its safety metadata is unexpected.");
        }

        await trace.EmitAsync(WorkflowTraceEvent.Create(workflowId, "ToolCallStarted", name, role),
            cancellationToken);
        var result = await client.CallAsync(name, arguments, cancellationToken);
        await trace.EmitAsync(WorkflowTraceEvent.Create(workflowId, "ToolCallCompleted",
            $"{name}: {(result.Succeeded ? "succeeded" : "failed")}", role), cancellationToken);
        return result;
    }

    private static bool Matches(CapabilityPolicy expected, McpToolDefinition actual) =>
        expected.Name == actual.Name && expected.ReadOnly == actual.ReadOnly
        && expected.Destructive == actual.Destructive && expected.OpenWorld == actual.OpenWorld
        && expected.Idempotent == actual.Idempotent;
}

/// <summary>Small stdio client for the existing server's list/call surface.</summary>
public sealed class McpStdioToolClient : IReadOnlyToolClient
{
    private readonly Process process;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Task stderrDrain;
    private int nextId;
    private IReadOnlyList<McpToolDefinition>? tools;
    public IWriteApprovalIssuer WriteApprovalIssuer { get; }

    private McpStdioToolClient(Process process, IWriteApprovalIssuer writeApprovalIssuer)
    {
        this.process = process;
        WriteApprovalIssuer = writeApprovalIssuer;
        stderrDrain = DrainStandardErrorAsync(process.StandardError, lifetime.Token);
    }

    public static async Task<McpStdioToolClient> StartAsync(string serverDll, string repositoryRoot,
        CancellationToken cancellationToken) =>
        await StartAsync(serverDll, repositoryRoot, WriteApprovalKey.Generate(), cancellationToken);

    public static async Task<McpStdioToolClient> StartAsync(string serverDll, string repositoryRoot,
        WriteApprovalKey approvalKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approvalKey);
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetTempPath()
        };
        start.ArgumentList.Add(serverDll);
        start.ArgumentList.Add("--repository-root");
        start.ArgumentList.Add(repositoryRoot);
        start.Environment[WriteApprovalKey.EnvironmentVariableName] = approvalKey.EncodeForChildEnvironment();
        var client = new McpStdioToolClient(Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the MCP server."),
            new HmacWriteApprovalIssuer(approvalKey));
        try
        {
            await client.SendAsync("initialize", new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "DungeonEscape.AgentOrchestrator", version = "1.0" }
            }, cancellationToken);
            await client.SendNotificationAsync("notifications/initialized", new { }, cancellationToken);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken)
    {
        if (tools is not null) return tools;
        var result = await SendAsync("tools/list", new { }, cancellationToken);
        tools = result.GetProperty("tools").EnumerateArray().Select(tool =>
        {
            var annotations = tool.GetProperty("annotations");
            return new McpToolDefinition(tool.GetProperty("name").GetString()!,
                tool.GetProperty("inputSchema").Clone(),
                annotations.GetProperty("readOnlyHint").GetBoolean(),
                annotations.GetProperty("destructiveHint").GetBoolean(),
                annotations.GetProperty("openWorldHint").GetBoolean(),
                tool.TryGetProperty("description", out var description) ? description.GetString() ?? "" : "",
                annotations.GetProperty("idempotentHint").GetBoolean());
        }).ToArray();
        return tools;
    }

    public async Task<ToolCallResult> CallAsync(string name, JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var result = await SendAsync("tools/call", new { name, arguments }, cancellationToken);
        var error = result.TryGetProperty("isError", out var isError) && isError.GetBoolean();
        var content = result.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        return new(!error, content, error ? content : null);
    }

    private async Task<JsonElement> SendAsync(string method, object parameters,
        CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref nextId);
        await WriteAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, cancellationToken);
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken)
                ?? throw new InvalidOperationException("MCP server closed stdout unexpectedly.");
            using var document = JsonDocument.Parse(line);
            var message = document.RootElement;
            if (!message.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id) continue;
            if (message.TryGetProperty("error", out var error))
                throw new InvalidOperationException("MCP request failed: " + error.GetRawText());
            return message.GetProperty("result").Clone();
        }
    }

    private Task SendNotificationAsync(string method, object parameters, CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken);

    private async Task WriteAsync(object message, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    private static async Task DrainStandardErrorAsync(StreamReader standardError,
        CancellationToken cancellationToken)
    {
        // Drain and discard diagnostics continuously. Never retain arbitrary child-process output.
        var buffer = new char[4096];
        try
        {
            while (await standardError.ReadAsync(buffer.AsMemory(), cancellationToken) > 0) { }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        process.StandardInput.Close();
        if (!process.HasExited)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                await process.WaitForExitAsync();
            }
        }
        try { await stderrDrain.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException)
        {
            lifetime.Cancel();
            await stderrDrain;
        }
        process.Dispose();
        lifetime.Dispose();
    }
}
