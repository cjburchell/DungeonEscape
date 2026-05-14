# Completed Architecture Work

This file archives architecture cleanup that has been completed. Active and future architecture work belongs in `ARCHITECTURE_BACKLOG.md`.

## Extract Unity Logic Into Core

Goal completed: separate engine-neutral rules from Unity scripts into `DungeonEscape.Core`, then add focused unit tests around the extracted logic.

### Map And Tiled Helpers

- Extracted map path normalization out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Loader.cs`.
- Extracted tileset/image path resolution out of `Loader.cs`.
- Extracted CSV tile data and Tiled GID parsing out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Collision.cs`.
- Extracted object-bounds-to-blocked-tile coordinate math out of `Collision.cs`.
- Added shared core unit tests for the extracted Tiled helper behavior.
- Unity `Loader` and `Collision` still expose their existing APIs, but delegate pure logic to `DungeonEscape.Core`.

### Save And Display Formatting

- Extracted game save title/summary formatting out of `GameState`.
- Extracted save usability checks and return-location display-name formatting into `GameSaveFormatter`.
- Added shared core unit tests for save summary/title formatting, usable-save detection, and location-name formatting.

### Store And Economy Rules

- Extracted sellable item filtering and sale price rules out of `StoreWindow`.
- Extracted store metadata, initial inventory selection, invalid inventory checks, and buy/sell operation rules out of `GameState`.
- Added shared core unit tests for store metadata, key stores, buy recipients, sellable filtering, sale prices, fixed/key inventory initialization, and buy/sell behavior.

### Core State And Data Organization

- Split file-backed data contracts out of `DungeonEscape.Core/State` into `DungeonEscape.Core/Data` with the `Redpoint.DungeonEscape.Data` namespace.
- Runtime/save objects and shared gameplay value types remain in `State`.

### Random Item Rules

- Extracted `CreateRandomItem`, `CreateRandomEquipment`, rarity selection, stat selection, and equipment naming out of `GameState`.
- Added unit tests for generated item level ranges, rarity behavior, stat ranges, class/slot constraints, and name construction.

### Quest Rules

- Extracted `StartQuest`, `AdvanceQuest`, XP/gold/item rewards, and completed-stage behavior out of `GameState`.
- Added unit tests for completed quests not restarting, reward grants, stage advancement, and item rewards.

### Encounter Rules

- Extracted random encounter filtering, rarity weights, monster group selection, and repel filtering out of `GameState`.
- Added unit tests for biome filtering, min/max monster levels, rarity weighting, group limits, party-level scaling, and repel behavior.

### Combat Round Rules

- Extracted monster action choice, action resolvability, target fallback, target selection helpers, run chance, and round action execution out of `CombatWindow.RoundFlow.cs`.
- Added focused core tests around combat round action choice, target resolution/fallback, run outcomes, and execution dispatch.
- Unity combat UI still owns rendering, input, sound, and animation orchestration.

## Split UI Drawing From UI Logic

Goal completed for current scope: reduce the size and coupling of Unity UI classes by separating drawing/layout code from state transitions, filtering, command handling, and display data preparation.

### Store UI

- Added `StoreViewModel` under `Redpoint.DungeonEscape.ViewModels`.
- Moved store metadata, selected tab/focus/index state, selection clamping, buy eligibility, sellable filtering, and price display into the view model.
- Added unit tests for `StoreViewModel`.
- `StoreWindow` still owns IMGUI drawing, modal rendering, scroll positions, Unity input repeat timing, sounds, and direct game command execution.

### Healer UI

- Added `HealerViewModel` under `Redpoint.DungeonEscape.ViewModels`.
- Moved healer metadata, service list construction, target filtering, costs, and selected service/target state into the view model.
- Added unit tests for `HealerViewModel`.
- `HealerWindow` still owns IMGUI drawing, Unity input repeat timing, sounds, and direct game command execution.

### Title UI

- Added `TitleViewModel` under `Redpoint.DungeonEscape.ViewModels`.
- Moved title mode, main-row availability, create-player choices, load/create navigation, dropdown indexes, blocked create-image selection, load-slot display rows, and load selection clamping into the view model.
- Added unit tests for `TitleViewModel`.
- `TitleMenu` still owns IMGUI drawing, background textures, audio, save/load/delete execution, random name generation, and Unity app quit/close behavior.

### Game Menu UI

- Added `GameMenuViewModel` under `Redpoint.DungeonEscape.ViewModels`.
- Moved menu screen/focus/tab state, selected indexes, row/detail clamping, page-based detail selection, main action availability, member ordering/filtering, settings/save/load row counts, detail counts, equipment slots, equipped item lookup, equipment candidate selection, item/spell use routing, item action labels, settings adjustment/activation effects, modal display/selection state, and mouse-driven settings change effects into the view model.
- Added unit tests for `GameMenuViewModel`.
- `GameMenu` still owns IMGUI drawing, scroll positions, modal command execution, input rebinding capture, settings persistence, and gameplay actions.

### Combat UI

- Added `CombatViewModel` under `Redpoint.DungeonEscape.ViewModels`.
- Moved combat UI state, selected-index movement, wraparound monster target selection, action/menu display rows, spell/item display labels, selected target lookup, target candidate checks, and party/monster target detection into the view model.
- Added unit tests for `CombatViewModel`.
- `CombatWindow` still owns battlefield rendering, menu rendering, target click handling, message reveal timing, audio, animation flashes, and action execution orchestration.

## UI Pattern Notes

- Unity can support MVVM-style patterns, especially with UI Toolkit data binding.
- The current UI is IMGUI-based, so strict MVVM is not a natural fit because drawing and event handling happen together during `OnGUI`.
- A pragmatic fit for the current code is view models plus presenter/controller classes:
  - View models hold display-ready data, selected indexes, enabled/disabled state, labels, and validation messages.
  - Presenters/controllers translate input commands into state changes and game actions.
  - IMGUI view classes draw controls and forward user intents.
- If the UI is later moved to UI Toolkit, the view-model layer can become a stronger MVVM boundary.
