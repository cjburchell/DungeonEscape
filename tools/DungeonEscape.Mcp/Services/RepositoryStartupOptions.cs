namespace DungeonEscape.Mcp.Services;

/// <summary>Arguments supplied by the trusted process launcher, never by an MCP caller.</summary>
public static class RepositoryStartupOptions
{
    public static string? ParseRepositoryRoot(string[] args)
    {
        if (args.Length == 0) return null;
        if (args.Length != 2 || args[0] != "--repository-root")
            throw new ArgumentException("Usage: DungeonEscape.Mcp [--repository-root <absolute path>].");
        if (string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("--repository-root requires an absolute path.");
        return args[1];
    }
}
