using System.Diagnostics;
using System.Text.Json;
using DungeonEscape.Mcp.Security;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class StdioTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandshakeListsExactlySevenBoundedToolsAndCallsThem(bool configured)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.Name != "DungeonEscape.Mcp.Tests") directory = directory.Parent;
        Assert.NotNull(directory);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(directory.Parent!.FullName, "DungeonEscape.Mcp", "bin", configuration, "net10.0", "DungeonEscape.Mcp.dll");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetTempPath()
        };
        start.ArgumentList.Add(dll);
        var expectedRoot = directory.Parent.Parent!.FullName;
        string? fixture = null;
        if (configured)
        {
            fixture = Path.Combine(Path.GetTempPath(), "DungeonEscape MCP stdio", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(fixture, "DungeonEscape.Core"));
            var settings = Directory.CreateDirectory(Path.Combine(fixture, "DungeonEscape.Unity", "ProjectSettings"));
            var scenes = Directory.CreateDirectory(Path.Combine(fixture, "DungeonEscape.Unity", "Assets", "Scenes"));
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.sln"), "");
            File.WriteAllText(Path.Combine(settings.FullName, "ProjectVersion.txt"), "m_EditorVersion: 6000.4.4f1\n");
            File.WriteAllText(Path.Combine(settings.FullName, "EditorBuildSettings.asset"),
                "EditorBuildSettings:\n  m_Scenes:\n  - enabled: 1\n    path: Assets/Scenes/Fixture.unity\n");
            File.WriteAllText(Path.Combine(scenes.FullName, "Fixture.unity"), "scene content is not read");
            var results = Directory.CreateDirectory(Path.Combine(fixture, "TestResults"));
            File.WriteAllText(Path.Combine(results.FullName, "playmode-log.txt"),
                "Error: controlled fixture Authorization: Bearer secret-token useful context\n");
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.Core", "Fixture.cs"), "class FixtureSymbol { }\n");
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.Core", "Readable.cs"),
                "line one\nknown read line\nline three\n");
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.Core", "Unicode.cs"),
                string.Concat(Enumerable.Repeat(char.ConvertFromUtf32(0x1F9ED), 180)) +
                "UnicodeNeedle" +
                string.Concat(Enumerable.Repeat(char.ConvertFromUtf32(0x1F9ED), 180)));
            RunGit(fixture, "init");
            RunGit(fixture, "config", "user.name", "DungeonEscape Tests");
            RunGit(fixture, "config", "user.email", "tests@example.invalid");
            RunGit(fixture, "add", ".");
            RunGit(fixture, "commit", "-m", "fixture");
            File.AppendAllText(Path.Combine(fixture, "DungeonEscape.Core", "Readable.cs"), "unstaged line\n");
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.Core", "Staged.cs"), "staged content\n");
            RunGit(fixture, "add", "DungeonEscape.Core/Staged.cs");
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.Core", "Untracked.cs"), "untracked content\n");
            start.ArgumentList.Add("--repository-root");
            start.ArgumentList.Add(fixture);
            expectedRoot = fixture;
        }
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Send(new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new
            {
                protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "DungeonEscape.Tests", version = "1.0" }
            }});
            var initialized = await Receive(1);
            Assert.True(initialized.GetProperty("result").TryGetProperty("serverInfo", out _));
            await Send(new { jsonrpc = "2.0", method = "notifications/initialized" });
            await Send(new { jsonrpc = "2.0", id = 2, method = "tools/list", @params = new { } });
            var listed = await Receive(2);
            var tools = listed.GetProperty("result").GetProperty("tools").EnumerateArray().ToArray();
            Assert.Equal(7, tools.Length);
            Assert.Equal(McpCapabilityCatalog.All.Select(definition => definition.Name).OrderBy(name => name),
                tools.Select(tool => tool.GetProperty("name").GetString()).OrderBy(name => name));
            foreach (var definition in McpCapabilityCatalog.All)
            {
                var registered = Assert.Single(tools,
                    tool => tool.GetProperty("name").GetString() == definition.Name);
                var annotations = registered.GetProperty("annotations");
                Assert.Equal(definition.ReadOnly, annotations.GetProperty("readOnlyHint").GetBoolean());
                Assert.Equal(definition.Destructive, annotations.GetProperty("destructiveHint").GetBoolean());
                Assert.Equal(definition.Idempotent, annotations.GetProperty("idempotentHint").GetBoolean());
                Assert.Equal(definition.OpenWorld, annotations.GetProperty("openWorldHint").GetBoolean());
            }
            var projectTool = tools.Single(tool => tool.GetProperty("name").GetString() == "get_project_info");
            Assert.Empty(projectTool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject());
            AssertStrictlyReadOnly(projectTool);
            var searchTool = tools.Single(tool => tool.GetProperty("name").GetString() == "search_code");
            var searchSchema = searchTool.GetProperty("inputSchema");
            Assert.Equal(["maxResults", "path", "query"], searchSchema.GetProperty("properties")
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
            Assert.Equal("query", Assert.Single(searchSchema.GetProperty("required").EnumerateArray()).GetString());
            AssertStrictlyReadOnly(searchTool);
            var readTool = tools.Single(tool => tool.GetProperty("name").GetString() == "read_script");
            var readSchema = readTool.GetProperty("inputSchema");
            Assert.Equal(["lineCount", "path", "startLine"], readSchema.GetProperty("properties")
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
            Assert.Equal("path", Assert.Single(readSchema.GetProperty("required").EnumerateArray()).GetString());
            AssertStrictlyReadOnly(readTool);
            var scenesTool = tools.Single(tool => tool.GetProperty("name").GetString() == "list_scenes");
            Assert.Empty(scenesTool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject());
            AssertStrictlyReadOnly(scenesTool);
            var consoleTool = tools.Single(tool => tool.GetProperty("name").GetString() == "get_unity_console");
            var consoleSchema = consoleTool.GetProperty("inputSchema");
            Assert.Equal(["maxEntries", "minimumSeverity"], consoleSchema.GetProperty("properties")
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
            Assert.False(consoleSchema.TryGetProperty("required", out var consoleRequired)
                && consoleRequired.GetArrayLength() > 0);
            AssertStrictlyReadOnly(consoleTool);
            var gitDiffTool = tools.Single(tool => tool.GetProperty("name").GetString() == "get_git_diff");
            var gitDiffSchema = gitDiffTool.GetProperty("inputSchema");
            Assert.Equal(["includeStaged", "includeUnstaged", "includeUntrackedMetadata", "maxFiles",
                "maxPatchCharacters"], gitDiffSchema.GetProperty("properties").EnumerateObject()
                .Select(property => property.Name).OrderBy(name => name));
            Assert.False(gitDiffSchema.TryGetProperty("required", out var gitDiffRequired)
                && gitDiffRequired.GetArrayLength() > 0);
            AssertStrictlyReadOnly(gitDiffTool);
            var runTestsTool = tools.Single(tool => tool.GetProperty("name").GetString() == "run_tests");
            var runTestsSchema = runTestsTool.GetProperty("inputSchema");
            Assert.Equal(["suite", "timeoutSeconds"], runTestsSchema.GetProperty("properties")
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
            Assert.Equal("suite", Assert.Single(runTestsSchema.GetProperty("required")
                .EnumerateArray()).GetString());
            Assert.Equal(["core", "mcp", "orchestrator"], runTestsSchema.GetProperty("properties")
                .GetProperty("suite").GetProperty("enum").EnumerateArray()
                .Select(value => value.GetString()).OrderBy(value => value));
            AssertBoundedExecution(runTestsTool);
            await Send(new { jsonrpc = "2.0", id = 3, method = "tools/call", @params = new { name = "get_project_info", arguments = new { } } });
            var called = (await Receive(3)).GetProperty("result");
            Assert.False(called.TryGetProperty("isError", out var error) && error.GetBoolean());
            Assert.Contains("DungeonEscape", called.GetRawText());
            Assert.Contains("6000.4.4f1", called.GetRawText());
            using var metadata = JsonDocument.Parse(called.GetProperty("content")[0].GetProperty("text").GetString()!);
            Assert.Equal(expectedRoot, metadata.RootElement.EnumerateObject()
                .Single(property => property.Name.Equals("RepositoryRoot", StringComparison.OrdinalIgnoreCase)).Value.GetString());
            var query = configured ? "FixtureSymbol" : "ProjectInfoService";
            var path = configured ? "DungeonEscape.Core" : "tools/DungeonEscape.Mcp";
            await Send(new { jsonrpc = "2.0", id = 4, method = "tools/call", @params = new
            {
                name = "search_code", arguments = new { query, path, maxResults = 5 }
            }});
            var searched = (await Receive(4)).GetProperty("result");
            Assert.False(searched.TryGetProperty("isError", out var searchError) && searchError.GetBoolean());
            Assert.Contains(query, searched.GetRawText());

            var readPath = configured
                ? "DungeonEscape.Core/Readable.cs"
                : "tools/DungeonEscape.Mcp/Services/ProjectInfoService.cs";
            await Send(new { jsonrpc = "2.0", id = 9, method = "tools/call", @params = new
            {
                name = "read_script", arguments = new { path = readPath, startLine = 1, lineCount = 3 }
            }});
            var read = (await Receive(9)).GetProperty("result");
            Assert.False(read.TryGetProperty("isError", out var readError) && readError.GetBoolean());
            using var readResult = JsonDocument.Parse(read.GetProperty("content")[0].GetProperty("text").GetString()!);
            Assert.Equal(readPath, readResult.RootElement.GetProperty("path").GetString());
            Assert.InRange(readResult.RootElement.GetProperty("linesReturned").GetInt32(), 1, 3);

            await Send(new { jsonrpc = "2.0", id = 11, method = "tools/call", @params = new
            {
                name = "list_scenes", arguments = new { }
            }});
            var listedScenes = (await Receive(11)).GetProperty("result");
            Assert.False(listedScenes.TryGetProperty("isError", out var scenesError) && scenesError.GetBoolean());
            using var sceneResult = JsonDocument.Parse(
                listedScenes.GetProperty("content")[0].GetProperty("text").GetString()!);
            var scenePath = sceneResult.RootElement.GetProperty("scenes")[0].GetProperty("path").GetString();
            Assert.StartsWith("DungeonEscape.Unity/Assets/", scenePath);
            Assert.EndsWith(".unity", scenePath);

            await Send(new { jsonrpc = "2.0", id = 12, method = "tools/call", @params = new
            {
                name = "get_unity_console", arguments = new { maxEntries = 5, minimumSeverity = "info" }
            }});
            var console = (await Receive(12)).GetProperty("result");
            Assert.False(console.TryGetProperty("isError", out var consoleError) && consoleError.GetBoolean());
            using var consoleResult = JsonDocument.Parse(
                console.GetProperty("content")[0].GetProperty("text").GetString()!);
            Assert.True(consoleResult.RootElement.TryGetProperty("status", out _));
            if (configured)
            {
                var consoleText = consoleResult.RootElement.GetRawText();
                Assert.Contains("controlled fixture", consoleText);
                Assert.Contains("[REDACTED]", consoleText);
                Assert.DoesNotContain("secret-token", consoleText);
            }

            if (configured)
            {
                await Send(new { jsonrpc = "2.0", id = 5, method = "tools/call", @params = new
                {
                    name = "search_code", arguments = new { query = "UnicodeNeedle", path = "DungeonEscape.Core" }
                }});
                var unicodeSearch = (await Receive(5)).GetProperty("result");
                Assert.False(unicodeSearch.TryGetProperty("isError", out var unicodeError) && unicodeError.GetBoolean());
                using var unicodeResult = JsonDocument.Parse(
                    unicodeSearch.GetProperty("content")[0].GetProperty("text").GetString()!);
                var snippet = unicodeResult.RootElement.GetProperty("matches")[0].GetProperty("snippet").GetString()!;
                Assert.Contains("UnicodeNeedle", snippet);
                Assert.True(snippet.Length <= 240);

                await Send(new { jsonrpc = "2.0", id = 13, method = "tools/call", @params = new
                {
                    name = "get_git_diff", arguments = new { maxFiles = 10, maxPatchCharacters = 4096 }
                }});
                var gitDiff = (await Receive(13)).GetProperty("result");
                Assert.False(gitDiff.TryGetProperty("isError", out var gitDiffError)
                    && gitDiffError.GetBoolean());
                using var gitDiffResult = JsonDocument.Parse(
                    gitDiff.GetProperty("content")[0].GetProperty("text").GetString()!);
                Assert.Equal("available", gitDiffResult.RootElement.GetProperty("status").GetString());
                var gitDiffText = gitDiffResult.RootElement.GetRawText();
                Assert.Contains("DungeonEscape.Core/Readable.cs", gitDiffText);
                Assert.Contains("DungeonEscape.Core/Staged.cs", gitDiffText);
                Assert.Contains("DungeonEscape.Core/Untracked.cs", gitDiffText);
                Assert.DoesNotContain(fixture!.Replace('\\', '/'), gitDiffText, StringComparison.OrdinalIgnoreCase);
            }

            if (!configured)
            {
                await Send(new { jsonrpc = "2.0", id = 14, method = "tools/call", @params = new
                {
                    name = "run_tests", arguments = new { suite = "core", timeoutSeconds = 120 }
                }});
                var testCall = (await Receive(14)).GetProperty("result");
                Assert.False(testCall.TryGetProperty("isError", out var testError) && testError.GetBoolean());
                using var testResult = JsonDocument.Parse(
                    testCall.GetProperty("content")[0].GetProperty("text").GetString()!);
                Assert.Equal("passed", testResult.RootElement.GetProperty("outcome").GetString());
                Assert.Equal(0, testResult.RootElement.GetProperty("exitCode").GetInt32());

                await Send(new { jsonrpc = "2.0", id = 15, method = "tools/call", @params = new
                {
                    name = "run_tests", arguments = new { suite = "core; whoami" }
                }});
                Assert.True((await Receive(15)).GetProperty("result").GetProperty("isError").GetBoolean());
            }

            await Send(new { jsonrpc = "2.0", id = 6, method = "tools/call", @params = new
            {
                name = "search_code", arguments = new { query = "x", path = "../outside" }
            }});
            Assert.True((await Receive(6)).GetProperty("result").GetProperty("isError").GetBoolean());
            await Send(new { jsonrpc = "2.0", id = 7, method = "tools/call", @params = new
            {
                name = "search_code", arguments = new { query = "x", path = "README.md" }
            }});
            Assert.True((await Receive(7)).GetProperty("result").GetProperty("isError").GetBoolean());
            await Send(new { jsonrpc = "2.0", id = 10, method = "tools/call", @params = new
            {
                name = "read_script", arguments = new { path = "../outside.cs" }
            }});
            Assert.True((await Receive(10)).GetProperty("result").GetProperty("isError").GetBoolean());
            await Send(new { jsonrpc = "2.0", id = 8, method = "tools/call", @params = new { name = "read_file", arguments = new { path = "outside" } } });
            var rejected = await Receive(8);
            Assert.True(rejected.TryGetProperty("error", out _) || rejected.GetProperty("result").GetProperty("isError").GetBoolean());
        }
        finally
        {
            process.StandardInput.Close();
            try { await process.WaitForExitAsync(timeout.Token); }
            finally
            {
                if (!process.HasExited) process.Kill(true);
                if (fixture is not null)
                {
                    foreach (var path in Directory.EnumerateFiles(fixture, "*", SearchOption.AllDirectories))
                        File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(fixture, true);
                }
            }
        }
        Assert.Equal(0, process.ExitCode);
        await stderr;

        async Task Send(object message)
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), timeout.Token);
            await process.StandardInput.FlushAsync(timeout.Token);
        }

        async Task<JsonElement> Receive(int id)
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
                Assert.NotNull(line);
                using var document = JsonDocument.Parse(line);
                var message = document.RootElement;
                if (message.TryGetProperty("id", out var responseId) && responseId.GetInt32() == id)
                    return message.Clone();
            }
        }

        static void AssertStrictlyReadOnly(JsonElement tool)
        {
            var annotations = tool.GetProperty("annotations");
            Assert.True(annotations.GetProperty("readOnlyHint").GetBoolean());
            Assert.True(annotations.GetProperty("idempotentHint").GetBoolean());
            Assert.False(annotations.GetProperty("destructiveHint").GetBoolean());
            Assert.False(annotations.GetProperty("openWorldHint").GetBoolean());
        }

        static void AssertBoundedExecution(JsonElement tool)
        {
            var annotations = tool.GetProperty("annotations");
            Assert.False(annotations.GetProperty("readOnlyHint").GetBoolean());
            Assert.False(annotations.GetProperty("idempotentHint").GetBoolean());
            Assert.False(annotations.GetProperty("destructiveHint").GetBoolean());
            Assert.False(annotations.GetProperty("openWorldHint").GetBoolean());
        }

        static void RunGit(string workingDirectory, params string[] arguments)
        {
            var git = new ProcessStartInfo("git")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var argument in arguments) git.ArgumentList.Add(argument);
            using var process = Process.Start(git)!;
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"git failed: {stdout} {stderr}");
        }
    }

    [Theory]
    [InlineData("--repository-root")]
    [InlineData("--repository-root", "relative/path")]
    [InlineData("--unknown")]
    [InlineData("--repository-root", "<missing>")]
    [InlineData("--repository-root", "<parent-escape>")]
    public async Task InvalidConfigurationFailsBeforeTransportWithoutFallback(params string[] args)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.Name != "DungeonEscape.Mcp.Tests") directory = directory.Parent;
        Assert.NotNull(directory);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetTempPath()
        };
        start.ArgumentList.Add(Path.Combine(directory.Parent!.FullName, "DungeonEscape.Mcp", "bin", configuration, "net10.0", "DungeonEscape.Mcp.dll"));
        foreach (var argument in args)
            start.ArgumentList.Add(argument switch
            {
                "<missing>" => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                "<parent-escape>" => Path.Combine(directory.Parent.Parent!.FullName, "DungeonEscape.Core", "..", ".."),
                _ => argument
            });
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) process.Kill(true); }
        Assert.Equal(2, process.ExitCode);
        Assert.Empty(await stdout);
        Assert.Contains("MCP startup configuration error:", await stderr);
    }
}
