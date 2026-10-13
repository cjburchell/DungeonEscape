using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DungeonEscape.WriteApproval;

public sealed record WriteApprovalScope(string ProposalId, string Path, string OriginalSha256,
    string ProposedSha256, int ProposedLength);

public static class WriteProposalIdentity
{
    public static string Create(string path, string originalSha256, string proposedSha256,
        int proposedLength)
    {
        if (proposedLength < 0) throw new ArgumentOutOfRangeException(nameof(proposedLength));
        var identity = $"DungeonEscape-write-v1\n{path}\n{originalSha256}\n{proposedSha256}\n{proposedLength}";
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))
            .ToLowerInvariant();
    }
}

public sealed record SignedWriteApproval(string ApprovalId, string Token, DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record VerifiedWriteApproval(string ApprovalId, DateTimeOffset ExpiresAtUtc);

public sealed record WriteApprovalVerification(bool Succeeded, string ErrorCode,
    VerifiedWriteApproval? Approval)
{
    internal static WriteApprovalVerification Success(VerifiedWriteApproval approval) =>
        new(true, "", approval);
    internal static WriteApprovalVerification Failure(string code) => new(false, code, null);
}

public interface IWriteApprovalIssuer
{
    SignedWriteApproval Issue(WriteApprovalScope scope);
}

public sealed class WriteApprovalKey
{
    public const string EnvironmentVariableName = "DUNGEONESCAPE_WRITE_APPROVAL_KEY";
    private const int KeyBytes = 32;
    private readonly byte[] bytes;

    private WriteApprovalKey(byte[] bytes) => this.bytes = bytes;

    public static WriteApprovalKey Generate() => new(RandomNumberGenerator.GetBytes(KeyBytes));

    public static bool TryParse(string? encoded, out WriteApprovalKey? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > 128) return false;
        try
        {
            var bytes = Base64Url.Decode(encoded);
            if (bytes.Length != KeyBytes) return false;
            key = new(bytes);
            return true;
        }
        catch (FormatException) { return false; }
    }

    public string EncodeForChildEnvironment() => Base64Url.Encode(bytes);
    internal byte[] CopyBytes() => bytes.ToArray();
}

public sealed class HmacWriteApprovalIssuer : IWriteApprovalIssuer
{
    private readonly byte[] key;
    private readonly TimeProvider timeProvider;

    public HmacWriteApprovalIssuer(WriteApprovalKey key, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        this.key = key.CopyBytes();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SignedWriteApproval Issue(WriteApprovalScope scope)
    {
        ApprovalTokenCodec.ValidateScope(scope);
        var issued = timeProvider.GetUtcNow();
        var expires = issued.AddMinutes(10);
        var approvalId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var payload = ApprovalTokenCodec.Serialize(approvalId, scope, issued, expires);
        var signature = HMACSHA256.HashData(key, payload);
        return new(approvalId, Base64Url.Encode(payload) + "." + Base64Url.Encode(signature),
            issued, expires);
    }
}

public sealed class HmacWriteApprovalVerifier
{
    private readonly byte[] key;
    private readonly TimeProvider timeProvider;

    public HmacWriteApprovalVerifier(WriteApprovalKey key, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        this.key = key.CopyBytes();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public WriteApprovalVerification Verify(string? token, WriteApprovalScope expected)
    {
        if (string.IsNullOrWhiteSpace(token)) return WriteApprovalVerification.Failure("missing-approval");
        if (token.Length > ApprovalTokenCodec.MaximumTokenCharacters)
            return WriteApprovalVerification.Failure("invalid-approval");
        var parts = token.Split('.');
        if (parts.Length != 2) return WriteApprovalVerification.Failure("invalid-approval");
        byte[] payload;
        byte[] signature;
        try
        {
            payload = Base64Url.Decode(parts[0]);
            signature = Base64Url.Decode(parts[1]);
        }
        catch (FormatException) { return WriteApprovalVerification.Failure("invalid-approval"); }
        if (payload.Length > ApprovalTokenCodec.MaximumPayloadBytes || signature.Length != 32)
            return WriteApprovalVerification.Failure("invalid-approval");
        var expectedSignature = HMACSHA256.HashData(key, payload);
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
            return WriteApprovalVerification.Failure("invalid-approval");
        if (!ApprovalTokenCodec.TryParse(payload, out var parsed))
            return WriteApprovalVerification.Failure("invalid-approval");
        try { ApprovalTokenCodec.ValidateScope(expected); }
        catch (ArgumentException) { return WriteApprovalVerification.Failure("approval-mismatch"); }
        if (parsed!.Scope != expected)
            return WriteApprovalVerification.Failure("approval-mismatch");
        var now = timeProvider.GetUtcNow();
        if (parsed.ExpiresAtUtc <= now) return WriteApprovalVerification.Failure("expired-approval");
        if (parsed.IssuedAtUtc > now.AddMinutes(1)
            || parsed.ExpiresAtUtc <= parsed.IssuedAtUtc
            || parsed.ExpiresAtUtc - parsed.IssuedAtUtc > TimeSpan.FromMinutes(10)
            || parsed.ExpiresAtUtc > now.AddMinutes(11))
            return WriteApprovalVerification.Failure("invalid-approval");
        return WriteApprovalVerification.Success(new(parsed.ApprovalId, parsed.ExpiresAtUtc));
    }
}

internal static class ApprovalTokenCodec
{
    internal const int MaximumTokenCharacters = 4096;
    internal const int MaximumPayloadBytes = 2048;
    private const string Version = "DungeonEscape-write-approval-v1";
    internal sealed record Parsed(string ApprovalId, WriteApprovalScope Scope,
        DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc);

