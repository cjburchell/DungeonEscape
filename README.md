# Dungeon Escape

Dungeon Escape is a retro-inspired 2D RPG migrated from the original MonoGame/Nez implementation to Unity.

The active target is the Unity project. The old MonoGame/Nez project has been removed from this branch; use `main` if old implementation reference is needed.

## Current Project Layout

```text
DungeonEscape.Unity/        # Active Unity project
DungeonEscape.Core/         # Unity-friendly shared domain/state code
DungeonEscape.Core.Test/    # Migration-relevant shared core tests
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

## Controls

Default keyboard and gamepad bindings:

| Action | Keyboard | Gamepad |
|---|---:|---:|
| Move left | `A` | D-pad left |
| Move right | `D` | D-pad right |
| Move up | `W` | D-pad up |
| Move down | `S` | D-pad down |
| Interact / confirm | `Space` | South button |
| Cancel / back | `Escape` | East button |
| Sprint | `LeftShift` | North button |
| Menu | `E` | West button |
| Previous tab | `[` | Left shoulder |
| Next tab | `]` | Right shoulder |
| Quick save | `F6` | Unbound |
| Quick load | `F9` | Unbound |

Bindings can be changed in the Unity game menu under Settings > Input Bindings.

## CI

GitLab CI restores, builds, and tests the solution. Unity validation and Windows player artifact builds are controlled with:

- `UNITY_CI_ENABLED`
- `UNITY_BUILD_WINDOWS_ENABLED`

Unity license variables are required for Unity CI jobs.
