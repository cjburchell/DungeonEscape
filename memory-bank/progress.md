# Progress

## Done

- Created Unity project structure and imported Unity-compatible assets.
- Created `DungeonEscape.Core` shared project.
- Migrated major portable state/domain models into shared core.
- Built Unity map loading/rendering from TMX/TSX files.
- Implemented map layer ordering and sprite/player sorting.
- Implemented movement, collision, continuous movement, sprint, water/ship rules, biome/damage data layers.
- Implemented map warps, default spawns, overworld return fallback, and fade transitions.
- Implemented chests, hidden items, opened doors, removed objects, object persistence, and save/load behavior.
- Implemented quest/dialog item give/take/progression paths for known starter quests.
- Implemented party creation, starter equipment, recruitment, followers, cart visual, coffin visual, party animations.
- Implemented party, inventory, quest, settings, save, store, healer, title/load UI.
- Implemented configurable UI scale/style and gamepad/keyboard input rebinding.
- Implemented `Outside`, `Return`, `Wings`, and `Open` map-mode behavior.
- Recreated the old splash screen in Unity and hid the map behind a black startup/title backdrop.
- Added hidden fast-start setting to skip splash/title and load the quick save for testing.
- Added title New Quest create-player flow with random names/dropdowns/portrait/stats/re-roll, variable manual-save load/delete, and in-game Main Menu/Quit actions.
- Added GitLab CI solution build/test and Unity validation/build artifact support.
- Added memory-bank docs.
- Removed the old `DungeonEscape.Test` project from the solution; migration tests should live in `DungeonEscape.Core.Test` or future Unity test assemblies.

## In Progress

- UI migration polish.
- Persistence/title/create-player flow decisions.
- Unity project cleanup.
- Build/test automation expansion.

## Deferred

- Encounter/combat migration.
- Audio/music/sound effects, until after combat.

## Current Known Pending Items

See `memory-bank/UNITY_MIGRATION.md` for the authoritative list. Main pending groups:

- Fullscreen setting runtime/UI wiring if still desired.
- Decide whether old splash/create-player flow should be recreated.
- Expand shared core unit tests.
- Add Unity edit mode tests for map loading, hidden item conditions, save/load behavior.
- Review ReSharper warnings and fix actionable issues.
- Replace remaining runtime filesystem asset loading with Unity-native references where appropriate.
- Remove remaining temporary/debug code when no longer needed.
- Decide whether old developer/debug console commands should be recreated.