    internal static byte[] Serialize(string approvalId, WriteApprovalScope scope,
        DateTimeOffset issued, DateTimeOffset expires)
    {
        ValidateApprovalId(approvalId);
        ValidateScope(scope);
        var canonical = string.Join('\n', Version, approvalId, scope.ProposalId, scope.Path,
            scope.OriginalSha256, scope.ProposedSha256,
            scope.ProposedLength.ToString(CultureInfo.InvariantCulture),
            issued.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            expires.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var bytes = Encoding.UTF8.GetBytes(canonical);
        if (bytes.Length > MaximumPayloadBytes) throw new ArgumentException("Approval payload is too large.");
        return bytes;
    }

    internal static bool TryParse(byte[] payload, out Parsed? parsed)
    {
        parsed = null;
        string text;
        try { text = new UTF8Encoding(false, true).GetString(payload); }
        catch (DecoderFallbackException) { return false; }
        var fields = text.Split('\n');
        if (fields.Length != 9 || fields[0] != Version || fields.Skip(1).Any(string.IsNullOrEmpty)) return false;
        if (!int.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out var proposedLength)
            || !long.TryParse(fields[7], NumberStyles.None, CultureInfo.InvariantCulture, out var issued)
            || !long.TryParse(fields[8], NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
            return false;
        try
        {
            var scope = new WriteApprovalScope(fields[2], fields[3], fields[4], fields[5], proposedLength);
            ValidateApprovalId(fields[1]);
            ValidateScope(scope);
            parsed = new(fields[1], scope, DateTimeOffset.FromUnixTimeSeconds(issued),
                DateTimeOffset.FromUnixTimeSeconds(expires));
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    internal static void ValidateScope(WriteApprovalScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ValidateField(scope.ProposalId, nameof(scope.ProposalId));
        ValidateField(scope.Path, nameof(scope.Path));
        ValidateHash(scope.OriginalSha256, nameof(scope.OriginalSha256));
        ValidateHash(scope.ProposedSha256, nameof(scope.ProposedSha256));
        if (scope.ProposedLength < 0 || scope.ProposalId != WriteProposalIdentity.Create(scope.Path,
                scope.OriginalSha256, scope.ProposedSha256, scope.ProposedLength))
            throw new ArgumentException("Approval proposal identity is inconsistent.", nameof(scope));
    }

    private static void ValidateField(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.Any(char.IsControl))
            throw new ArgumentException("Approval fields must be bounded non-control text.", name);
    }

    private static void ValidateHash(string value, string name)
    {
        if (value is not { Length: 64 } || !value.All(char.IsAsciiHexDigit))
            throw new ArgumentException("Approval hashes must be SHA-256 hex.", name);
    }

    private static void ValidateApprovalId(string value)
    {
        if (value is not { Length: 32 } || !value.All(char.IsAsciiHexDigit))
            throw new ArgumentException("Approval IDs must be 128-bit hexadecimal nonces.", nameof(value));
    }

}

internal static class Base64Url
{
    internal static string Encode(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal static byte[] Decode(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Any(character => !(char.IsAsciiLetterOrDigit(character)
                || character is '-' or '_')))
            throw new FormatException("Invalid base64url.");
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
            { 0 => "", 2 => "==", 3 => "=", _ => throw new FormatException() };
        return Convert.FromBase64String(padded);
    }
}
