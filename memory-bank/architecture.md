# Architecture

## Repository Shape

Dungeon Escape now runs from the Unity project, with portable game/domain logic preserved in shared C# code.

- `DungeonEscape.Unity/` is the active Unity project.
- `DungeonEscape.Core/` is the shared portable C# domain layer.
- `DungeonEscape.Core.Test/` contains shared core regression tests.
- `memory-bank/` stores durable project context for human or agent handoff.

The old MonoGame/Nez project was removed from this branch. Use `main` if old implementation reference is needed.

## Runtime Layers

### Shared Core

`DungeonEscape.Core` targets `netstandard2.0` for Unity compatibility. It owns portable state and domain models such as party, heroes, items, quests, dialogs, map object state, settings, dice, and Tiled metadata DTOs.

Important constraints:

- Do not reference Unity, MonoGame, or Nez from this project.
- Keep language/runtime choices Unity-friendly.
- Put gameplay rules here when they are engine-neutral.

### Unity Runtime

`DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity` owns Unity-specific application behavior and is split by responsibility:

- `Core/` namespace `Redpoint.DungeonEscape.Unity.Core`: Unity bootstrap, game state, data/settings caches, audio, input, display settings, JSON/path helpers.
- `Map/` namespace `Redpoint.DungeonEscape.Unity.Map`: player movement, warps, followers, cart/coffin, and high-level map interaction behavior.
- `Rendering/` namespace `Redpoint.DungeonEscape.Unity.Rendering`: directional sprite helpers.
- `Map/Tiled/` namespace `Redpoint.DungeonEscape.Unity.Map.Tiled`: TMX/TSX map loading, collision, rendering, view/viewport, NPC object rendering, Tiled sprite animation, and renderer pooling.
- `UI/` namespace `Redpoint.DungeonEscape.Unity.UI`: IMGUI menus, message boxes, title/splash flow, store/combat/status windows, reusable UI theme/control helpers.

Important scripts:

- `Map/Tiled/Loader.cs`: loads TMX maps and TSX tilesets.
- `Map/Tiled/Renderer.cs`: renders TMX layers and map objects.
- `Map/Tiled/View.cs`: owns current rendered map view and refreshes.
- `Map/Tiled/Collision.cs`: tile/object collision queries.
- `Map/PlayerGridController.cs`: movement, facing, interaction, warps, followers.
- `Core/GameState.cs`: Unity-facing game state, party, object state, quests, shops, saves.
- `Rendering/HeroSpriteResolver.cs`: resolves saved hero sprite identity into directional sprites for UI portraits, the player map sprite, and followers.
- `UI/GameMenu.cs`: IMGUI party/inventory/quest/settings/save UI.
- `UI/StoreWindow.cs`: tabbed buy/sell store UI.
- `UI/MessageBox.cs`: modal map dialogs.
- `UI/TitleMenu.cs`: title/continue/new/load/quit flow.

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
- `NpcPartyMember` map objects should have a sprite `gid`; recruited heroes persist the source tileset and local tile id from that referenced map sprite.

## Save And Persistence

Save data is Unity-side and uses the migrated shared state models. Unsupported future/old save versions are archived and ignored instead of trying to migrate old MonoGame save formats.

Hero visual identity is persisted on `Hero` separately from class/gender:

- Startup-created heroes can store a hero-sheet `SpriteFrameIndex`.
- Recruited NPC party members can store `SpriteTilesetPath` and `SpriteTileId` resolved from the map object's `gid`.
- Class and gender still drive gameplay stats/name generation/default fallback sprite choice, but should not be treated as the authoritative visual once explicit sprite fields exist.

Autosave policy:

- Timer autosave can be enabled/disabled in settings.
- Autosave is skipped while title/menu/store/dialog UI is active.
- Combat can block autosave through `GameState.AutoSaveBlocked`.

Transition save policy:

- Save only when moving to or from the overworld.
- Other map transitions do not force a save.

## UI Architecture

The Unity UI is currently IMGUI-based. It uses reusable style/control helpers:

- `UiTheme`
- `UiSettings`
- `UiControls`
- `UiAssetResolver`

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
