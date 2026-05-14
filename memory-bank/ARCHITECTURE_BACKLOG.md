# Architecture Backlog

This file tracks architecture cleanup ideas that are not active work yet.

## Extract Unity Logic Into Core

Goal: separate engine-neutral rules from Unity scripts into `DungeonEscape.Core`, then add focused unit tests around the extracted logic.

### Completed Extractions

- Extracted map path normalization, tileset path resolution, Tiled CSV/GID parsing, boolean parsing, and object-bounds tile index math into core helpers.
- Added shared core unit tests for the extracted Tiled helper behavior.
- Unity `Loader` and `Collision` still expose their existing APIs, but delegate the extracted pure logic to `DungeonEscape.Core`.

### Good First Extractions

- Map path and Tiled helpers:
  - Move map id/path normalization out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Loader.cs`.
  - Move tileset/image path resolution out of `Loader.cs`.
  - Move CSV tile data and Tiled GID parsing out of `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Map/Tiled/Collision.cs`.
  - Move object-bounds-to-blocked-tile coordinate math out of `Collision.cs`.
- Save and display formatting:
  - Move game save title/summary formatting out of `GameState`.
  - Move return-location display-name formatting out of `GameState`.
- Store and economy rules:
  - Move sellable item filtering and sale price rules out of `StoreWindow`.
  - Move store inventory generation and buy/sell operation rules out of `GameState`.

### Medium-Sized Extractions

- Random item generation:
  - Move `CreateRandomItem`, `CreateRandomEquipment`, rarity selection, stat selection, and equipment naming out of `GameState`.
  - Add unit tests for generated item level ranges, rarity behavior, stat ranges, class/slot constraints, and name construction.
- Quest progression and rewards:
  - Move `StartQuest`, `AdvanceQuest`, XP/gold/item rewards, and completed-stage behavior out of `GameState`.
  - Add unit tests for completed quests not restarting, reward grants, stage advancement, and item rewards.
- Encounter generation:
  - Move random encounter filtering, rarity weights, monster group selection, and repel filtering out of `GameState`.
  - Add unit tests for biome filtering, min/max monster levels, rarity weighting, group limits, party-level scaling, and repel behavior.

### Larger Extractions

- Combat round rules:
  - Move monster action choice, action resolvability, target fallback, target selection helpers, run chance, and round action execution out of `CombatWindow.RoundFlow.cs`.
  - Keep Unity combat UI responsible for rendering, input, animation, and sound orchestration.
  - This should happen before adding battle tactics, because tactics need testable round/action selection rules.

### Keep Unity-Side For Now

- IMGUI drawing/layout code in `GameMenu`, `TitleMenu`, `StoreWindow`, `HealerWindow`, and `CombatWindow`.
- Unity sprite, texture, and asset loading code.
- Audio playback code.
- File IO/autosave timing and Unity lifecycle orchestration.

### Suggested Order

1. Extract map/Tiled helpers and add low-risk unit tests.
2. Extract save/location formatting and add unit tests.
3. Extract store/economy rules and add unit tests.
4. Extract random item generation and add unit tests.
5. Extract quest progression/reward rules and add regression tests for known quest bugs.
6. Extract encounter generation and add unit tests before tuning random monsters/biomes.
7. Extract combat round rules before implementing battle tactics.

## Split UI Drawing From UI Logic

Goal: reduce the size and coupling of Unity UI classes by separating drawing/layout code from state transitions, filtering, command handling, and display data preparation.

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
  - Split menu screen state, action availability, item/spell/ability filtering, and modal state away from IMGUI drawing.
  - Keep IMGUI files responsible for layout and control rendering only.
- `CombatWindow`:
  - Split combat command selection, target selection state, and round-flow display state away from battlefield/message rendering.
  - Pair this with the larger combat round-rules extraction before adding battle tactics.
- `StoreWindow` and `HealerWindow`:
  - Split available actions, prices, recipient choices, and modal state from drawing code.
- `TitleMenu`:
  - Split create-player state, save-slot display data, and navigation state from drawing code.

### Suggested Steps

1. Start with one small UI surface, probably `StoreWindow` or `HealerWindow`, to test the pattern.
2. Create plain C# view-model classes that do not reference `UnityEngine`.
3. Move filtering, labels, selected row clamping, and enabled-state decisions into those classes.
4. Keep IMGUI code as a thin renderer that calls view-model commands.
5. Add unit tests for view-model behavior where it does not require Unity.
6. Apply the pattern to `GameMenu` and `CombatWindow` after the smaller surface proves useful.
