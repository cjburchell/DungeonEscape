# Architecture Backlog

This file tracks architecture cleanup ideas that are not active work yet.

## Extract Unity Logic Into Core

Goal: separate engine-neutral rules from Unity scripts into `DungeonEscape.Core`, then add focused unit tests around the extracted logic.

### Completed Extractions

- Extracted map path normalization, tileset path resolution, Tiled CSV/GID parsing, boolean parsing, and object-bounds tile index math into core helpers.
- Added shared core unit tests for the extracted Tiled helper behavior.
- Unity `Loader` and `Collision` still expose their existing APIs, but delegate the extracted pure logic to `DungeonEscape.Core`.
- Extracted game save title/summary formatting, save usability checks, and return-location display-name formatting into `GameSaveFormatter`.
- Added shared core unit tests for save summary/title formatting, usable-save detection, and location-name formatting.
- Extracted store/economy metadata, buy eligibility, sellable item filtering, sale prices, initial store inventory selection, invalid store inventory checks, and buy/sell mutation rules into `StoreRules`.
- Added shared core unit tests for store metadata, key stores, buy recipients, sellable filtering, sale prices, fixed/key inventory initialization, and buy/sell behavior.
- Split file-backed data contracts out of `DungeonEscape.Core/State` into `DungeonEscape.Core/Data` with the `Redpoint.DungeonEscape.Data` namespace.
- Extracted random item generation, quest progression/rewards, and random encounter selection/repel rules into core rule classes with focused unit tests.
- Extracted combat round action choice, target resolution/fallback, run outcomes, and action execution dispatch into `CombatRoundRules` with focused unit tests.

### Good First Extractions

- Map path and Tiled helpers:
  - Done: Move map id/path normalization out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Loader.cs`.
  - Done: Move tileset/image path resolution out of `Loader.cs`.
  - Done: Move CSV tile data and Tiled GID parsing out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Collision.cs`.
  - Done: Move object-bounds-to-blocked-tile coordinate math out of `Collision.cs`.
- Save and display formatting:
  - Done: Move game save title/summary formatting out of `GameState`.
  - Done: Move return-location display-name formatting out of `GameState`.
- Store and economy rules:
  - Done: Move sellable item filtering and sale price rules out of `StoreWindow`.
  - Done: Move store metadata, initial inventory selection, invalid inventory checks, and buy/sell operation rules out of `GameState`.
- Core state/data organization:
  - Done: Move file-backed data definitions and parsed Tiled contracts out of `State`.
  - Keep save/runtime objects and shared gameplay value types in `State`.

### Medium-Sized Extractions

- Random item generation:
  - Done: Move `CreateRandomItem`, `CreateRandomEquipment`, rarity selection, stat selection, and equipment naming out of `GameState`.
  - Done: Add unit tests for generated item level ranges, rarity behavior, stat ranges, class/slot constraints, and name construction.
- Quest progression and rewards:
  - Done: Move `StartQuest`, `AdvanceQuest`, XP/gold/item rewards, and completed-stage behavior out of `GameState`.
  - Done: Add unit tests for completed quests not restarting, reward grants, stage advancement, and item rewards.
- Encounter generation:
  - Done: Move random encounter filtering, rarity weights, monster group selection, and repel filtering out of `GameState`.
  - Done: Add unit tests for biome filtering, min/max monster levels, rarity weighting, group limits, party-level scaling, and repel behavior.

### Larger Extractions

- Combat round rules:
  - Done: Move monster action choice, action resolvability, target fallback, target selection helpers, run chance, and round action execution out of `CombatWindow.RoundFlow.cs`.
  - Keep Unity combat UI responsible for rendering, input, animation, and sound orchestration.
  - Done: This should happen before adding battle tactics, because tactics need testable round/action selection rules.

### Keep Unity-Side For Now

- IMGUI drawing/layout code in `GameMenu`, `TitleMenu`, `StoreWindow`, `HealerWindow`, and `CombatWindow`.
- Unity sprite, texture, and asset loading code.
- Audio playback code.
- File IO/autosave timing and Unity lifecycle orchestration.

### Suggested Order

1. Extract map/Tiled helpers and add low-risk unit tests.
2. Extract save/location formatting and add unit tests.
3. Extract store/economy rules and add unit tests.
4. Done: Extract random item generation and add unit tests.
5. Done: Extract quest progression/reward rules and add regression tests for known quest bugs.
6. Done: Extract encounter generation and add unit tests before tuning random monsters/biomes.
7. Done: Extract combat round rules before implementing battle tactics.

