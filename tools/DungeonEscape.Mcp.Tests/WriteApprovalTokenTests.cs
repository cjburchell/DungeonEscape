using DungeonEscape.WriteApproval;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class WriteApprovalTokenTests
{
    private static readonly string OriginalHash = new('b', 64);
    private static readonly string ProposedHash = new('c', 64);
    private const string Path = "DungeonEscape.Core/Example.cs";
    private static readonly WriteApprovalScope Scope = new(
        WriteProposalIdentity.Create(Path, OriginalHash, ProposedHash, 3),
        Path, OriginalHash, ProposedHash, 3);

    [Fact]
    public void ValidSignedApprovalIsAccepted()
    {
        var clock = new MutableTimeProvider();
        var key = WriteApprovalKey.Generate();
        var approval = new HmacWriteApprovalIssuer(key, clock).Issue(Scope);

        var result = new HmacWriteApprovalVerifier(key, clock).Verify(approval.Token, Scope);

        Assert.True(result.Succeeded);
        Assert.Equal(approval.ApprovalId, result.Approval?.ApprovalId);
    }

    [Fact]
    public void OtherKeyModifiedPayloadAndModifiedSignatureAreRejected()
    {
        var clock = new MutableTimeProvider();
        var key = WriteApprovalKey.Generate();
        var approval = new HmacWriteApprovalIssuer(key, clock).Issue(Scope);
        var verifier = new HmacWriteApprovalVerifier(key, clock);

        Assert.Equal("invalid-approval", new HmacWriteApprovalVerifier(WriteApprovalKey.Generate(), clock)
            .Verify(approval.Token, Scope).ErrorCode);
        var parts = approval.Token.Split('.');
        Assert.Equal("invalid-approval", verifier.Verify(Flip(parts[0]) + "." + parts[1], Scope).ErrorCode);
        Assert.Equal("invalid-approval", verifier.Verify(parts[0] + "." + Flip(parts[1]), Scope).ErrorCode);
    }

    [Fact]
    public void ExactProposalPathAndHashesMustMatch()
    {
        var clock = new MutableTimeProvider();
        var key = WriteApprovalKey.Generate();
        var token = new HmacWriteApprovalIssuer(key, clock).Issue(Scope).Token;
        var verifier = new HmacWriteApprovalVerifier(key, clock);

        Assert.Equal("approval-mismatch", verifier.Verify(token,
            Scope with { ProposalId = "sha256:" + new string('d', 64) }).ErrorCode);
        Assert.Equal("approval-mismatch", verifier.Verify(token,
            Scope with { Path = "DungeonEscape.Core/Other.cs" }).ErrorCode);
        Assert.Equal("approval-mismatch", verifier.Verify(token,
            Scope with { OriginalSha256 = new string('d', 64) }).ErrorCode);
        Assert.Equal("approval-mismatch", verifier.Verify(token,
            Scope with { ProposedSha256 = new string('d', 64) }).ErrorCode);
    }

    [Fact]
    public void ExpiredAndFutureApprovalsAreRejected()
    {
        var issuerClock = new MutableTimeProvider();
        var key = WriteApprovalKey.Generate();
        var approval = new HmacWriteApprovalIssuer(key, issuerClock).Issue(Scope);
        var verifierClock = new MutableTimeProvider { UtcNow = issuerClock.UtcNow.AddMinutes(11) };
        Assert.Equal("expired-approval",
            new HmacWriteApprovalVerifier(key, verifierClock).Verify(approval.Token, Scope).ErrorCode);

        issuerClock.UtcNow = verifierClock.UtcNow.AddMinutes(2);
        var future = new HmacWriteApprovalIssuer(key, issuerClock).Issue(Scope);
        Assert.Equal("invalid-approval",
            new HmacWriteApprovalVerifier(key, verifierClock).Verify(future.Token, Scope).ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("a.b.c")]
    [InlineData("*.abc")]
    public void MalformedApprovalsFailClosed(string token)
    {
        var result = new HmacWriteApprovalVerifier(WriteApprovalKey.Generate()).Verify(token, Scope);
        Assert.Equal(string.IsNullOrEmpty(token) ? "missing-approval" : "invalid-approval", result.ErrorCode);
    }

    [Fact]
    public void OversizedApprovalFailsClosed()
    {
        var result = new HmacWriteApprovalVerifier(WriteApprovalKey.Generate())
            .Verify(new string('a', 4097), Scope);
        Assert.Equal("invalid-approval", result.ErrorCode);
    }

    private static string Flip(string encoded)
    {
        var chars = encoded.ToCharArray();
        chars[0] = chars[0] == 'A' ? 'B' : 'A';
        return new string(chars);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
