using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class StdioTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandshakeListsExactlyOneParameterlessReadOnlyToolAndCallsIt(bool configured)
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
            File.WriteAllText(Path.Combine(fixture, "DungeonEscape.sln"), "");
            File.WriteAllText(Path.Combine(settings.FullName, "ProjectVersion.txt"), "m_EditorVersion: 6000.4.4f1\n");
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
            var tool = Assert.Single(listed.GetProperty("result").GetProperty("tools").EnumerateArray());
            Assert.Equal("get_project_info", tool.GetProperty("name").GetString());
            var schema = tool.GetProperty("inputSchema");
            Assert.Empty(schema.GetProperty("properties").EnumerateObject());
            Assert.True(tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
            await Send(new { jsonrpc = "2.0", id = 3, method = "tools/call", @params = new { name = "get_project_info", arguments = new { } } });
            var called = (await Receive(3)).GetProperty("result");
            Assert.False(called.TryGetProperty("isError", out var error) && error.GetBoolean());
            Assert.Contains("DungeonEscape", called.GetRawText());
            Assert.Contains("6000.4.4f1", called.GetRawText());
            using var metadata = JsonDocument.Parse(called.GetProperty("content")[0].GetProperty("text").GetString()!);
            Assert.Equal(expectedRoot, metadata.RootElement.EnumerateObject()
                .Single(property => property.Name.Equals("RepositoryRoot", StringComparison.OrdinalIgnoreCase)).Value.GetString());
            await Send(new { jsonrpc = "2.0", id = 4, method = "tools/call", @params = new { name = "read_file", arguments = new { path = "outside" } } });
            var rejected = await Receive(4);
            Assert.True(rejected.TryGetProperty("error", out _) || rejected.GetProperty("result").GetProperty("isError").GetBoolean());
        }
        finally
        {
            process.StandardInput.Close();
            try { await process.WaitForExitAsync(timeout.Token); }
            finally
            {
                if (!process.HasExited) process.Kill(true);
                if (fixture is not null) Directory.Delete(fixture, true);
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
