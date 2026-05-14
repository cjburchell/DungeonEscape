# UI Toolkit Migration Plan

This file tracks the plan for migrating Dungeon Escape's Unity UI from IMGUI `OnGUI` drawing to UI Toolkit.

## Goal

Move Unity UI rendering to UI Toolkit while keeping existing core view models responsible for screen state, navigation decisions, row data, and action decisions.

Unity UI scripts should become thin adapters that:

- create or load UI Toolkit visual trees
- bind core view model data into controls
- translate keyboard/gamepad/mouse input into view model calls
- trigger existing Unity orchestration such as audio, animation, scene flow, and game state effects

## Current Starting Point

- UI drawing currently lives under `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/UI`.
- Core view models already exist under `DungeonEscape.Core/ViewModels`.
- Store, healer, title, game menu, and combat have started moving UI logic away from IMGUI drawing.
- No `.uxml` or `.uss` files currently exist for Dungeon Escape UI.
- Existing IMGUI files should remain working while UI Toolkit screens are migrated one at a time.

## Migration Principles

- Keep core view models UI-framework-agnostic.
- Do not move game rules or action decisions into UI Toolkit event handlers.
- Prefer C#-built UI Toolkit views first for complex dynamic screens; introduce UXML when the layout stabilizes.
- Use shared USS variables/classes for window chrome, row selection, disabled rows, modal overlays, status bars, and icon sizing.
- Keep keyboard/gamepad parity with the existing action/cancel/menu bindings.
- Keep manual smoke tests updated for each migrated screen.
- Leave IMGUI fallback available until a replacement screen passes manual validation.

## Proposed Folder Structure

- `DungeonEscape.Unity/Assets/DungeonEscape/UI/`
  - `Styles/`
    - shared USS files
  - `Layouts/`
    - optional UXML files for stable static layouts
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/UI/Toolkit/`
  - UI Toolkit screen adapters
  - shared controls and binding helpers
  - input routing helpers

## Phase 1: Foundation

- Add a root `UIDocument` host for runtime UI.
- Add a shared UI Toolkit theme stylesheet matching the current UI theme.
- Build reusable controls:
  - framed window
  - selectable row list
  - command button row
  - modal overlay
  - item/spell/ability row with icon
  - party member/status row
  - HP/MP/progress bar
  - gold/status summary strip
- Add an input bridge from existing `InputManager` actions to focused UI Toolkit controls.
- Add a simple screen stack/router so one UI Toolkit screen can open modals and nested screens without owning global game flow.
- Keep existing IMGUI windows active by default.

## Phase 2: Low-Risk Overlay Screens

Migrate simple, mostly presentational UI first.

- `GoldWindow`
- `PartyStatusWindow`
- `MessageBox`
- `ScreenFade`
- `SplashScreen`

Acceptance criteria:

- Visual layout matches current behavior closely enough for gameplay.
- Keyboard/gamepad confirm/cancel behavior is unchanged.
- Map, dialog, and combat overlays still coordinate correctly.
- IMGUI fallback can be removed for each completed screen.

## Phase 3: Service Windows

Migrate screens that already have focused view models and limited surface area.

- `StoreWindow`
- `HealerWindow`

Acceptance criteria:

- Buy/sell/heal service availability matches core view model output.
- Selection clamping and disabled row behavior match current behavior.
- Gold and inventory updates render immediately after actions.
- Mouse clicks, keyboard, and gamepad all route through the same view model decisions.

## Phase 4: Title Flow

Migrate `TitleMenu`.

Acceptance criteria:

- Splash-to-title flow remains unchanged.
- Continue/New Quest/Load Quest/Quit navigation works with keyboard and gamepad.
- Create-player dropdowns, stat reroll, portrait/class/gender changes, and random names match current behavior.
- Load/delete save rows match `TitleViewModel` display data.

## Phase 5: Game Menu

Migrate `GameMenu` after service windows and title flow prove the shared controls.

Target areas:

- top-level action list
- item/spell/ability/equipment/status/party/quest screens
- save/load/settings screens
- item/spell/ability/party/settings modals

Acceptance criteria:

- Existing `GameMenuViewModel` remains the source for screen state, visible actions, counts, labels, and modal decisions.
- Page state, selected member, selected item/spell/ability, and settings adjustments match current behavior.
- Long item/spell/ability names fit at supported UI scale values.
- Existing manual game-menu smoke test passes.

## Phase 6: Combat UI

Migrate `CombatWindow` last because it has the most orchestration: rendering, target hit regions, round messages, action menus, combat audio, and battle pacing.

Target areas:

- message window
- action menu
- spell/item/skill lists
- target highlight/display state
- party target selection over the status panel
- victory/defeat/reward messages

Acceptance criteria:

- `CombatViewModel` remains the source for combat menu display data and target display state.
- Existing combat round rules remain outside rendering code.
- Monster battlefield rendering remains coordinated with combat target selection.
- Run, victory, defeat, reward, and close-combat flows match current behavior.

## Phase 7: Cleanup

- Remove replaced IMGUI drawing files once their UI Toolkit equivalents pass validation.
- Remove obsolete `GUIStyle`, layout helper, and IMGUI-only control code.
- Consolidate duplicated UI constants into USS or shared theme bindings.
- Update memory-bank docs and manual test plans.
- Add Unity edit/play mode tests for high-risk UI routing where practical.

## Risks

- UI Toolkit focus and gamepad navigation may not match current custom IMGUI navigation without an explicit input bridge.
- Dynamic combat/menu layouts may be easier to build in C# than UXML.
- Current UI scale/theme settings need a deliberate mapping to USS variables or runtime style updates.
- Mixed IMGUI and UI Toolkit overlays can create ordering/focus bugs during the transition.
- Combat target selection may require special handling if battlefield sprites remain normal Unity renderers while menus move to UI Toolkit.

## First Implementation Slice

The recommended first slice is:

- add the UI Toolkit runtime host
- add shared USS/theme plumbing
- migrate `GoldWindow` and `MessageBox`
- keep IMGUI fallback behind a simple feature flag or component toggle
- manually validate map play, NPC dialog, and combat message overlays

