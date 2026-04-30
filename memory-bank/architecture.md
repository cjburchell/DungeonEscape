# Architecture

## Repository Shape

Dungeon Escape is being migrated from the old MonoGame/Nez project into a Unity project while preserving portable game/domain logic.

- `DungeonEscape.Unity/` is the active Unity project.
- `DungeonEscape.Core/` is the shared portable C# domain layer.
- `DungeonEscape.Core.Test/` contains shared core regression tests.
- `DungeonEscape/` is the old MonoGame project and should not be changed during Unity migration unless explicitly requested.
- `memory-bank/` stores durable migration context for human or agent handoff.

## Runtime Layers

### Shared Core

`DungeonEscape.Core` targets `netstandard2.0` for Unity compatibility. It owns portable state and domain models such as party, heroes, items, quests, dialogs, map object state, settings, dice, and Tiled metadata DTOs.

Important constraints:

- Do not reference Unity, MonoGame, or Nez from this project.
- Keep language/runtime choices Unity-friendly.
- Put gameplay rules here when they are engine-neutral.

### Unity Runtime

`DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity` owns Unity-specific application behavior:

- TMX/TSX loading and rendering.
- Map movement, collision, warps, water/ship behavior, damage/biome layers.
- Player, follower, cart, coffin, NPC animation, and sorting behavior.
- Unity UI/menu/store/message/title flow.
- Save/load/autosave policy and Unity file/runtime path handling.

Important scripts:

- `TiledMapLoader.cs`: loads TMX maps and TSX tilesets.
- `TiledMapRenderer.cs`: renders TMX layers and map objects.
- `TiledMapView.cs`: owns current rendered map view and refreshes.
- `TiledMapCollision.cs`: tile/object collision queries.
- `PlayerGridController.cs`: movement, facing, interaction, warps, followers.
- `DungeonEscapeGameState.cs`: Unity-facing game state, party, object state, quests, shops, saves.
- `DungeonEscapeGameMenu.cs`: IMGUI party/inventory/quest/settings/save UI.
- `DungeonEscapeStoreWindow.cs`: tabbed buy/sell store UI.
- `DungeonEscapeMessageBox.cs`: modal map dialogs.
- `DungeonEscapeTitleMenu.cs`: title/continue/new/load/quit flow.

## Data And Assets

Unity data lives under:

- `DungeonEscape.Unity/Assets/DungeonEscape/Data`
- `DungeonEscape.Unity/Assets/DungeonEscape/Maps`
- `DungeonEscape.Unity/Assets/DungeonEscape/Tilesets`
- `DungeonEscape.Unity/Assets/DungeonEscape/Images`
- `DungeonEscape.Unity/Assets/DungeonEscape/Audio`

Builds stage runtime files into `StreamingAssets` so TMX, TSX, image, and data files load outside the editor.

The active Unity maps use Tiled object `class="..."` only. Do not reintroduce object `type="..."`.

Current map object conventions:

- Chests: `class="Chest"`, `Locked=false` unless deliberately locked.
- Doors: `class="Door"`, `Locked=true` unless deliberately unlocked.
- Warps: `class="Warp"` with `WarpMap` and optional `SpawnId`.
- Spawn points: `class="Spawn"` with `DefaultSpawn=true` where a map needs a default spawn.
- Hidden items: `class="HiddenItem"` with quest/item metadata as needed.
- NPCs/services use class values such as `Npc`, `NpcHeal`, `NpcStore`, `NpcSave`, `NpcKey`, and `NpcPartyMember`.

## Save And Persistence

Save data is Unity-side and uses the migrated shared state models. Unsupported future/old save versions are archived and ignored instead of trying to migrate old MonoGame save formats.

Autosave policy:

- Timer autosave can be enabled/disabled in settings.
- Autosave is skipped while title/menu/store/dialog UI is active.
- Combat can block autosave through `DungeonEscapeGameState.AutoSaveBlocked`.

Transition save policy:

- Save only when moving to or from the overworld.
- Other map transitions do not force a save.

## UI Architecture

The Unity UI is currently IMGUI-based. It uses reusable style/control helpers:

- `DungeonEscapeUiTheme`
- `DungeonEscapeUiSettings`
- `DungeonEscapeUiControls`
- `DungeonEscapeUiAssetResolver`

UI scale and colors are settings-driven. Avoid hard-coded UI colors or ad hoc controls when an existing helper exists.

Menu/modal behavior:

- Game menu, store window, message boxes, and title menu should block unrelated controls while active.
- Menu action dialogs should use overlay/modal behavior so the menu does not disappear behind prompts.
- Gamepad navigation must remain usable.

## Input

Input supports keyboard and gamepad bindings through the Unity input layer. Current defaults include WASD movement, a consolidated Menu action, sprint, interact, cancel, and gamepad navigation.

Settings UI has an Input Bindings tab. Rebinding is popup-based and blocks unrelated menu controls while active.

## Build And CI

GitLab CI restores/builds/tests the solution and can validate/build Unity using Unity CI images.

Top-level CI variables include:

- `UNITY_CI_ENABLED`
- `UNITY_BUILD_WINDOWS_ENABLED`

Unity license can be supplied through GitLab variables using a license file/content variable or serial/email/password activation.

Windows Unity builds are stored as downloadable artifacts when enabled.
