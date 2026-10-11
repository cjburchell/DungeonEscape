# Existing coding conventions

These rules summarize inspected code and configuration; they do not introduce a repository-wide
style migration. Follow the surrounding file when conventions vary.

## Compatibility and boundaries

- Core targets `netstandard2.0` with explicit C# `7.3` and nullable disabled. Keep Core changes within
  those settings and engine-neutral; do not introduce Unity dependencies or modern syntax it cannot compile.
  Evidence: [Core project](../DungeonEscape.Core/DungeonEscape.Core.csproj).
- GameEditor targets `net8.0`, with nullable and implicit usings enabled. MCP and its tests target
  `net10.0`. Do not apply those settings or syntax globally to Core or Unity.
- Put portable rules and view-model decisions alongside existing Core helpers. Keep Unity lifecycle,
  rendering, input and scene/asset integration in Unity. This is the existing incremental extraction
  pattern, not a claim that all game logic already lives in Core.

## Layout and naming

- Representative Core and Unity C# use four-space indentation, braces on separate lines,
  block namespaces, PascalCase types/methods/properties and camelCase parameters/private fields.
  Core namespaces follow `Redpoint.DungeonEscape.Data`, `.State`, `.Rules`, `.ViewModels`, and `.Tools`.
  Unity namespaces follow `Redpoint.DungeonEscape.Unity` responsibility folders.
- Match the local style: GameEditor uses file-scoped namespaces, and MCP uses modern C# syntax.
  Using order varies; do not mass-reorder existing files to invent a new convention.
- Existing Unity `GameMenu.*.cs` and `CombatWindow.*.cs` split large screen implementations into
  partial classes. Extend the relevant responsibility file instead of creating duplicate screen logic.
- [`.editorconfig`](../.editorconfig) mainly scopes ReSharper diagnostics for DTOs, dynamically
  consumed members and Unity code; it does not establish a comprehensive formatting policy.
  **Unknown:** any additional team formatter policy beyond these checked-in settings and examples.

Examples: [StoreRules.cs](../DungeonEscape.Core/Rules/StoreRules.cs),
[HealerViewModel.cs](../DungeonEscape.Core/ViewModels/HealerViewModel.cs),
[GameEditor Program.cs](../DungeonEscape.Tools.GameEditor/Program.cs).

## Contracts and UI

- Data/save contracts intentionally expose mutable properties. Newtonsoft.Json attributes map
  serialized IDs and enum values and exclude linked runtime objects; preserve the existing contract
  shape unless a change is explicitly in scope. See [Item.cs](../DungeonEscape.Core/Data/Item.cs).
- Use existing Core rule/view-model methods and Unity `UiControls`, `UiTheme`, `UiSettings` and
  asset helpers when they fit. Preserve modal/input blocking and keyboard/gamepad behavior.
- Preserve Unity serialized fields, asset references and `.meta` identity when editing Unity content.
  Never edit generated project/build artifacts to make a source change appear to work.
- Tiled object metadata uses `class`, not legacy object `type`. Preserve explicit lock/spawn/warp
  properties and unknown values when working with data. See [systemPatterns.md](../memory-bank/systemPatterns.md)
  and the actual loader/editor code before altering metadata.

## Tests and validation

- Core tests mirror responsibility folders and use xUnit `[Fact]`, `[Theory]`, `[InlineData]`,
  descriptive behavior names and direct assertions. See [StoreRulesTests.cs](../DungeonEscape.Core.Test/Rules/StoreRulesTests.cs).
- Unity has separate Edit Mode and Play Mode test assemblies under `Assets/DungeonEscape/Tests/`.
  Use the existing helpers and NUnit patterns rather than mixing xUnit into Unity tests.
- Run core tests for shared rules/state/data/view-model changes, Unity Edit Mode tests for Unity-side
  changes, and Play Mode tests for runtime/UI/scene/map/combat/persistence changes when available,
  as required by [AGENTS.md](../AGENTS.md). Report blockers and manual checks explicitly.
- Documentation-only changes need structure/link/source review and diff checks; do not report
  historical build/test results as validation of the current change.
