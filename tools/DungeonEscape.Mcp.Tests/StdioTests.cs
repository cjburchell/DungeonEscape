using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class StdioTests
{
    [Fact]
    public async Task HandshakeListsExactlyOneParameterlessReadOnlyToolAndCallsIt()
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
            await Send(new { jsonrpc = "2.0", id = 4, method = "tools/call", @params = new { name = "read_file", arguments = new { path = "outside" } } });
            var rejected = await Receive(4);
            Assert.True(rejected.TryGetProperty("error", out _) || rejected.GetProperty("result").GetProperty("isError").GetBoolean());
        }
        finally
        {
            process.StandardInput.Close();
            try { await process.WaitForExitAsync(timeout.Token); }
            finally { if (!process.HasExited) process.Kill(true); }
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
}
