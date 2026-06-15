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
- First core extraction completed: Tiled map path helpers, tile data parsing, and object bounds tile math now live in `DungeonEscape.Core` with unit tests.
- Save/location formatting extraction completed: save title/summary, usable-save checks, and return-location display names now live in `DungeonEscape.Core` with unit tests.
- Store/economy extraction completed: store metadata, inventory selection, buy/sell rules, sale prices, and sellable item filtering now live in `DungeonEscape.Core` with unit tests.
- Core data/state split completed: file-backed data contracts and parsed Tiled map contracts now live under `DungeonEscape.Core/Data`; runtime/save objects remain under `State`.
- Random item, quest progression/reward, and encounter generation extractions completed with focused core tests.
- Combat round rules extraction completed: action choice, target resolution/fallback, run outcomes, and execution dispatch now live in core with tests.
- UI drawing/logic split started with `StoreViewModel` in the core `ViewModels` namespace; store selection, filtering, metadata, and price decisions have matching core tests.
- `HealerViewModel` added under the core `ViewModels` namespace; healer service availability, costs, target filtering, metadata, and selection state have matching core tests.
- `TitleViewModel` and `GameMenuViewModel` added under the core `ViewModels` namespace; title navigation/create state, save-slot display rows, game-menu screen state, selection clamping, action availability, member filtering, row counts, detail counts, equipment candidate selection, item/spell use routing, item action labels, modal state, and settings adjustment/change effects have matching core tests.
- `CombatViewModel` added under the core `ViewModels` namespace; combat UI state, selected-index movement, action/menu display rows, spell/item labels, selected target lookup, and target candidate/type checks have matching core tests.
- Future feature backlog has been captured in `memory-bank/FUTURE_FEATURES.md`.
- Known bugs and rough edges are tracked in `memory-bank/BUGS.md`.
- The planned UI Toolkit migration was abandoned; the active direction is to keep the current IMGUI runtime UI and improve it incrementally.
- Added first-pass Unity UI Play Mode regression coverage for boot/runtime roots, title opening, create-game flow, and game-menu open/close behavior, plus a shared Play Mode helper for scene/object/reflection setup.
- Expanded Unity UI Play Mode regression coverage to combat open/message/autosave blocking, action-selection transition, target-selection return-to-action behavior, and combat close cleanup.
- Removed stale Toolkit-oriented Unity UI tests that no longer match the active IMGUI direction.
- Added `DungeonEscape.Tools.GameEditor`, a standalone Photino.Blazor desktop tool for editing monster JSON files (open/new/save any monster array file, searchable list with image thumbnails, add/duplicate/remove, and a full property editor with dropdowns for image/rarity/biomes/spells/skills/items). It references `DungeonEscape.Core` so saved JSON matches the game format.
- Game Editor: consolidated New/Open/Save/Save As into a single **File** dropdown menu, and added an `EditorSettingsService` that persists the last opened/saved file (`%AppData%/DungeonEscape.GameEditor/settings.json`) and **auto-loads it on startup**.
- Data Editor: expanded the Photino tool into a full Data-folder editor covering monsters, spells, skills, custom items, item definitions, quests, dialogs, class levels, stat names, and names. Added a collapsible validation panel for duplicate identifiers, required name/id checks, broken cross-references, invalid image IDs, missing class-level definitions, and dialog nesting/reference issues.
- Data Editor/Core data contracts: class-level entries and spell/item/item-definition class references now use string class names from `classlevels.json`; runtime hero state still uses the existing `Class` enum and compares against those strings by name.
- Data Editor: renamed the class-level tab to **Class** and made class stats a fixed normalized list of Health, Attack, Defence, MagicDefence, Agility, and Magic rather than add/remove rows.
- Data Editor: class stat rows now show the stat name as the group header and display the initial character stat roll as `RollTimes`d`Roll`+`StartConst`, matching `Stats.RollStartValue()`.
- Data Editor: dialog choice `NextQuestStage` is now shown only when an effective quest is available, uses a dropdown of that quest's stages, and treats `0` as none.
- Data Editor: save output is now sparse JSON, omitting null/default values, zeroes, `false`, empty strings, empty arrays, and empty objects while keeping root files valid.
- Data Editor: Stat Names is now a fixed normalized list of Agility, Defence, Health, Attack, Magic, and MagicDefence; rows cannot be added, duplicated, removed, or retargeted to another stat.
- Data Editor/Unity title flow: class definitions now support a default hero-sheet image index, the Class editor exposes a visual picker for it, and New Quest class selection applies that default image automatically.
- Data Editor: added a **Maps** tab that auto-detects Unity TMX maps from the opened Data folder's asset root and edits gameplay metadata without taking over Tiled-owned layout/display data. It supports map properties, object `name`/`class`, friendly NPC/chest/door/warp property forms, and per-map random monster JSON files under `Data/maps/{mapId}_monsters.json`; overworld encounters remain biome-driven from monster data. The map root `class` is edited as an Overworld checkbox only: checked saves `class="Overworld"`, unchecked saves no map class. Advanced raw TMX property add/remove editors are hidden to avoid unsupported map metadata edits. Spawn objects only expose `DefaultSpawn`; warp objects only expose `WarpMap` and `SpawnId`, with empty `SpawnId` labeled as the target map's default spawn. Chest/hidden item forms hide unused `MoveRadius` and the legacy `Gold` fallback; runtime still supports `Gold` when no `ItemId` is present, but current maps use `ItemId=#Random#`, which can generate gold. Integer-valued map metadata uses number inputs while saving as TMX property strings. Map validation now covers unsupported map root classes, broken dialog/item/key/warp/class/monster references, duplicate TMX object ids within the same object layer, and missing explicit chest/door lock metadata. Map object/dialog reference fields now use shared styled selects, with `WarpMap`/dialog `MapId` linked to target-map `SpawnId` options while preserving unknown custom values. The Maps tab object selector uses a fixed-height scrolling list so it does not grow with large object counts. Item and monster reference dropdowns show thumbnails when possible.



## In Progress

- Feature development and next-phase architecture planning.
- Active architecture ideas are tracked in `memory-bank/ARCHITECTURE_BACKLOG.md`.
- Completed architecture work is archived in `memory-bank/ARCHITECTURE_COMPLETED.md`.

## Deferred

- Expand shared core unit tests beyond level-up and skill/spell progression.
- Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.
- Expand Unity Play Mode UI regression coverage to deeper game-menu tabs/modals/settings plus store and healer flows.

## Current Known Backlog Items

See `memory-bank/UNITY_MIGRATION_COMPLETED.md` for the final migration record and `memory-bank/UNITY_MIGRATION.md` for active post-migration follow-up. Main backlog groups:

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
- Remove or archive leftover experimental UI Toolkit code and notes when they are confirmed unused.
