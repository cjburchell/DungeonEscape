using System.Security.Cryptography;
using System.Text;
using DungeonEscape.WriteApproval;

namespace DungeonEscape.Mcp.Services;

public sealed record PrepareWriteResult(string Status, string ProposalId, string Path,
    string OriginalSha256, string ProposedSha256, int OriginalLength, int ProposedLength,
    bool ContentChanged, string Preview, bool PreviewTruncated, IReadOnlyList<string> Warnings);

public sealed record ApplyWriteResult(string Status, string Path, string PreviousSha256,
    string ResultingSha256, int CharactersWritten, string ProposalId, string ApprovalId,
    IReadOnlyList<string> Warnings);

public sealed class ControlledWriteException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}

public sealed record WriteApprovalVerifierConfiguration(HmacWriteApprovalVerifier? Verifier);

internal interface IFileReplacement { void Replace(string temporaryPath, string targetPath); }
internal sealed class AtomicFileReplacement : IFileReplacement
{
    public void Replace(string temporaryPath, string targetPath) => File.Move(temporaryPath, targetPath, true);
}

public sealed class ApprovalReplayRegistry(TimeProvider timeProvider)
{
    private const int MaximumConsumedApprovals = 1000;
    private readonly Dictionary<string, DateTimeOffset> consumed = new(StringComparer.Ordinal);
    private readonly Lock gate = new();

    public void Claim(string approvalId, DateTimeOffset expiresAtUtc)
    {
        lock (gate)
        {
            foreach (var expired in consumed.Where(item => item.Value <= timeProvider.GetUtcNow())
                         .Select(item => item.Key).ToArray()) consumed.Remove(expired);
            if (consumed.ContainsKey(approvalId))
                throw new ControlledWriteException("reused-approval", "The approval token was already consumed.");
            if (consumed.Count >= MaximumConsumedApprovals)
                throw new ControlledWriteException("approval-capacity", "Approval replay capacity is exhausted.");
            // Claiming permanently consumes the nonce before mutation. This prevents concurrent reuse and
            // deliberately requires fresh human approval after any failure during the mutation phase.
            consumed.Add(approvalId, expiresAtUtc);
        }
    }
}

