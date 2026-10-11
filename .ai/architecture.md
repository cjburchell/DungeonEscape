# Current architecture

Source/configuration initially inspected on 2026-10-10 and updated for AI-010 on 2026-10-11.
It describes the local checkout and is not a claim that all components are validated today.

## Components and dependencies

| Component | Current role and boundary | Evidence |
| --- | --- | --- |
| `DungeonEscape.Unity/` | Active Unity runtime, editor integration, scenes and assets; Unity `6000.4.4f1` | [ProjectVersion.txt](../DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt), [AGENTS.md](../AGENTS.md) |
| `DungeonEscape.Core/` | Portable data/state, gameplay rules and view models; `netstandard2.0`, C# 7.3, Newtonsoft.Json; no Unity project reference | [Core project](../DungeonEscape.Core/DungeonEscape.Core.csproj) |
| `DungeonEscape.Core.Test/` | `net10.0` xUnit regression tests referencing Core | [test project](../DungeonEscape.Core.Test/DungeonEscape.Core.Test.csproj) |
| `DungeonEscape.Tools.GameEditor/` | Standalone `net8.0` Photino/Blazor desktop data editor referencing Core; nullable enabled, newer-runtime roll-forward allowed | [editor project](../DungeonEscape.Tools.GameEditor/DungeonEscape.Tools.GameEditor.csproj), [Program.cs](../DungeonEscape.Tools.GameEditor/Program.cs) |
| `tools/DungeonEscape.Mcp/` | Standalone `net10.0` stdio MCP metadata server with no Core/Unity reference | [MCP project](../tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj), [README](../tools/DungeonEscape.Mcp/README.md) |
| `tools/DungeonEscape.Mcp.Tests/` | Separate `net10.0` xUnit metadata/boundary and stdio protocol tests | [MCP test project](../tools/DungeonEscape.Mcp.Tests/DungeonEscape.Mcp.Tests.csproj) |
| `tools/DungeonEscape.AgentOrchestrator/` | Standalone `net10.0` deterministic Manager/Engineer/Reviewer state machine; no Unity reference or real LLM backend | [orchestrator README](../tools/DungeonEscape.AgentOrchestrator/README.md) |
| `tools/DungeonEscape.AgentOrchestrator.Tests/` | Separate `net10.0` xUnit workflow, permission, trace, failure and real MCP integration tests | [orchestrator test project](../tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj) |

[DungeonEscape.sln](../DungeonEscape.sln) contains Core, Core.Test and GameEditor. Unity is opened
as its own project; MCP and agent-orchestration projects are built separately. Core's `CopyCoreToUnity` post-build target
copies its DLL/PDB to `DungeonEscape.Unity/Assets/DungeonEscape/Plugins`. Building Core can therefore
write generated Unity plugin files even when only a .NET command was requested.

Legacy-named `DungeonEscape/`, `DungeonEscape.Test/`, `Nez.Portable/`, and
`DungeonEscape.Tools.MonsterEditor/` folders have no tracked files in this checkout and are not
solution projects. Their presence on disk does not identify an active runtime.

## Runtime flow and responsibilities

The only enabled build scene is `Assets/DungeonEscape/Scenes/Boot.unity`, per
[EditorBuildSettings.asset](../DungeonEscape.Unity/ProjectSettings/EditorBuildSettings.asset).
[Bootstrap](../DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Core/Bootstrap.cs) initializes
settings/display/audio, splash/title flow, loads and links a `DungeonEscapeDataSet`, populates
`GameDataCache`, and creates required game/UI objects. Its hidden fast-start setting can skip the
splash and load a quick save. This is Unity `MonoBehaviour` orchestration, not a uniform DI framework.

Under `Assets/DungeonEscape/Scripts/Unity/`:

- `Core/`: bootstrap, `GameState`, data/settings caches, input, audio, display and asset/JSON helpers.
  [GameState.cs](../DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Core/GameState.cs) implements
  Core's `IGame` and coordinates party/world state, interactions and persistence.
- `Map/`: player grid movement, followers and transitions; `Map/Tiled/` loads TMX/TSX,
  queries collision and renders map tiles/objects.
- `Rendering/`: directional sprites and saved hero sprite resolution.
- `UI/`: IMGUI menus, combat, dialogs and helper controls. Large screens are split into partial files;
  Core view models handle selected screen decisions, not all UI/runtime behavior.

Core separates file-backed contracts in `Data/`, mutable runtime/save models in `State/`,
engine-neutral calculations in `Rules/`, screen decisions in `ViewModels/`, and helpers in `Tools/`.
Examples: [Item](../DungeonEscape.Core/Data/Item.cs),
[StoreRules](../DungeonEscape.Core/Rules/StoreRules.cs),
[HealerViewModel](../DungeonEscape.Core/ViewModels/HealerViewModel.cs).
The boundary is incremental: significant orchestration and gameplay coordination remain Unity-side.

