using System.Text.Json;
using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class McpIntegrationTests
{
    [Fact]
    public async Task ExistingMcpServerProvidesOnlyAllowedReadOnlyMetadataTool()
    {
        var testProject = FindAncestor(AppContext.BaseDirectory, "DungeonEscape.AgentOrchestrator.Tests");
        var toolsDirectory = testProject.Parent!;
        var root = toolsDirectory.Parent!.FullName;
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(toolsDirectory.FullName, "DungeonEscape.Mcp", "bin", configuration,
            "net10.0", "DungeonEscape.Mcp.dll");
        Assert.True(File.Exists(dll), $"Expected MCP server at {dll}");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpStdioToolClient.StartAsync(dll, root, timeout.Token);
        var definitions = await client.ListToolsAsync(timeout.Token);
        var tool = Assert.Single(definitions);
        Assert.Equal("get_project_info", tool.Name);
        Assert.True(tool.ReadOnly);
        Assert.False(tool.Destructive);
        Assert.False(tool.OpenWorld);

        var trace = new InMemoryWorkflowTraceSink();
        var gateway = new RoleBasedToolGateway(client, trace, "integration");
        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "get_project_info",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.True(result.Succeeded);
        Assert.Contains("DungeonEscape", result.Content);

        var rejected = await gateway.CallAsync(AgentRole.Reviewer, "read_file",
            JsonSerializer.SerializeToElement(new { path = "anything" }), timeout.Token);
        Assert.False(rejected.Succeeded);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallRejected");
    }

    private static DirectoryInfo FindAncestor(string start, string name)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            if (directory.Name == name) return directory;
        throw new InvalidOperationException($"Could not find {name} from {start}.");
    }
}
