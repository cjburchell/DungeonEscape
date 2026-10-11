# DungeonEscape deterministic agent orchestrator

Standalone .NET 10 proof of the AI-010 Manager → Unity Engineer → Reviewer workflow. It is deliberately
small and remains outside `DungeonEscape.sln`, like the existing MCP tooling.

This implementation proves typed role hand-offs, application-enforced transitions, one bounded
`REQUEST_CHANGES` retry, role-based MCP admission, structured tracing, malformed-output handling, and
executor failure handling. `ScriptedAgentExecutor` runs deterministic caller-supplied functions; no
real LLM or autonomous reasoning backend is implemented.

## Safety boundary

- Manager has no tools.
- Unity Engineer and Reviewer may call `get_project_info` and bounded read-only `search_code`.
- The gateway also requires the MCP descriptor to be read-only, non-destructive, and closed-world.
- No arbitrary file reading, write-capable, shell, Git, QA, or RAG capability is provided.
- Trace events contain lifecycle metadata and summaries, not chain-of-thought, credentials,
  environment variables, or arbitrary source contents.

The existing MCP server is started as a separate stdio process and is not modified by this project.
The orchestrator passes specialist searches through without interpreting, indexing, caching, or
persisting results. The deterministic demonstration continues to use explicit static context.

## Build and test

```powershell
dotnet build tools/DungeonEscape.AgentOrchestrator/DungeonEscape.AgentOrchestrator.csproj
dotnet test tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj
```

The test project references the MCP project only as a build dependency, launches its existing DLL,
and verifies the real handshake/list/call path plus role and metadata enforcement.

## Deterministic demonstration

Build the existing MCP server, then run:

```powershell
dotnet build tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj
dotnet run --project tools/DungeonEscape.AgentOrchestrator/DungeonEscape.AgentOrchestrator.csproj -- --demo
```

The scripted example analyzes a locked-chest request from explicitly supplied architecture context,
calls `get_project_info`, receives Reviewer approval, prints trace transitions, and changes no files.

## Extension boundary

`IAgentExecutor` is the backend seam for a future LLM executor. Additional roles such as QA can add
typed transitions later, and retrieval can be added as another allowlisted read-only capability,
without moving orchestration into Unity or weakening the current MCP boundary.
