using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class RunTestsServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeRunTests",
        Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(ApprovedTestSuite.Core, "DungeonEscape.Core.Test/DungeonEscape.Core.Test.csproj")]
    [InlineData(ApprovedTestSuite.Mcp, "tools/DungeonEscape.Mcp.Tests/DungeonEscape.Mcp.Tests.csproj")]
    [InlineData(ApprovedTestSuite.Orchestrator,
        "tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj")]
    public void ApprovedSuitesResolveOnlyFixedDotnetCommands(ApprovedTestSuite suite, string project)
    {
        var command = ApprovedTestSuiteCatalog.Resolve(suite, root);
        Assert.Equal("dotnet", command.Executable);
        Assert.Equal(root, command.WorkingDirectory);
        Assert.Equal(["test", project, "--no-restore", "--logger", "console;verbosity=minimal"],
            command.Arguments);
        Assert.DoesNotContain(command.Arguments, argument => argument.Contains("--filter", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownSuiteCannotResolveACommand()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApprovedTestSuiteCatalog.Resolve((ApprovedTestSuite)int.MaxValue, root));
    }

    [Fact]
    public async Task SuccessAndFailureUseAuthoritativeExecutorOutcome()
    {
        CreateIdentity();
        var passed = await Service(new TestProcessResult("passed", 0, 123, false, false, false,
            "passed output", "", [])).RunAsync(ApprovedTestSuite.Core);
        var failed = await Service(new TestProcessResult("failed", 7, 456, false, false, false,
            "failed output", "failure", [])).RunAsync(ApprovedTestSuite.Mcp);

        Assert.Equal("passed", passed.Outcome);
        Assert.Equal(0, passed.ExitCode);
        Assert.Equal(123, passed.DurationMilliseconds);
        Assert.Equal("failed", failed.Outcome);
        Assert.Equal(7, failed.ExitCode);
        Assert.Equal(456, failed.DurationMilliseconds);
        Assert.Empty(failed.FailedTests);
    }

    [Theory]
    [InlineData("timed-out", true, false)]
    [InlineData("cancelled", false, true)]
    [InlineData("process-error", false, false)]
    public async Task ExceptionalProcessOutcomesRemainDistinct(string status, bool timedOut, bool cancelled)
    {
        CreateIdentity();
        var result = await Service(new TestProcessResult(status, null, 42, timedOut, cancelled, true,
            new string('o', 100), new string('e', 100), ["bounded warning"]))
            .RunAsync(ApprovedTestSuite.Orchestrator, 10);

        Assert.Equal(status, result.Status);
        Assert.Equal(status, result.Outcome);
        Assert.Equal(timedOut, result.TimedOut);
        Assert.Equal(cancelled, result.Cancelled);
        Assert.True(result.OutputTruncated);
        Assert.Equal(["bounded warning"], result.Warnings);
    }

    [Fact]
    public async Task UnavailableRepositoryDoesNotStartProcess()
    {
        var executor = new FakeExecutor(new TestProcessResult("passed", 0, 1, false, false, false,
            "", "", []));
        var service = new RunTestsService(new ProjectInfoService(Path.GetTempPath(), null), executor);

        var result = await service.RunAsync(ApprovedTestSuite.Core);

        Assert.Equal("unavailable", result.Status);
        Assert.Equal(0, executor.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(301)]
    public async Task TimeoutOutsideFixedBoundsIsRejected(int timeout)
    {
        CreateIdentity();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Service(Passed()).RunAsync(ApprovedTestSuite.Core, timeout));
    }

    [Fact]
    public async Task InvalidEnumIsRejectedBeforeExecution()
    {
        CreateIdentity();
        var executor = new FakeExecutor(Passed());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new RunTestsService(ProjectInfo(), executor).RunAsync((ApprovedTestSuite)int.MaxValue));
        Assert.Equal(0, executor.Calls);
    }

    [Fact]
    public void OutputLimitsAreFiniteAndTotalAtMost192KiB()
    {
        Assert.Equal(128 * 1024, ApprovedTestSuiteExecutor.MaximumStandardOutputBytes);
        Assert.Equal(64 * 1024, ApprovedTestSuiteExecutor.MaximumStandardErrorBytes);
        Assert.Equal(192 * 1024, ApprovedTestSuiteExecutor.MaximumStandardOutputBytes
            + ApprovedTestSuiteExecutor.MaximumStandardErrorBytes);
    }

    [Theory]
    [InlineData(ApprovedTestSuiteExecutor.MaximumStandardOutputBytes)]
    [InlineData(ApprovedTestSuiteExecutor.MaximumStandardErrorBytes)]
    public async Task VeryNoisyProcessStreamsRetainOnlyBoundedPrefix(int maximum)
    {
        var noise = Enumerable.Range(0, maximum * 3).Select(index => (byte)(index % 251)).ToArray();
        await using var stream = new MemoryStream(noise);

        var result = await BoundedTestOutputReader.ReadAsync(stream, maximum);

        Assert.Equal(maximum, result.Bytes.Length);
        Assert.True(result.Truncated);
        Assert.Equal(noise[..maximum], result.Bytes);
    }

    [Fact]
    public async Task RealExecutorProcessStartFailureIsStructured()
    {
        CreateIdentity();
        var result = await new ApprovedTestSuiteExecutor("definitely-not-a-real-dotnet-executable")
            .ExecuteAsync(ApprovedTestSuite.Core, root, TimeSpan.FromSeconds(10), default);
        Assert.Equal("process-error", result.Status);
        Assert.Null(result.ExitCode);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task RealExecutorDistinguishesTimeoutAndExternalCancellation()
    {
        CreateIdentity();
        var executor = new ApprovedTestSuiteExecutor();
        var timedOut = await executor.ExecuteAsync(ApprovedTestSuite.Core, root, TimeSpan.Zero, default);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await executor.ExecuteAsync(ApprovedTestSuite.Core, root,
            TimeSpan.FromSeconds(10), cancellation.Token);

        Assert.Equal("timed-out", timedOut.Status);
        Assert.True(timedOut.TimedOut);
        Assert.False(timedOut.Cancelled);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.False(cancelled.TimedOut);
        Assert.True(cancelled.Cancelled);
    }

    private RunTestsService Service(TestProcessResult result) => new(ProjectInfo(), new FakeExecutor(result));
    private ProjectInfoService ProjectInfo() => new(Path.GetTempPath(), root);
    private static TestProcessResult Passed() => new("passed", 0, 1, false, false, false, "", "", []);

    private void CreateIdentity()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    private sealed class FakeExecutor(TestProcessResult result) : IApprovedTestSuiteExecutor
    {
        public int Calls { get; private set; }
        public Task<TestProcessResult> ExecuteAsync(ApprovedTestSuite suite, string repositoryRoot,
            TimeSpan timeout, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
