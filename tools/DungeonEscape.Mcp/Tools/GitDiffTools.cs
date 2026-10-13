using System.ComponentModel;
using DungeonEscape.Mcp.Services;
using DungeonEscape.Mcp.Security;
using ModelContextProtocol.Server;

namespace DungeonEscape.Mcp.Tools;

[McpServerToolType]
public sealed class GitDiffTools(GitDiffService service)
{
    [McpServerTool(Name = McpCapabilityNames.GetGitDiff, ReadOnly = true, Destructive = false,
        Idempotent = true, OpenWorld = false)]
    [Description("Returns a bounded structured view of local Git changes using fixed read-only commands.")]
    public Task<GitDiffResult> GetGitDiff(
        [Description("Include staged tracked changes; defaults to true.")] bool includeStaged = true,
        [Description("Include unstaged tracked changes; defaults to true.")] bool includeUnstaged = true,
        [Description("Include untracked path/status metadata without file content; defaults to true.")]
        bool includeUntrackedMetadata = true,
        [Description("Maximum changed files from 1 to 200; defaults to 200.")] int? maxFiles = null,
        [Description("Maximum total patch characters from 1 to 262144; defaults to 262144.")]
        int? maxPatchCharacters = null,
        CancellationToken cancellationToken = default) =>
        service.GetAsync(includeStaged, includeUnstaged, includeUntrackedMetadata, maxFiles,
            maxPatchCharacters, cancellationToken);
}
