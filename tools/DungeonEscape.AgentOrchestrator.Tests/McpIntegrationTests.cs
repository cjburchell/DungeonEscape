using System.Text.Json;
using Xunit;

namespace DungeonEscape.AgentOrchestrator.Tests;

public sealed class McpIntegrationTests
{
    [Fact]
    public async Task ExistingMcpServerAdvertisesSixToolsWhileGitDiffRemainsUnauthorized()
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
        Assert.Equal(6, definitions.Count);
        var projectTool = definitions.Single(tool => tool.Name == "get_project_info");
        AssertStrictlyReadOnly(projectTool);
        Assert.Empty(projectTool.InputSchema.GetProperty("properties").EnumerateObject());
        var searchTool = definitions.Single(tool => tool.Name == "search_code");
        AssertStrictlyReadOnly(searchTool);
        Assert.Equal(["maxResults", "path", "query"], searchTool.InputSchema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal("query", Assert.Single(searchTool.InputSchema.GetProperty("required")
            .EnumerateArray()).GetString());
        var readTool = definitions.Single(tool => tool.Name == "read_script");
        AssertStrictlyReadOnly(readTool);
        Assert.Equal(["lineCount", "path", "startLine"], readTool.InputSchema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal("path", Assert.Single(readTool.InputSchema.GetProperty("required")
            .EnumerateArray()).GetString());
        var scenesTool = definitions.Single(tool => tool.Name == "list_scenes");
        AssertStrictlyReadOnly(scenesTool);
        Assert.Empty(scenesTool.InputSchema.GetProperty("properties").EnumerateObject());
        var consoleTool = definitions.Single(tool => tool.Name == "get_unity_console");
        AssertStrictlyReadOnly(consoleTool);
        Assert.Equal(["maxEntries", "minimumSeverity"], consoleTool.InputSchema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        var gitDiffTool = definitions.Single(tool => tool.Name == "get_git_diff");
        AssertStrictlyReadOnly(gitDiffTool);
        Assert.Equal(["includeStaged", "includeUnstaged", "includeUntrackedMetadata", "maxFiles",
            "maxPatchCharacters"], gitDiffTool.InputSchema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name));

        var trace = new InMemoryWorkflowTraceSink();
        var gateway = new RoleBasedToolGateway(client, trace, "integration");
        var result = await gateway.CallAsync(AgentRole.UnityEngineer, "get_project_info",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.True(result.Succeeded);
        Assert.Contains("DungeonEscape", result.Content);

        var engineerSearch = await gateway.CallAsync(AgentRole.UnityEngineer, "search_code",
            JsonSerializer.SerializeToElement(new { query = "ProjectInfoService" }), timeout.Token);
        Assert.True(engineerSearch.Succeeded);
        Assert.Contains("ProjectInfoService", engineerSearch.Content);

        var reviewerSearch = await gateway.CallAsync(AgentRole.Reviewer, "search_code",
            JsonSerializer.SerializeToElement(new { query = "ProjectInfoService", maxResults = 1 }),
            timeout.Token);
        Assert.True(reviewerSearch.Succeeded);
        Assert.Contains("ProjectInfoService", reviewerSearch.Content);

        var managerSearch = await gateway.CallAsync(AgentRole.Manager, "search_code",
            JsonSerializer.SerializeToElement(new { query = "ProjectInfoService" }), timeout.Token);
        Assert.False(managerSearch.Succeeded);

        var readArguments = JsonSerializer.SerializeToElement(new
        {
            path = "tools/DungeonEscape.Mcp/Services/ProjectInfoService.cs", startLine = 1, lineCount = 30
        });
        var engineerRead = await gateway.CallAsync(AgentRole.UnityEngineer, "read_script", readArguments,
            timeout.Token);
        Assert.True(engineerRead.Succeeded);
        Assert.Contains("ProjectInfoService", engineerRead.Content);

        var reviewerRead = await gateway.CallAsync(AgentRole.Reviewer, "read_script", readArguments,
            timeout.Token);
        Assert.True(reviewerRead.Succeeded);
        Assert.Contains("ProjectInfoService", reviewerRead.Content);

        var managerRead = await gateway.CallAsync(AgentRole.Manager, "read_script", readArguments,
            timeout.Token);
        Assert.False(managerRead.Succeeded);

        var engineerScenes = await gateway.CallAsync(AgentRole.UnityEngineer, "list_scenes",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.True(engineerScenes.Succeeded);
        Assert.Contains("DungeonEscape.Unity/Assets/DungeonEscape/Scenes/Boot.unity", engineerScenes.Content);

        var reviewerScenes = await gateway.CallAsync(AgentRole.Reviewer, "list_scenes",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.True(reviewerScenes.Succeeded);

        var managerScenes = await gateway.CallAsync(AgentRole.Manager, "list_scenes",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.False(managerScenes.Succeeded);

        var consoleArguments = JsonSerializer.SerializeToElement(new
            { maxEntries = 5, minimumSeverity = "warning" });
        var engineerConsole = await gateway.CallAsync(AgentRole.UnityEngineer, "get_unity_console",
            consoleArguments, timeout.Token);
        Assert.True(engineerConsole.Succeeded);

        var reviewerConsole = await gateway.CallAsync(AgentRole.Reviewer, "get_unity_console",
            consoleArguments, timeout.Token);
        Assert.True(reviewerConsole.Succeeded);

        var managerConsole = await gateway.CallAsync(AgentRole.Manager, "get_unity_console",
            JsonSerializer.SerializeToElement(new { }), timeout.Token);
        Assert.False(managerConsole.Succeeded);

        var gitDiffArguments = JsonSerializer.SerializeToElement(new
            { maxFiles = 10, maxPatchCharacters = 4096 });
        var engineerGitDiff = await gateway.CallAsync(AgentRole.UnityEngineer, "get_git_diff",
            gitDiffArguments, timeout.Token);
        Assert.True(engineerGitDiff.Succeeded);
        var reviewerGitDiff = await gateway.CallAsync(AgentRole.Reviewer, "get_git_diff",
            gitDiffArguments, timeout.Token);
        Assert.True(reviewerGitDiff.Succeeded);
        var managerGitDiff = await gateway.CallAsync(AgentRole.Manager, "get_git_diff",
            gitDiffArguments, timeout.Token);
        Assert.False(managerGitDiff.Succeeded);
        Assert.Contains(trace.Events,
            e => e.EventType == "ToolCallRejected" && e.Role == AgentRole.Manager
                && e.Detail.StartsWith("get_git_diff:", StringComparison.Ordinal));

        Assert.DoesNotContain(trace.Events,
            e => e.Detail.Contains(engineerConsole.Content, StringComparison.Ordinal));
        Assert.DoesNotContain(trace.Events,
            e => e.Detail.Contains(reviewerConsole.Content, StringComparison.Ordinal));
        Assert.DoesNotContain(trace.Events,
            e => e.Detail.Contains(engineerGitDiff.Content, StringComparison.Ordinal));
        Assert.DoesNotContain(trace.Events,
            e => e.Detail.Contains(reviewerGitDiff.Content, StringComparison.Ordinal));

        var rejected = await gateway.CallAsync(AgentRole.Reviewer, "read_file",
            JsonSerializer.SerializeToElement(new { path = "anything" }), timeout.Token);
        Assert.False(rejected.Succeeded);
        Assert.Contains(trace.Events, e => e.EventType == "ToolCallRejected");
    }

    [Fact]
    public async Task SustainedMcpStderrDoesNotBlockListCallOrShutdown()
    {
        var (dll, root) = ServerPaths();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = await McpStdioToolClient.StartAsync(dll, root, timeout.Token);

        var definitions = await client.ListToolsAsync(timeout.Token);
        Assert.Equal(6, definitions.Count);
        for (var index = 0; index < 750; index++)
        {
            var result = await client.CallAsync("get_project_info",
                JsonSerializer.SerializeToElement(new { }), timeout.Token);
            Assert.True(result.Succeeded);
        }

        await client.DisposeAsync().AsTask().WaitAsync(timeout.Token);
    }

    private static void AssertStrictlyReadOnly(McpToolDefinition tool)
    {
        Assert.True(tool.ReadOnly);
        Assert.False(tool.Destructive);
        Assert.False(tool.OpenWorld);
    }

    private static (string Dll, string Root) ServerPaths()
    {
        var testProject = FindAncestor(AppContext.BaseDirectory, "DungeonEscape.AgentOrchestrator.Tests");
        var toolsDirectory = testProject.Parent!;
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        return (Path.Combine(toolsDirectory.FullName, "DungeonEscape.Mcp", "bin", configuration,
            "net10.0", "DungeonEscape.Mcp.dll"), toolsDirectory.Parent!.FullName);
    }

    private static DirectoryInfo FindAncestor(string start, string name)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            if (directory.Name == name) return directory;
        throw new InvalidOperationException($"Could not find {name} from {start}.");
    }
}
