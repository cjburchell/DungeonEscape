# Project Brief

## Goal

Migrate Dungeon Escape from the old MonoGame/Nez implementation into a Unity project while reusing assets and as much portable C# gameplay/domain code as possible.

## Current Direction

The active game target is `DungeonEscape.Unity`. The old MonoGame/Nez project has been removed from this branch; use `main` if old implementation reference is needed.

The migration strategy is:

1. Keep shared game state and engine-neutral rules in `DungeonEscape.Core`.
2. Build Unity-specific adapters and runtime systems around the shared core.
3. Finish map-mode gameplay before combat.
4. Defer audio until after combat.
5. Keep manual tests and migration status updated in `memory-bank`.

## Non-Goals

- Do not migrate old MonoGame save files.
- Do not add Unity, MonoGame, or Nez references to `DungeonEscape.Core`.

## Handoff Rule

Before starting work, read:

- `memory-bank/activeContext.md`
- `memory-bank/progress.md`
- `memory-bank/UNITY_MIGRATION.md`
- `memory-bank/MANUAL_TESTS.md`

