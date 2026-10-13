using System.ComponentModel;
using DungeonEscape.Mcp.Security;
using DungeonEscape.Mcp.Services;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class ControlledWriteTools(ControlledWriteService service)
{
    [McpServerTool(Name = McpCapabilityNames.PrepareWrite, ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Prepares a bounded exact replacement of one existing approved repository text file without writing it.")]
    public PrepareWriteResult PrepareWrite(
        [Description("Required repository-relative path to an existing approved text file.")] string path,
        [Description("Required complete UTF-8 replacement text, limited to 1 MiB.")] string proposedText,
        [Description("Required SHA-256 of the expected current file bytes.")] string expectedCurrentSha256) =>
        service.Prepare(path, proposedText, expectedCurrentSha256);

    [McpServerTool(Name = McpCapabilityNames.ApplyWrite, ReadOnly = false, Destructive = true,
        Idempotent = false, OpenWorld = false)]
    [Description("Applies one exact prepared replacement using a trusted, short-lived, single-use approval token.")]
    public ApplyWriteResult ApplyWrite(
        [Description("Required content-derived proposal identifier returned by prepare_write.")] string proposalId,
        [Description("Required canonical repository-relative path returned by prepare_write.")] string path,
        [Description("Required original SHA-256 returned by prepare_write.")] string originalSha256,
        [Description("Required proposed SHA-256 returned by prepare_write.")] string proposedSha256,
        [Description("Required opaque approval token issued outside MCP by the trusted human-approval authority.")]
        string approvalToken) =>
        service.Apply(proposalId, path, originalSha256, proposedSha256, approvalToken);
}
