# Project Brief

## Goal

Keep Dungeon Escape moving forward as a Unity RPG while preserving engine-neutral gameplay/domain rules in shared C# code.

## Current Direction

The active game target is `DungeonEscape.Unity`. The old MonoGame/Nez project has been removed from this branch; use `main` if old implementation reference is needed.

The current engineering direction is:

1. Keep shared game state and engine-neutral rules in `DungeonEscape.Core`.
2. Build Unity-specific adapters and runtime systems around the shared core.
3. Treat the Unity migration as complete and keep migration leftovers as deferred backlog.
4. Prioritize new feature work and architecture cleanup in the Unity runtime and shared core.
5. Keep manual tests, architecture notes, and active context updated in `memory-bank`.

## Non-Goals

- Do not migrate old MonoGame save files.
- Do not add Unity, MonoGame, or Nez references to `DungeonEscape.Core`.

## Handoff Rule

Before starting work, read:

- `memory-bank/activeContext.md`
- `memory-bank/progress.md`
- `memory-bank/UNITY_MIGRATION.md`
- `memory-bank/MANUAL_TESTS.md`

