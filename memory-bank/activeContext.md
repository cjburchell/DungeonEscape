# Active Context

## Current Focus

The Unity migration is considered complete. The project is moving from migration tracking to new feature work and architecture cleanup. Recent completed migration work included:

- `Open` spell/items act on the object the player is facing.
- `Open` does not ask for a party-member target.
- Doors and chests use a shared facing-object open path.
- Chests support `Locked=true`; current chests are explicitly `Locked=false`.
- Doors are explicitly `Locked=true` unless intended to be unlocked.
- Unity TMX object metadata is normalized to `class="..."`; object `type="..."` should not be used.
- Startup now shows the old splash image before the title menu.
- Splash/title UI draws on a black backdrop so the map is not visible until gameplay starts.
- Hidden `SkipSplashAndLoadQuickSave` can be enabled in the settings file for fast Play Mode testing.
- Title flow now includes create-player, variable manual-save load/delete, hidden Continue/Load Quest buttons when unavailable, and in-game return-to-main-menu/quit actions.
- New Quest create-player now lets the player choose a hero-sheet image independently from class and gender.
- Hero save data now persists explicit sprite selection fields. UI portraits, player map sprite, and party followers resolve from that stored sprite data.
- Recruited `NpcPartyMember` heroes copy their sprite from the map object's referenced Tiled sprite `gid`, storing the resolved tileset path and local tile id.
- Combat target selection no longer opens a separate target list. Enemy targets are selected directly from the displayed monster sprites, while party targets are selected directly from the always-visible party status window with a highlight around the selected member. Keyboard/gamepad target selection still highlights and confirms the current target.

## Important Current Constraint

The old MonoGame/Nez project was removed from this branch. Use `main` if old implementation reference is needed. Work should target:

- `DungeonEscape.Unity/`
- `DungeonEscape.Core/`
- tests/docs/CI as needed

## Recently Touched Areas

- `DungeonEscape.Core/State/TiledMapInfo.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Maps/**/*.tmx`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/GameMenu.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/GameState.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/PlayerGridController.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Rendering/HeroSpriteResolver.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/TitleMenu.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/UI/CombatWindow.cs`
- `DungeonEscape.sln`
- `memory-bank/MANUAL_TESTS.md`
- `memory-bank/UNITY_MIGRATION.md`
- `memory-bank/`

## Recent Validation

Recent checks passed:

- `dotnet build DungeonEscape.sln`
- `dotnet test DungeonEscape.sln --no-restore`
- `git diff --check`

Latest local verification for hero sprite selection/recruit sprite work:

- `dotnet test DungeonEscape.sln`
- Unity batch validation was blocked because another Unity editor instance already had `DungeonEscape.Unity` open.

Latest local verification for combat target UI work:

- `dotnet build DungeonEscape.sln`
- `dotnet test DungeonEscape.sln --no-build`

Unity map metadata validation showed:

- Chests: `109`
- Doors: `5`
- Missing `Locked`: `0`
- Wrong `Locked`: `0`
- Object-level `type=` count: `0`

## Next Likely Work

Move on from migration completion into new feature and architecture work. Current deferred migration backlog is accepted as non-blocking:

- Expand shared core unit tests beyond level-up and skill/spell progression.
- Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.

Core extraction ideas are tracked in `memory-bank/ARCHITECTURE_BACKLOG.md`.
Future feature ideas are now tracked in `memory-bank/FUTURE_FEATURES.md`.
Known bugs and rough edges are tracked in `memory-bank/BUGS.md`.

Latest architecture cleanup:

- Extracted Tiled map path helpers, tile data parsing, and object bounds tile math into `DungeonEscape.Core`.
- Added unit tests for the extracted Tiled helpers.
- Extracted save/location formatting into `DungeonEscape.Core`.
- Added unit tests for save title/summary, usable-save checks, and return-location display names.
- Extracted store/economy rules into `DungeonEscape.Core`.
- Added unit tests for store metadata, inventory selection, buy/sell rules, sale prices, and sellable item filtering.
- Split file-backed data contracts into `DungeonEscape.Core/Data` and `Redpoint.DungeonEscape.Data`; runtime/save objects remain in `State`.
- Extracted random item generation, quest progression/rewards, and random encounter rules into core rule classes with unit tests.
- Extracted combat round rules into `CombatRoundRules`; Unity combat UI still owns rendering, input, sound, and animation effects.
- Began the UI drawing/logic split with `StoreViewModel` in `Redpoint.DungeonEscape.ViewModels` and matching `ViewModels` tests; `StoreWindow` now delegates selected store UI decisions to it.
- Added `HealerViewModel` in `Redpoint.DungeonEscape.ViewModels` with matching tests; `HealerWindow` now delegates healer metadata, service list, target filtering, costs, and selection state to it.
- Added `TitleViewModel` and expanded `GameMenuViewModel` in `Redpoint.DungeonEscape.ViewModels` with matching tests; `TitleMenu` and `GameMenu` now delegate title navigation/create state plus game-menu screen state, selection clamping, action availability, member filtering, row counts, detail counts, and equipment candidate selection to core view models.
- Added `CombatViewModel` in `Redpoint.DungeonEscape.ViewModels` with matching tests; `CombatWindow` now delegates combat UI state and selected-index movement to core.

After every implemented gameplay step, update `memory-bank/MANUAL_TESTS.md` with manual verification steps.
