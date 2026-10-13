# DungeonEscape agent orchestrator

Standalone .NET 10 deterministic Manager -> Unity Engineer -> Reviewer workflow. It remains outside
`DungeonEscape.sln`, like the MCP tooling. AI-011 adds a production-capable specialist adapter for the
official OpenAI .NET SDK `2.14.0` Responses API; the Manager and workflow remain application code.

The implementation provides typed role hand-offs, application-enforced transitions, one bounded
`REQUEST_CHANGES` retry, role-based MCP admission, structured tracing, malformed-output handling, and
executor failure handling. `ScriptedAgentExecutor` remains deterministic test/demo infrastructure.
`LlmAgentExecutor` runs Engineer and Reviewer through a provider-neutral `ILlmClient`;
`OpenAiLlmClient` is the single production provider.

## Safety boundary

- Manager has no tools.
- Unity Engineer and Reviewer may call only `get_project_info`, `search_code`, `read_script`,
  `list_scenes`, `get_unity_console`, and `get_git_diff`.
- The gateway also requires every MCP descriptor to be read-only, non-destructive, and closed-world.
- `run_tests` remains denied to all three roles pending separate QA-agent work.
- Model tool requests always flow through `IAgentToolGateway`; the model never connects to MCP directly.
- Each specialist execution defaults to eight tool calls and 120 seconds, with hard maxima of 20 calls
  and 300 seconds. Cancellation propagates through provider and MCP calls.
- Role instructions state the exact call budget, and each JSON tool-result envelope reports the remaining
  calls so the model can finalize instead of attempting an unauthorized extra call.
- No arbitrary file read, write, shell, Git mutation, generic process, QA, or RAG capability is provided.
- Traces contain safe operational metadata, not chain-of-thought, keys, environment variables, prompts,
  transcripts, or arbitrary source/tool contents.

The orchestrator passes bounded tool results through without interpreting, indexing, caching, persisting,
or copying them into traces. OpenAI receives only the typed assignment, role instructions, allowlisted
schemas, and bounded tool results needed for the current specialist execution.

## Build and offline tests

```powershell
dotnet build tools/DungeonEscape.AgentOrchestrator/DungeonEscape.AgentOrchestrator.csproj
dotnet test tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj
```

The tests use a fake `ILlmClient`, exercise the real MCP handshake/list/call integration, and require no
network or provider credentials. The credential-gated live test is skipped by default.

## Scripted demonstration

```powershell
dotnet build tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj
dotnet run --project tools/DungeonEscape.AgentOrchestrator/DungeonEscape.AgentOrchestrator.csproj -- --demo
```

The scripted example calls `get_project_info`, receives Reviewer approval, prints deterministic trace
transitions, and changes no files.

## OpenAI-backed local workflow

Set secrets only in the process environment; never commit them. A model is deliberately required rather
than silently defaulted because Responses/tool/structured-output support depends on the selected model.

```powershell
$env:OPENAI_API_KEY = '<your-api-key>'
$env:DUNGEONESCAPE_OPENAI_MODEL = '<responses-tool-capable-model>'
$env:DUNGEONESCAPE_LLM_TIMEOUT_SECONDS = '120' # optional, 1-300
$env:DUNGEONESCAPE_LLM_MAX_TOOL_CALLS = '8'    # optional, 1-20

dotnet build tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj
dotnet run --project tools/DungeonEscape.AgentOrchestrator/DungeonEscape.AgentOrchestrator.csproj -- `
  --provider openai `
  --repository-root "$PWD" `
  --task "Inspect the DungeonEscape project and identify the main classes involved in scene discovery. Explain how they interact and identify one potential maintenance risk. Do not modify anything."
```

The command starts MCP, initializes the real provider, runs the deterministic workflow, and prints the
typed proposal/verdict plus a safe trace. Missing credentials/model fail clearly without printing values.

Run the opt-in live provider test with the same two required environment variables:

```powershell
dotnet test tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj `
  --filter Category=LiveOpenAI
```

Final outputs use strict JSON schemas and deserialize into the existing hand-off records. Malformed,
incomplete, refused, timed-out, cancelled, over-budget, or provider-failed executions fail closed. Trace
metadata may include provider, model, response ID, duration, tool names/count, status, and token counts.

## Extension boundary

`IAgentExecutor` remains the workflow seam and `ILlmClient` is the deliberately small provider seam.
Future QA or retrieval can add typed transitions or another allowlisted capability without moving
orchestration into Unity or weakening the MCP boundary.
