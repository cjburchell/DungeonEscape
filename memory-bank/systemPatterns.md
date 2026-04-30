# System Patterns

## Shared-Core First

Engine-neutral rules belong in `DungeonEscape.Core`. Unity should call into the shared models rather than duplicating rules when possible.

Use Unity-specific code only for:

- Unity scene/runtime integration.
- Rendering.
- Input.
- IMGUI/UI.
- Asset/file loading.
- GameObject behavior.

## Map Object State

TMX objects define initial map data. Runtime state is persisted in object state records so doors, chests, hidden items, NPC recruitment, and moved NPC positions survive map reload/save/load.

General pattern:

1. Read TMX object metadata through `TiledMapInfo`.
2. Initialize or retrieve persisted object state through `DungeonEscapeGameState`.
3. Apply behavior.
4. Refresh object state/rendering through `TiledMapView` or `PlayerGridController`.

## Interaction Pattern

Player interaction is facing-direction based:

- `PlayerGridController` resolves the facing tile/object.
- Service NPCs, doors, dialogs, hidden items, chests, and recruitable NPCs are dispatched by object class.
- `Open` spell/items use the same facing-object path and do not use a party target picker.

## TMX Metadata Pattern

Use Tiled object `class`, not object `type`.

Examples:

- `class="Chest"`
- `class="Door"`
- `class="Warp"`
- `class="Npc"`
- `class="HiddenItem"`

Use properties for behavior:

- `Locked`
- `Collideable`
- `MoveRadius`
- `ItemId`
- `WarpMap`
- `SpawnId`
- `DefaultSpawn`
- `Dialog`
- `Text`

## UI Pattern

Prefer the existing IMGUI helper layer:

- `DungeonEscapeUiControls` for reusable controls.
- `DungeonEscapeUiTheme` for colors/styles.
- `DungeonEscapeUiSettings` for scale and settings-driven UI values.

Keep modals on top and block unrelated controls while active.

## Testing Pattern

Use automated tests for shared core behavior and manual tests for Unity map-mode behavior until Unity edit/play mode tests are added.

Before closing a task, run where practical:

- `dotnet build DungeonEscape.sln`
- `dotnet test DungeonEscape.sln --no-restore`
- `git diff --check`

Add manual test steps to `memory-bank/MANUAL_TESTS.md` when behavior changes.

