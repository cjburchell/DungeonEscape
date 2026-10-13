using System.Security.Cryptography;
using System.Text;
using DungeonEscape.Mcp.Services;
using DungeonEscape.WriteApproval;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class ControlledWriteServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeControlledWrite",
        Guid.NewGuid().ToString("N"));
    private readonly MutableClock clock = new();
    private readonly HmacWriteApprovalIssuer issuer;
    private readonly ControlledWriteService service;

    public ControlledWriteServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity"));
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "solution\n");
        var key = WriteApprovalKey.Generate();
        issuer = new(key, clock);
        var verifier = new HmacWriteApprovalVerifier(key, clock);
        service = CreateService(verifier);
    }

    [Fact]
    public void PrepareReturnsExactBoundedPreviewWithoutModification()
    {
        var file = Write("DungeonEscape.Core/Example.cs", "old line\n");
        var proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new line\n", Hash("old line\n"));

        Assert.Equal("prepared", proposal.Status);
        Assert.StartsWith("sha256:", proposal.ProposalId);
        Assert.True(proposal.ContentChanged);
        Assert.Contains("-old line", proposal.Preview);
        Assert.Contains("+new line", proposal.Preview);
        Assert.False(proposal.PreviewTruncated);
        Assert.Equal("old line\n", File.ReadAllText(file));
    }

    [Fact]
    public void PreviewIsBoundedAndNoOpIsExplicit()
    {
        Write("DungeonEscape.Core/Example.cs", "same\n");
        var unchanged = service.Prepare("DungeonEscape.Core/Example.cs", "same\n", Hash("same\n"));
        Assert.False(unchanged.ContentChanged);

        var large = string.Concat(Enumerable.Repeat("changed-content-0123456789\n", 30000));
        var bounded = service.Prepare("DungeonEscape.Core/Example.cs", large, Hash("same\n"));
        Assert.True(bounded.PreviewTruncated);
        Assert.True(bounded.Preview.Length <= ControlledWriteService.MaximumPreviewCharacters);
    }

    [Theory]
    [InlineData("../outside.cs")]
    [InlineData("../../some-file")]
    [InlineData("C:/outside/file.cs")]
    [InlineData("/etc/passwd")]
    [InlineData("DungeonEscape.Core/bin/generated.cs")]
    [InlineData("DungeonEscape.Core/obj/generated.cs")]
    public void UnsafePathsAreRejected(string path)
    {
        var error = Assert.Throws<ControlledWriteException>(() => service.Prepare(path, "new", Hash("old")));
        Assert.Equal("path-rejected", error.Code);
    }

    [Fact]
    public void UnsupportedBinaryOversizedAndStaleInputsAreRejected()
    {
        Write("DungeonEscape.Core/project.csproj", "<Project />");
        Assert.Equal("unsupported-extension", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/project.csproj", "<Project />", Hash("<Project />"))).Code);

        File.WriteAllBytes(Path.Combine(root, "DungeonEscape.Core", "Binary.cs"), [0x41, 0, 0x42]);
        Assert.Equal("binary-content", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/Binary.cs", "text", Hash([0x41, 0, 0x42]))).Code);

        Write("DungeonEscape.Core/Large.cs", new string('a', ControlledWriteService.MaximumFileBytes + 1));
        Assert.Equal("source-too-large", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/Large.cs", "text", new string('0', 64))).Code);

        Write("DungeonEscape.Core/Small.cs", "old");
        Assert.Equal("proposed-too-large", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/Small.cs",
                new string('a', ControlledWriteService.MaximumProposedBytes + 1), Hash("old"))).Code);
        Assert.Equal("stale-original", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/Small.cs", "new", new string('0', 64))).Code);
    }

    [Fact]
    public void InvalidUtf8AndLinkedTargetsAreRejected()
    {
        File.WriteAllBytes(Path.Combine(root, "DungeonEscape.Core", "Invalid.cs"), [0xC3, 0x28]);
        Assert.Equal("binary-content", Assert.Throws<ControlledWriteException>(() =>
            service.Prepare("DungeonEscape.Core/Invalid.cs", "new", Hash([0xC3, 0x28]))).Code);

        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
        File.WriteAllText(outside, "outside");
        var link = Path.Combine(root, "DungeonEscape.Core", "Linked.cs");
        try
        {
            File.CreateSymbolicLink(link, outside);
            Assert.Equal("path-rejected", Assert.Throws<ControlledWriteException>(() =>
                service.Prepare("DungeonEscape.Core/Linked.cs", "new", Hash("outside"))).Code);
        }
        finally { File.Delete(outside); }
    }

    [Fact]
    public void ApplyRequiresTrustedExactApprovalAndRejectsProposalTampering()
    {
        Write("DungeonEscape.Core/One.cs", "one");
        Write("DungeonEscape.Core/Two.cs", "two");
        var one = service.Prepare("DungeonEscape.Core/One.cs", "ONE", Hash("one"));
        var two = service.Prepare("DungeonEscape.Core/Two.cs", "TWO", Hash("two"));
        var approval = Issue(one);

        Assert.Equal("missing-approval", Error(() => Apply(one, "")).Code);
        Assert.Equal("invalid-approval", Error(() => Apply(one, "not-a-token")).Code);
        Assert.Equal("approval-mismatch", Error(() => Apply(two, approval.Token)).Code);
        Assert.Equal("proposal-mismatch", Error(() => service.Apply(one.ProposalId, two.Path,
            one.OriginalSha256, one.ProposedSha256, approval.Token)).Code);
        Assert.Equal("proposal-mismatch", Error(() => service.Apply(one.ProposalId, one.Path,
            one.OriginalSha256, two.ProposedSha256, approval.Token)).Code);
        Assert.Equal("proposal-mismatch", Error(() => service.Apply(one.ProposalId, one.Path,
            two.OriginalSha256, one.ProposedSha256, approval.Token)).Code);
    }

    [Fact]
    public void ExpiredApprovalAndChangedFileAreRejected()
    {
        var file = Write("DungeonEscape.Core/Example.cs", "old");
        var proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        var approval = Issue(proposal);
        clock.UtcNow = clock.UtcNow.AddMinutes(11);
        Assert.Equal("expired-approval", Error(() => Apply(proposal, approval.Token)).Code);

        clock.UtcNow = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        approval = Issue(proposal);
        File.WriteAllText(file, "changed");
        Assert.Equal("stale-original", Error(() => Apply(proposal, approval.Token)).Code);
    }

    [Fact]
    public void SuccessfulApprovedWriteMatchesHashAndConsumesApproval()
    {
        var file = Write("DungeonEscape.Core/Example.cs", "old");
        var proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        var approval = Issue(proposal);

        var result = Apply(proposal, approval.Token);

        Assert.Equal("applied", result.Status);
        Assert.Equal(proposal.ProposedSha256, result.ResultingSha256);
        Assert.Equal(approval.ApprovalId, result.ApprovalId);
        Assert.Equal("new", File.ReadAllText(file));
        Assert.Equal("proposal-missing", Error(() => Apply(proposal, approval.Token)).Code);
    }

    [Fact]
    public void ApplyFailsClosedWhenApprovalVerificationIsNotConfigured()
    {
        Write("DungeonEscape.Core/Example.cs", "old");
        var unavailable = CreateService(null);
        var proposal = unavailable.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));

        var error = Assert.Throws<ControlledWriteException>(() => unavailable.Apply(proposal.ProposalId,
            proposal.Path, proposal.OriginalSha256, proposal.ProposedSha256, "unavailable"));

        Assert.Equal("approval-unavailable", error.Code);
    }

    [Fact]
    public async Task ConcurrentApprovalReplayCannotDoubleWrite()
    {
        Write("DungeonEscape.Core/Example.cs", "old");
        var proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        var approval = Issue(proposal);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try { Apply(proposal, approval.Token); return true; }
            catch (ControlledWriteException) { return false; }
        })));

        Assert.Single(results, succeeded => succeeded);
        Assert.Equal("new", File.ReadAllText(Path.Combine(root, "DungeonEscape.Core", "Example.cs")));
    }

    [Fact]
    public void ReplayIsRejectedEvenWhenSameProposalIsPreparedAgain()
    {
        Write("DungeonEscape.Core/Example.cs", "old");
        var proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        var approval = Issue(proposal);
        Apply(proposal, approval.Token);
        Write("DungeonEscape.Core/Example.cs", "old");
        proposal = service.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));

        Assert.Equal("reused-approval", Error(() => Apply(proposal, approval.Token)).Code);
    }

    [Fact]
    public void FailedAtomicReplacementConsumesApprovalAndCleansTemporaryFile()
    {
        Write("DungeonEscape.Core/Example.cs", "old");
        var key = WriteApprovalKey.Generate();
        var localIssuer = new HmacWriteApprovalIssuer(key, clock);
        var replay = new ApprovalReplayRegistry(clock);
        var failing = new ControlledWriteService(new ProjectInfoService(root, root),
            new HmacWriteApprovalVerifier(key, clock), clock, replay, new FailingReplacement());
        var proposal = failing.Prepare("DungeonEscape.Core/Example.cs", "new", Hash("old"));
        var approval = localIssuer.Issue(Scope(proposal));

        Assert.Equal("write-failed", Assert.Throws<ControlledWriteException>(() => failing.Apply(
            proposal.ProposalId, proposal.Path, proposal.OriginalSha256, proposal.ProposedSha256,
            approval.Token)).Code);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, "DungeonEscape.Core"),
            ".dungeonescape-write-*"));
        Assert.Equal("old", File.ReadAllText(Path.Combine(root, "DungeonEscape.Core", "Example.cs")));
        Assert.Equal("reused-approval", Assert.Throws<ControlledWriteException>(() => failing.Apply(
            proposal.ProposalId, proposal.Path, proposal.OriginalSha256, proposal.ProposedSha256,
            approval.Token)).Code);
    }

    private ApplyWriteResult Apply(PrepareWriteResult proposal, string token) => service.Apply(
        proposal.ProposalId, proposal.Path, proposal.OriginalSha256, proposal.ProposedSha256, token);
    private SignedWriteApproval Issue(PrepareWriteResult proposal) => issuer.Issue(Scope(proposal));
    private static WriteApprovalScope Scope(PrepareWriteResult proposal) => new(proposal.ProposalId,
        proposal.Path, proposal.OriginalSha256, proposal.ProposedSha256, proposal.ProposedLength);
    private ControlledWriteService CreateService(HmacWriteApprovalVerifier? verifier) => new(
        new ProjectInfoService(root, root), verifier, clock, new ApprovalReplayRegistry(clock),
        new AtomicFileReplacement());
    private static ControlledWriteException Error(Action action) => Assert.Throws<ControlledWriteException>(action);
    private string Write(string relative, string content)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }
    private static string Hash(string content) => Hash(Encoding.UTF8.GetBytes(content));
    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    public void Dispose()
    {
        if (!Directory.Exists(root)) return;
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        Directory.Delete(root, true);
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
    private sealed class FailingReplacement : IFileReplacement
    {
        public void Replace(string temporaryPath, string targetPath) => throw new IOException("fixture");
    }
}
