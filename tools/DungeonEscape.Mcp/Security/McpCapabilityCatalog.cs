namespace DungeonEscape.Mcp.Security;

public enum McpSideEffectClassification
{
    None,
    LocalTestArtifacts,
    ApprovedRepositoryWrite
}

public sealed record McpCapabilityDefinition(string Name, bool ReadOnly, bool Destructive,
    bool Idempotent, bool OpenWorld, McpSideEffectClassification SideEffects, string Description);

public static class McpCapabilityNames
{
    public const string GetProjectInfo = "get_project_info";
    public const string SearchCode = "search_code";
    public const string ReadScript = "read_script";
    public const string ListScenes = "list_scenes";
    public const string GetUnityConsole = "get_unity_console";
    public const string GetGitDiff = "get_git_diff";
    public const string RunTests = "run_tests";
    public const string PrepareWrite = "prepare_write";
    public const string ApplyWrite = "apply_write";
}

/// <summary>Authoritative fail-closed inventory of the MCP server's nine capabilities.</summary>
public static class McpCapabilityCatalog
{
    private static readonly McpCapabilityDefinition[] Definitions =
    [
        ReadOnly(McpCapabilityNames.GetProjectInfo, "Bounded local repository metadata."),
        ReadOnly(McpCapabilityNames.SearchCode, "Bounded approved-root literal source search."),
        ReadOnly(McpCapabilityNames.ReadScript, "Bounded approved source/document line-range read."),
        ReadOnly(McpCapabilityNames.ListScenes, "Bounded Unity scene and build-settings discovery."),
        ReadOnly(McpCapabilityNames.GetUnityConsole, "Bounded redacted project-scoped Unity diagnostics."),
        ReadOnly(McpCapabilityNames.GetGitDiff, "Bounded local working-tree and index inspection."),
        new(McpCapabilityNames.RunTests, false, false, false, false,
            McpSideEffectClassification.LocalTestArtifacts,
            "Runs one fixed test suite and may regenerate local build/test artifacts."),
        ReadOnly(McpCapabilityNames.PrepareWrite,
            "Previews one exact bounded replacement without modifying the repository."),
        new(McpCapabilityNames.ApplyWrite, false, true, false, false,
            McpSideEffectClassification.ApprovedRepositoryWrite,
            "Applies one exact prepared replacement with trusted single-use approval.")
    ];

    private static readonly IReadOnlyDictionary<string, McpCapabilityDefinition> ByName =
        CreateValidatedIndex(Definitions);

    public static IReadOnlyList<McpCapabilityDefinition> All { get; } =
        Array.AsReadOnly(Definitions);

    public static bool TryGet(string name, out McpCapabilityDefinition definition) =>
        ByName.TryGetValue(name, out definition!);

    public static McpCapabilityDefinition GetRequired(string name) =>
        TryGet(name, out var definition) ? definition
            : throw new KeyNotFoundException($"Unknown MCP capability: {name}");

    internal static IReadOnlyDictionary<string, McpCapabilityDefinition> CreateValidatedIndex(
        IEnumerable<McpCapabilityDefinition> definitions)
    {
        var index = new Dictionary<string, McpCapabilityDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Name)
                || string.IsNullOrWhiteSpace(definition.Description))
                throw new InvalidOperationException("MCP capability definitions require a name and description.");
            if (!index.TryAdd(definition.Name, definition))
                throw new InvalidOperationException($"Duplicate MCP capability: {definition.Name}");
        }
        return index;
    }

    private static McpCapabilityDefinition ReadOnly(string name, string description) =>
        new(name, true, false, true, false, McpSideEffectClassification.None, description);
}
