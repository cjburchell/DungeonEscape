# Unity Project Instructions For Codex

Use Unity Editor version `6000.4.4f1`.

The Unity project is in `DungeonEscape.Unity`, not the repository root.

## Editing Rules

- Do not edit generated Unity or build folders: `DungeonEscape.Unity/Library/`, `DungeonEscape.Unity/Temp/`, `DungeonEscape.Unity/Obj/`, `DungeonEscape.Unity/Build/`, `DungeonEscape.Unity/Builds/`, `DungeonEscape.Unity/Logs/`, or `DungeonEscape.Unity/UserSettings/`.
- Prefer changes under `DungeonEscape.Unity/Assets/`, `DungeonEscape.Unity/Packages/`, `DungeonEscape.Unity/ProjectSettings/`, `DungeonEscape.Core/`, `DungeonEscape.Core.Test/`, and project tool folders such as `DungeonEscape.Tools.GameEditor/`.
- Do not edit generated `.csproj` files under the Unity project unless the user explicitly asks.

## Memory Bank

- Treat `memory-bank/` as the persistent project context for Codex, similar to project rules.
- At the start of a new task, read `memory-bank/README.md` and `memory-bank/activeContext.md` before making planning assumptions.
- When the task touches roadmap, current focus, completed work, bugs, or manual validation, also read the relevant file such as `memory-bank/progress.md`, `memory-bank/FUTURE_FEATURES.md`, `memory-bank/BUGS.md`, or `memory-bank/MANUAL_TESTS.md`.
- After meaningful gameplay, architecture, tooling, validation, or planning changes, update the appropriate memory-bank files so the next session has current context.

## Test Commands

- Restore: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-restore.ps1`
- Solution build: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-build.ps1 -NoRestore`
- Core tests: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-test.ps1 -NoRestore`
- ReSharper scan: `powershell -ExecutionPolicy Bypass -File scripts\run-resharper.ps1`
- Unity validation: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-validate.ps1`
- Unity Edit Mode: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-editmode-tests.ps1`
- Unity Play Mode: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-playmode-tests.ps1`
- Unity Windows build: `powershell -ExecutionPolicy Bypass -File scripts\build-unity-windows.ps1`

The Unity scripts default to `C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe`. To use another install, set `UNITY_EXE` or pass `-Unity`.

GitLab CI uses the matching bash scripts under `scripts/ci/`.

## Validation Guidance

- Run core tests for shared rules, state, data, and view model changes.
- Run Unity Edit Mode tests before finalizing Unity-side changes when the editor is available.
- Run Unity Play Mode tests for gameplay, runtime UI, scene flow, map, combat, or persistence changes.