## Split UI Drawing From UI Logic

Goal: reduce the size and coupling of Unity UI classes by separating drawing/layout code from state transitions, filtering, command handling, and display data preparation.

### Progress

- Started with `StoreWindow`.
- Added a plain C# `StoreViewModel` under `Redpoint.DungeonEscape.ViewModels` for store metadata, selected tab/focus/index state, selection clamping, buy eligibility, sellable filtering, and price display.
- Added unit tests for `StoreViewModel`.
- `StoreWindow` still owns IMGUI drawing, modal rendering, scroll positions, Unity input repeat timing, sounds, and direct game command execution.
- Added a plain C# `HealerViewModel` under `Redpoint.DungeonEscape.ViewModels` for healer metadata, service list construction, target filtering, costs, and selected service/target state.
- Added unit tests for `HealerViewModel`.
- `HealerWindow` still owns IMGUI drawing, Unity input repeat timing, sounds, and direct game command execution.
- Added a plain C# `TitleViewModel` under `Redpoint.DungeonEscape.ViewModels` for title mode, main-row availability, create-player choices, load/create navigation, dropdown indexes, blocked create-image selection, load-slot display rows, and load selection clamping.
- Added unit tests for `TitleViewModel`.
- `TitleMenu` still owns IMGUI drawing, background textures, audio, save/load/delete execution, random name generation, and Unity app quit/close behavior.
- Added a plain C# `GameMenuViewModel` under `Redpoint.DungeonEscape.ViewModels` for menu screen/focus/tab state, selected indexes, row/detail clamping, page-based detail selection, main action availability, member ordering/filtering, settings/save/load row counts, detail counts, equipment slots, equipped item lookup, and equipment candidate selection.
- Added unit tests for `GameMenuViewModel`.
- `GameMenu` still owns IMGUI drawing, scroll positions, modal command execution, input rebinding capture, settings persistence, and gameplay actions.
- Added a plain C# `CombatViewModel` under `Redpoint.DungeonEscape.ViewModels` for combat UI state and selected-index movement, including wraparound monster target selection.
- Added unit tests for `CombatViewModel`.
- `CombatWindow` still owns battlefield rendering, menu rendering, target click handling, message reveal timing, audio, animation flashes, and action execution orchestration.

### Unity UI And MVVM Notes

- Unity can support MVVM-style patterns, especially with UI Toolkit data binding.
- The current UI is IMGUI-based, so strict MVVM is not a natural fit because drawing and event handling happen together during `OnGUI`.
- A pragmatic fit for the current code is view models plus presenter/controller classes:
  - View models hold display-ready data, selected indexes, enabled/disabled state, labels, and validation messages.
  - Presenters/controllers translate input commands into state changes and game actions.
  - IMGUI view classes only draw controls and forward user intents.
- If the UI is later moved to UI Toolkit, the view-model layer can become a stronger MVVM boundary.

### Candidate Areas

- `GameMenu`:
  - In progress: Split menu screen state, selection clamping, action availability, member filtering, detail counts, save/load row counts, settings row counts, and equipment candidate selection away from IMGUI drawing.
  - Continue splitting item/spell/ability modal action decisions and settings adjustment decisions away from IMGUI drawing.
  - Keep IMGUI files responsible for layout and control rendering only.
- `CombatWindow`:
  - In progress: Split combat command selection state and round-flow rules away from battlefield/message rendering.
  - Continue splitting target display state and combat menu display data from rendering.
- `StoreWindow` and `HealerWindow`:
  - Done for current scope: Split available actions, prices, recipient choices, service choices, and target choices from drawing code.
- `TitleMenu`:
  - Done for current scope: Split create-player state, navigation state, and save-slot display data from drawing code.

### Suggested Steps

1. Done: Start with one small UI surface, probably `StoreWindow` or `HealerWindow`, to test the pattern.
2. In progress: Create plain C# view-model classes that do not reference `UnityEngine`.
3. In progress: Move filtering, labels, selected row clamping, and enabled-state decisions into those classes.
4. Keep IMGUI code as a thin renderer that calls view-model commands.
5. In progress: Add unit tests for view-model behavior where it does not require Unity.
6. In progress: Apply the pattern to `GameMenu` and `CombatWindow` after the smaller surface proves useful.
