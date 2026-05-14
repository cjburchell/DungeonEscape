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
- Implemented persisted hero sprite selection independent from class/gender, including New Quest image selection and map-gid-based recruit sprites.
- Implemented party, inventory, quest, settings, save, store, healer, title/load UI.
- Implemented configurable UI scale/style and gamepad/keyboard input rebinding.
- Implemented `Outside`, `Return`, `Wings`, and `Open` map-mode behavior.
- Implemented combat target selection through displayed monster sprites for enemy targets and the always-visible party status window for party targets.
- Recreated the old splash screen in Unity and hid the map behind a black startup/title backdrop.
- Added hidden fast-start setting to skip splash/title and load the quick save for testing.
- Added title New Quest create-player flow with random names/dropdowns/portrait/stats/re-roll, variable manual-save load/delete, and in-game Main Menu/Quit actions.
- Added GitLab CI solution build/test and Unity validation/build artifact support.
- Added memory-bank docs.
- Removed the old `DungeonEscape.Test` project from the solution; migration tests should live in `DungeonEscape.Core.Test` or future Unity test assemblies.
- Removed the old MonoGame/Nez project and `Nez.Portable` from this branch; use `main` if old implementation reference is needed.
- Unity migration is considered complete; remaining automation and warning-review items are accepted as post-migration backlog.

## In Progress

- Feature development and architecture cleanup planning.
- Core extraction ideas are tracked in `memory-bank/ARCHITECTURE_BACKLOG.md`.
- First core extraction completed: Tiled map path helpers, tile data parsing, and object bounds tile math now live in `DungeonEscape.Core` with unit tests.
- Save/location formatting extraction completed: save title/summary, usable-save checks, and return-location display names now live in `DungeonEscape.Core` with unit tests.
- Store/economy extraction completed: store metadata, inventory selection, buy/sell rules, sale prices, and sellable item filtering now live in `DungeonEscape.Core` with unit tests.
- Core data/state split completed: file-backed data contracts and parsed Tiled map contracts now live under `DungeonEscape.Core/Data`; runtime/save objects remain under `State`.
- Random item, quest progression/reward, and encounter generation extractions completed with focused core tests.
- Combat round rules extraction completed: action choice, target resolution/fallback, run outcomes, and execution dispatch now live in core with tests.
- Future feature backlog has been captured in `memory-bank/FUTURE_FEATURES.md`.
- Known bugs and rough edges are tracked in `memory-bank/BUGS.md`.

## Deferred

- Expand shared core unit tests beyond level-up and skill/spell progression.
- Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.

## Current Known Backlog Items

See `memory-bank/UNITY_MIGRATION.md` for the final migration record. Main post-migration backlog groups:

- Expand shared core unit tests.
- Add Unity edit mode tests for map loading, hidden item conditions, save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues.
- Replace remaining runtime filesystem asset loading with Unity-native references where appropriate.
- Remove remaining temporary/debug code when no longer needed.
- Decide whether old developer/debug console commands should be recreated.
- Review and prioritize core extraction work in `memory-bank/ARCHITECTURE_BACKLOG.md`.
- Review and prioritize future feature ideas in `memory-bank/FUTURE_FEATURES.md`.
- Triage and prioritize known issues in `memory-bank/BUGS.md`.