The active UI direction is IMGUI according to the tracked memory bank, and current `GameMenu`/`TitleMenu`
have `OnGUI` paths. Toolkit view and optional title preview code also remain; do not describe them as
removed or as a completed Toolkit migration. See [TitleMenu.cs](../DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/UI/TitleMenu.cs)
and [UI_TOOLKIT_MIGRATION.md](../memory-bank/UI_TOOLKIT_MIGRATION.md).

## Content, persistence and tooling

Unity's `Assets/DungeonEscape/` holds `Data/` JSON, `Maps/` TMX, `Tilesets/` TSX,
`Images/`, and `Audio/`. Bootstrap supports serialized `TextAsset` inputs and path-based fallbacks.
[UnityAssetPath](../DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Core/UnityAssetPath.cs)
resolves editor files or staged `StreamingAssets/DungeonEscape` content.
[UnityStreamingAssetsBuildProcessor](../DungeonEscape.Unity/Assets/DungeonEscape/Editor/UnityStreamingAssetsBuildProcessor.cs)
copies these content folders before builds. StreamingAssets output is generated, not a second source of truth.

`GameState` serializes shared `GameFile`/`GameSave` models using Newtonsoft.Json to `save.json`
under `Environment.SpecialFolder.ApplicationData/Redpoint/DungeonEscape`. It supports quick/manual
saves and archives unsupported save versions. This is file persistence, not a database service.
See [GameFile.cs](../DungeonEscape.Core/State/GameFile.cs) and the save/load methods in `GameState`.

GameEditor uses singleton services registered in `Program.cs`, Razor components, data-folder and
TMX metadata editing services, and JSON schemas. See its [README](../DungeonEscape.Tools.GameEditor/README.md)
and [Schemas](../DungeonEscape.Tools.GameEditor/Schemas/README.md). It is a development tool, not Unity UI.

MCP `Program.cs` explicitly registers only `ProjectInfoTools`; parameterless read-only
`get_project_info` delegates to `ProjectInfoService`. The trusted launcher may select an absolute
checkout with startup-only `--repository-root`; canonicalization, identity markers and reparse checks
are required, and invalid configuration fails startup without fallback. When omitted, discovery is
anchored to the executable's checkout location. Only fixed Unity/Git metadata paths are read; missing,
linked, malformed or unsupported metadata returns `not available`. No arbitrary filesystem, shell,
network or write tools are exposed; logs use stderr. The [MCP README](../tools/DungeonEscape.Mcp/README.md)
records precise limitations.

The AI-010 orchestrator is a separate consumer of that MCP boundary. Its application state machine
enforces Manager → Unity Engineer → Reviewer transitions, permits one Engineer revision after
`REQUEST_CHANGES`, and requires human handling after a second request. Manager has no tools; the two
specialists are allowlisted only for `get_project_info`, and the gateway rejects tools not advertised
as read-only, non-destructive, and closed-world. Typed trace events record dispatches, tool activity,
transitions, retries, and outcomes without recording reasoning transcripts or arbitrary source content.
`ScriptedAgentExecutor` is deterministic test/demonstration infrastructure, not an LLM implementation.
See the durable [agent role definitions](agents/README.md).

## Validation and delivery

Core tests use xUnit. Unity Edit/Play Mode test assemblies exist under
`Assets/DungeonEscape/Tests/` with NUnit test references. Repository `scripts/` provide .NET,
ReSharper, Unity validation/test and Windows build commands; use [AGENTS.md](../AGENTS.md) for exact invocations.
Both [GitHub Actions](../.github/workflows/ci.yml) and [GitLab CI](../.gitlab-ci.yml) configurations
exist and use `scripts/ci/` bash helpers. Their presence does not prove that hosted jobs are enabled or passing.

## Unknowns and limits

- **Unknown:** authoritative release CI host/branch and current hosted variable/license configuration.
- **Unknown:** fresh runtime, build and manual-test results for this documentation task; none were run.
- **Unknown:** full support guarantees for platforms other than the configured Windows player build.
- **Unknown:** eventual disposition of Toolkit previews and historical folders; no removal is implied.
- **Assumption for this summary:** inspected project files and call sites describe this checkout;
  older memory-bank statements are context/history and need checking when they conflict with source.

Use the [memory-bank index](../memory-bank/README.md) for detailed history, active work, known bugs
and manual checks; those records remain the durable handoff context.
