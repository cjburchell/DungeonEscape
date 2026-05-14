# Unity Migration Status

The Dungeon Escape Unity migration is complete. Completed migration details are archived in `UNITY_MIGRATION_COMPLETED.md`.

## Current Status

- Map-mode gameplay, party systems, UI, persistence, audio, combat, build automation, and Unity cleanup are migrated enough to move on to feature and architecture work.
- The old MonoGame/Nez project was removed from this branch; use `main` if old implementation reference is needed.
- Current active architecture work is tracked in `ARCHITECTURE_BACKLOG.md`.
- Future feature ideas are tracked in `FUTURE_FEATURES.md`.
- Known bugs and rough edges are tracked in `BUGS.md`.

## Deferred Post-Migration Work

- Expand automated tests:
  - shared core unit tests where coverage is still thin
  - Unity edit mode tests
  - Unity play mode tests for high-risk UI/gameplay flows
  - quest/dialog regression tests
- Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.
- Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Add regression tests for quest dialog actions and item rewards.

## Final Migration Record

See `UNITY_MIGRATION_COMPLETED.md`.
