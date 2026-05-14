# Unity Project Instructions For Codex

Use Unity Editor version `6000.4.4f1`.

The Unity project is in `DungeonEscape.Unity`, not the repository root.

## Editing Rules

- Do not edit generated Unity or build folders: `DungeonEscape.Unity/Library/`, `DungeonEscape.Unity/Temp/`, `DungeonEscape.Unity/Obj/`, `DungeonEscape.Unity/Build/`, `DungeonEscape.Unity/Builds/`, `DungeonEscape.Unity/Logs/`, or `DungeonEscape.Unity/UserSettings/`.
- Prefer changes under `DungeonEscape.Unity/Assets/`, `DungeonEscape.Unity/Packages/`, `DungeonEscape.Unity/ProjectSettings/`, `DungeonEscape.Core/`, and `DungeonEscape.Core.Test/`.
- Do not edit generated `.csproj` files under the Unity project unless the user explicitly asks.

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
