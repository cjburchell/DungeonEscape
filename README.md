# Dungeon Escape

Dungeon Escape is a retro-inspired 2D RPG being migrated from the original MonoGame/Nez implementation to Unity.

In this branch, the active target is the Unity project. The old `DungeonEscape/` MonoGame project remains in the repository for reference while the migration is completed, but migration work should happen in `DungeonEscape.Unity/`, `DungeonEscape.Core/`, tests, CI, and `memory-bank/`.

## Current Project Layout

```text
DungeonEscape.Unity/        # Active Unity project
DungeonEscape.Core/         # Unity-friendly shared domain/state code
DungeonEscape.Core.Test/    # Migration-relevant shared core tests
DungeonEscape/              # Old MonoGame/Nez project, reference only in this branch
Nez.Portable/               # Old Nez dependency used by the old project
memory-bank/                # Migration status, manual tests, architecture, handoff docs
```

## Memory Bank

Use `memory-bank/` as the source of truth for migration context:

- `memory-bank/UNITY_MIGRATION.md`: migration status and pending work.
- `memory-bank/MANUAL_TESTS.md`: manual play-test plans.
- `memory-bank/architecture.md`: current architecture overview.
- `memory-bank/activeContext.md`: current working context.
- `memory-bank/progress.md`: high-level progress summary.

## Build And Test

### .NET

```powershell
dotnet restore DungeonEscape.sln
dotnet build DungeonEscape.sln
dotnet test DungeonEscape.sln --no-restore
```

The old `DungeonEscape.Test` project has been removed. Current automated migration tests run through `DungeonEscape.Core.Test`; Unity edit/play mode tests can be added later.

### Unity

Open:

```text
DungeonEscape.Unity/
```

The active boot scene is:

```text
DungeonEscape.Unity/Assets/DungeonEscape/Scenes/Boot.unity
```

Unity runtime assets are under:

```text
DungeonEscape.Unity/Assets/DungeonEscape/
  Data/
  Images/
  Maps/
  Scripts/
  Tilesets/
```

## Data And Maps

The Unity migration uses data and assets under `DungeonEscape.Unity/Assets/DungeonEscape/`.

Tiled object metadata in Unity maps should use `class="..."`, not object `type="..."`. Chest and door locking is explicit:

- Chests use `class="Chest"` and default map data should set `Locked=false`.
- Doors use `class="Door"` and default map data should set `Locked=true`.

## CI

GitLab CI restores, builds, and tests the solution. Unity validation and Windows player artifact builds are controlled with:

- `UNITY_CI_ENABLED`
- `UNITY_BUILD_WINDOWS_ENABLED`

Unity license variables are required for Unity CI jobs.