public sealed class ControlledWriteService
{
    public const int MaximumFileBytes = 1024 * 1024;
    public const int MaximumProposedBytes = 1024 * 1024;
    public const int MaximumPreviewCharacters = 128 * 1024;
    private const int MaximumProposals = 100;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HashSet<string> WriteExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".cs", ".json", ".md", ".txt", ".xml", ".yml", ".yaml" };

    private sealed record StoredProposal(PrepareWriteResult Public, string ProposedContent,
        DateTimeOffset ExpiresAtUtc);
    private readonly Dictionary<string, StoredProposal> proposals = new(StringComparer.Ordinal);
    private readonly Lock gate = new();
    private readonly ProjectInfoService projectInfo;
    private readonly HmacWriteApprovalVerifier? approvalVerifier;
    private readonly ApprovalReplayRegistry replayRegistry;
    private readonly TimeProvider timeProvider;
    private readonly IFileReplacement replacement;

    public ControlledWriteService(ProjectInfoService projectInfo, WriteApprovalVerifierConfiguration configuration,
        TimeProvider timeProvider, ApprovalReplayRegistry replayRegistry)
        : this(projectInfo, configuration.Verifier, timeProvider, replayRegistry, new AtomicFileReplacement()) { }

    internal ControlledWriteService(ProjectInfoService projectInfo,
        HmacWriteApprovalVerifier? approvalVerifier, TimeProvider timeProvider,
        ApprovalReplayRegistry replayRegistry, IFileReplacement replacement)
    {
        this.projectInfo = projectInfo;
        this.approvalVerifier = approvalVerifier;
        this.timeProvider = timeProvider;
        this.replayRegistry = replayRegistry;
        this.replacement = replacement;
    }

    public PrepareWriteResult Prepare(string path, string proposedText, string expectedCurrentSha256)
    {
        if (proposedText is null) throw new ArgumentNullException(nameof(proposedText));
        if (proposedText.Contains('\0'))
            throw new ControlledWriteException("binary-content", "Proposed text must not contain NUL characters.");
        var proposedBytes = StrictUtf8.GetBytes(proposedText);
        if (proposedBytes.Length > MaximumProposedBytes)
            throw new ControlledWriteException("proposed-too-large", "Proposed text exceeds 1 MiB.");

        var target = ResolveTarget(path);
        var originalBytes = ReadBounded(target.FullPath);
        var originalText = Decode(originalBytes);
        var originalHash = Hash(originalBytes);
        if (!IsSha256(expectedCurrentSha256) || !originalHash.Equals(expectedCurrentSha256,
                StringComparison.OrdinalIgnoreCase))
            throw new ControlledWriteException("stale-original", "Expected current SHA-256 does not match the file.");

        var proposedHash = Hash(proposedBytes);
        var proposalId = WriteProposalIdentity.Create(target.RelativePath, originalHash, proposedHash,
            proposedBytes.Length);
        var preview = BuildPreview(target.RelativePath, originalText, proposedText);
        var warnings = preview.Truncated ? new[] { "Preview is truncated; approval still binds to the full content." }
            : Array.Empty<string>();
        var result = new PrepareWriteResult("prepared", proposalId, target.RelativePath, originalHash,
            proposedHash, originalBytes.Length, proposedBytes.Length, originalHash != proposedHash,
            preview.Text, preview.Truncated, warnings);
        lock (gate)
        {
            RemoveExpiredProposals();
            if (proposals.TryGetValue(proposalId, out var existing)) return existing.Public;
            if (!proposals.ContainsKey(proposalId) && proposals.Count >= MaximumProposals)
            {
                var oldest = proposals.MinBy(item => item.Value.ExpiresAtUtc).Key;
                proposals.Remove(oldest);
            }
            // Proposals outlive their 10-minute approvals so expiry is reported distinctly and a
            // human can request a new approval without rebuilding the preview immediately.
            proposals[proposalId] = new(result, proposedText, timeProvider.GetUtcNow().AddMinutes(30));
        }
        return result;
    }

    public ApplyWriteResult Apply(string proposalId, string path, string originalSha256,
        string proposedSha256, string approvalToken)
    {
        if (string.IsNullOrWhiteSpace(proposalId))
            throw new ControlledWriteException("proposal-missing", "A prepared proposal ID is required.");
        StoredProposal stored;
        lock (gate)
        {
            RemoveExpiredProposals();
            if (!proposals.TryGetValue(proposalId, out stored!))
                throw new ControlledWriteException("proposal-missing", "The prepared proposal is unavailable or expired.");
        }
        if (stored.Public.Path != path || stored.Public.OriginalSha256 != originalSha256
            || stored.Public.ProposedSha256 != proposedSha256)
            throw new ControlledWriteException("proposal-mismatch", "Apply arguments do not match the prepared proposal.");

        var target = ResolveTarget(path);
        var currentBytes = ReadBounded(target.FullPath);
        var currentHash = Hash(currentBytes);
        if (currentHash != stored.Public.OriginalSha256)
            throw new ControlledWriteException("stale-original", "The file changed after preview.");
        var proposedBytes = StrictUtf8.GetBytes(stored.ProposedContent);
        if (Hash(proposedBytes) != stored.Public.ProposedSha256)
            throw new ControlledWriteException("proposal-mismatch", "Stored proposed content failed verification.");

        if (approvalVerifier is null)
            throw new ControlledWriteException("approval-unavailable",
                "Trusted write approval verification is not configured.");
        var scope = new WriteApprovalScope(stored.Public.ProposalId, stored.Public.Path,
            stored.Public.OriginalSha256, stored.Public.ProposedSha256, stored.Public.ProposedLength);
        var verification = approvalVerifier.Verify(approvalToken, scope);
        if (!verification.Succeeded)
            throw new ControlledWriteException(verification.ErrorCode, verification.ErrorCode switch
            {
                "missing-approval" => "A trusted approval token is required.",
                "expired-approval" => "The approval token has expired.",
                "approval-mismatch" => "The approval token does not authorize this exact proposal.",
                _ => "The approval token is invalid."
            });
        var approval = verification.Approval!;
        replayRegistry.Claim(approval.ApprovalId, approval.ExpiresAtUtc);
        if (!stored.Public.ContentChanged)
        {
            lock (gate) proposals.Remove(proposalId);
            return new("unchanged", target.RelativePath, currentHash, currentHash, 0,
                proposalId, approval.ApprovalId, []);
        }

        var temporaryPath = Path.Combine(Path.GetDirectoryName(target.FullPath)!,
            ".dungeonescape-write-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant());
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, FileOptions.WriteThrough))
            {
                stream.Write(proposedBytes);
                stream.Flush(true);
            }
            if (Hash(ReadBounded(target.FullPath)) != stored.Public.OriginalSha256)
                throw new ControlledWriteException("stale-original", "The file changed during execution.");
            replacement.Replace(temporaryPath, target.FullPath);
            var resultingHash = Hash(ReadBounded(target.FullPath));
            if (resultingHash != stored.Public.ProposedSha256)
                throw new ControlledWriteException("verification-failed", "Written content failed verification.");
            lock (gate) proposals.Remove(proposalId);
            return new("applied", target.RelativePath, currentHash, resultingHash,
                stored.ProposedContent.Length, proposalId, approval.ApprovalId, []);
        }
        catch (ControlledWriteException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ControlledWriteException("write-failed", "The atomic repository write failed.");
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }
    }

    private (string FullPath, string RelativePath) ResolveTarget(string path)
    {
        var root = projectInfo.DiscoverRepositoryRoot();
        if (root is null) throw new ControlledWriteException("repository-unavailable", "Repository is unavailable.");
        string normalized;
        try { normalized = RepositoryContentPolicy.NormalizeRequestedPath(path, true)!; }
        catch (ArgumentException exception) { throw new ControlledWriteException("path-rejected", exception.Message); }
        string fullPath;
        try { fullPath = RepositoryContentPolicy.ResolveApprovedFile(root, normalized); }
        catch (ArgumentException exception) { throw new ControlledWriteException("path-rejected", exception.Message); }
        if (!WriteExtensions.Contains(Path.GetExtension(fullPath)))
            throw new ControlledWriteException("unsupported-extension", "Path extension is not approved for writes.");
        if (!File.Exists(fullPath))
            throw new ControlledWriteException("file-missing", "Only existing files can be replaced.");
        return (fullPath, RepositoryContentPolicy.Relative(root, fullPath));
    }

    private static byte[] ReadBounded(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaximumFileBytes)
            throw new ControlledWriteException("source-too-large", "Existing file exceeds 1 MiB.");
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > MaximumFileBytes)
                throw new ControlledWriteException("source-too-large", "Existing file exceeds 1 MiB.");
            _ = Decode(bytes);
            return bytes;
        }
        catch (DecoderFallbackException)
        {
            throw new ControlledWriteException("binary-content", "Existing file is not valid UTF-8 text.");
        }
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.AsSpan().IndexOf((byte)0) >= 0)
            throw new ControlledWriteException("binary-content", "NUL/binary content is not supported.");
        return StrictUtf8.GetString(bytes);
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static bool IsSha256(string value) => value is { Length: 64 }
        && value.All(character => char.IsAsciiHexDigit(character));

    private static (string Text, bool Truncated) BuildPreview(string path, string original, string proposed)
    {
        var oldLines = original.Replace("\r\n", "\n").Split('\n');
        var newLines = proposed.Replace("\r\n", "\n").Split('\n');
        var prefix = 0;
        while (prefix < oldLines.Length && prefix < newLines.Length && oldLines[prefix] == newLines[prefix]) prefix++;
        var oldSuffix = oldLines.Length;
        var newSuffix = newLines.Length;
        while (oldSuffix > prefix && newSuffix > prefix
            && oldLines[oldSuffix - 1] == newLines[newSuffix - 1]) { oldSuffix--; newSuffix--; }
        var builder = new StringBuilder();
        var truncated = false;
        Append($"--- a/{path}\n+++ b/{path}\n");
        Append($"@@ -{prefix + 1},{oldSuffix - prefix} +{prefix + 1},{newSuffix - prefix} @@\n");
        for (var index = prefix; index < oldSuffix && !truncated; index++) Append("-" + oldLines[index] + "\n");
        for (var index = prefix; index < newSuffix && !truncated; index++) Append("+" + newLines[index] + "\n");
        return (builder.ToString(), truncated);

        void Append(string value)
        {
            var remaining = MaximumPreviewCharacters - builder.Length;
            if (remaining <= 0) { truncated = true; return; }
            if (value.Length <= remaining) { builder.Append(value); return; }
            var length = remaining;
            if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
            builder.Append(value, 0, length);
            truncated = true;
        }
    }

    private void RemoveExpiredProposals()
    {
        foreach (var id in proposals.Where(item => item.Value.ExpiresAtUtc <= timeProvider.GetUtcNow())
                     .Select(item => item.Key).ToArray()) proposals.Remove(id);
    }
}
