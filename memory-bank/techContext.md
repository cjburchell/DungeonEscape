# Tech Context

## Languages And Frameworks

- C#
- Unity 6.0 line, currently using Unity project under `DungeonEscape.Unity`
- .NET solution with `dotnet build` and `dotnet test`
- Shared core targets `netstandard2.0`

## Key Dependencies

- Newtonsoft.Json for JSON data loading.
- Unity runtime APIs for rendering/input/UI.
- Tiled `.tmx` maps and `.tsx` tilesets.

## Important Paths

- Unity project: `DungeonEscape.Unity/`
- Unity scripts: `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/`
- Unity maps: `DungeonEscape.Unity/Assets/DungeonEscape/Maps/`
- Unity data: `DungeonEscape.Unity/Assets/DungeonEscape/Data/`
- Unity tilesets: `DungeonEscape.Unity/Assets/DungeonEscape/Tilesets/`
- Shared core: `DungeonEscape.Core/`
- Shared tests: `DungeonEscape.Core.Test/`
- Project docs: `memory-bank/`

## Commands

Build:

```powershell
dotnet build DungeonEscape.sln
```

Test:

```powershell
dotnet test DungeonEscape.sln --no-restore
```

Whitespace check:

```powershell
git diff --check
```

## CI

GitLab CI is configured for solution restore/build/test and Unity validation/build when enabled. Unity Windows build artifacts can be downloaded from CI.

ReSharper CI scans `DungeonEscape.sln` and, when Unity validation runs, the Unity-generated solution/project files produced by the validation job.

Relevant variables:

- `UNITY_CI_ENABLED`
- `UNITY_BUILD_WINDOWS_ENABLED`
- `UNITY_LICENSE_B64`, `UNITY_LICENSE`, or Unity serial/email/password variables

The old `DungeonEscape.Test` project has been removed. `dotnet test DungeonEscape.sln --no-restore` should run the current automated shared-core tests in `DungeonEscape.Core.Test`.

## Editor Notes

Unity external editor settings may need to point to the currently installed Visual Studio/Rider instead of old Visual Studio 2017 paths.

Unity package manager may need Newtonsoft Json support installed for editor/runtime compatibility.
