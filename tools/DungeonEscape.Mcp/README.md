# DungeonEscape MCP server

Standalone, read-only repository metadata server for AI-002 / GitHub issue #8.
Targets .NET 10 and uses the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk)
(`ModelContextProtocol` 2.2.0). It has no Unity or gameplay project references.

## Structure

- `Program.cs`: minimal host, stderr logging, stdio transport, explicit tool registration.
- `Tools/ProjectInfoTools.cs`: MCP presentation and read-only annotations.
- `Services/ProjectInfoService.cs`: repository discovery and bounded metadata reads, independent of MCP.
- `../DungeonEscape.Mcp.Tests/`: xUnit discovery, metadata, boundary and real stdio tests.

These projects intentionally remain outside the gameplay solution. Build and test them directly;
no Unity editor, Unity assemblies or changes to game build scripts are needed.

## Build and test

From the repository root, with the .NET 10 SDK installed:

```powershell
dotnet build tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj
dotnet test tools/DungeonEscape.Mcp.Tests/DungeonEscape.Mcp.Tests.csproj
```

The test run includes a real subprocess handshake (`initialize`, `notifications/initialized`),
`tools/list` verification of exactly one parameterless read-only tool, `tools/call`, and unknown-tool rejection.
It runs the server from an unrelated working directory to verify discovery is independent of cwd.
Other tests use isolated temporary fixtures and do not modify the Unity project.
The Windows boundary test creates and removes a temporary junction; other platforms use a symlink.

## Run

Build first, then launch the DLL from its build output inside this checkout:

```powershell
dotnet tools/DungeonEscape.Mcp/bin/Debug/net10.0/DungeonEscape.Mcp.dll
```

For an MCP client, configure the executable as `dotnet` and pass the **absolute path** to that DLL
as its only argument. No API key or HTTP endpoint is required. The process waits for MCP messages;
it is not an interactive command prompt. Standard input/output carry newline-delimited JSON-RPC;
host logs go to stderr. Avoid launching through `dotnet run` in a client because build output can
interfere with protocol traffic. Close stdin to shut down the server.

## Available tool

`get_project_info` takes no parameters and returns:

- `projectName`: `DungeonEscape` when the checkout is recognized.
- `unityVersion`: `m_EditorVersion` from `DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt`.
- `repositoryRoot`: absolute recognized checkout path.
- `currentBranch`: a simple symbolic branch from `.git/HEAD`.
- `defaultBranch`: a simple symbolic branch from `.git/refs/remotes/origin/HEAD`.

Unavailable values are the explicit string `not available`. No default branch is inferred from
the current branch or from conventional names such as `main`. Missing repository identity makes
all fields unavailable. Serialization may use the record's PascalCase property names in text content.

## Security boundaries and limitations

- Discovery starts at the executable's directory, finds its `tools/DungeonEscape.Mcp` ancestor,
  and verifies adjacent `DungeonEscape.sln`, `DungeonEscape.Core`, and `DungeonEscape.Unity` markers.
  It stops at that candidate and never falls back to other repositories. Caller arguments,
  environment configuration, and working directory do not select the repository.
- Only the three fixed metadata files above are read, each bounded to 4,096 characters.
  Callers cannot supply paths, commands, URLs or write requests. No shell, Git subprocess,
  network access, file-reading tool, resources, prompts or HTTP transport is registered.
- Symlinks/junctions/reparse points in discovery and metadata paths are rejected. Detached or
  malformed HEAD, `.git` files (including worktrees/submodules), shared Git directories,
  packed remote HEAD metadata and complex branch names are unavailable. Git is not invoked,
  and remote references are never fetched. A local origin HEAD may be stale.
- The checkout and local process environment are trusted. Marker files establish layout, not
  cryptographic repository identity. This is not an OS sandbox against a malicious local user
  changing files or links concurrently between checks and reads.
- Moving/publishing the executable outside `tools/DungeonEscape.Mcp` makes discovery unavailable.
  Linked checkout paths are intentionally unsupported. Repository root exposes a local absolute
  path to the client as required by the issue.
- NuGet restore needs network access during development; tool execution does not.

Manual verification remaining: configure this DLL in the intended MCP client and confirm its
tool UI displays `get_project_info` and returned metadata. The protocol itself is covered by the automated test.
