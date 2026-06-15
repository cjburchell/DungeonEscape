# Dungeon Escape - Game Editor

A standalone desktop tool for editing the game's data JSON files and gameplay
metadata stored on TMX map objects. It is a
[Photino.Blazor](https://github.com/tryphotino/photino.Blazor) app (HTML/CSS UI
hosted in a native OS window) that references `DungeonEscape.Core`, so the JSON
it writes matches the game's data format exactly (Newtonsoft serialization,
`Spells`/`Skills` property remapping, string enums for `Rarity`/`Biomes`/etc.).

## Loading

The editor works on a whole **Data folder** rather than a single file. Use
**File &rarr; Open Folder…** and pick the game's data directory, e.g.

```
DungeonEscape.Unity/Assets/DungeonEscape/Data
```

It loads the following files into one shared, in-memory dataset:

| File              | Edited on tab |
|-------------------|---------------|
| `allmonsters.json`| Monsters      |
| `spells.json`     | Spells        |
| `skills.json`     | Skills        |
| `customitems.json`| Items         |
| `itemdef.json`    | Item Definitions |
| `quests.json`     | Quests        |
| `dialog.json`     | Dialogs       |
| `statnames.json`  | Stat Names    |
| `classlevels.json`| Class         |
| `names.json`      | Names         |
| `maps/**/*_monsters.json` | Maps random monsters / Encounter Simulator |

After the Data folder is opened, the tool also auto-detects the neighboring
`Maps` folder and loads `DungeonEscape.Unity/Assets/DungeonEscape/Maps/**/*.tmx`
for the **Maps** tab. TMX layout/display data remains owned by Tiled; the editor
only exposes gameplay metadata.

The last opened folder is remembered and **auto-loaded on startup** (stored in
`%AppData%/DungeonEscape.GameEditor/settings.json`).

## Features

- Tabs for **Monsters**, **Spells**, **Skills**, **Items**, **Item Definitions**,
  **Quests**, **Dialogs**, **Class**, **Stat Names**, **Names**, **Maps**,
  **Quest Graph**, and **Encounter Simulator**. Array-backed data tabs include a
  searchable list and **Add**, **Duplicate**, and **Remove** actions; `names.json`
  is edited as a single document, and maps are discovered from the Unity Maps
  folder rather than added or removed in this editor. The graph and simulator
  tabs are read-only analysis tools and do not mark the project dirty.
- A collapsible **Validation** panel flags duplicate identifiers, empty required
  names/IDs, broken spell/skill/item/quest/monster references, invalid image IDs,
  missing class-level definitions, dialog nesting issues, broken map object
  references, duplicate TMX object ids, missing explicit chest/door lock
  metadata, invalid map biome values, missing map music files, generated item
  definition image IDs, missing tileset source images, and missing monster PNGs.
- All lists share a single in-memory dataset, so cross-references update
  **live** &mdash; e.g. add a new item on the Items tab and it immediately
  appears in a monster's drop list and any item dropdown; rename a skill and the
  Spell/Item skill dropdowns update without reloading.
- **File &rarr; Save Project** writes every data file back at once; a single
  unsaved-changes indicator drives the save prompt. Saved JSON is pruned so
  null/default values, zeroes, `false`, empty strings, empty arrays, and empty
  objects are omitted.
- Per-entity property editors:
  - **Monster** &mdash; image (with preview), rarity, levels, combat stats,
    health/magic rolls, biomes, and Spell/Skill/Item references.
  - **Spell** &mdash; image (with preview), skill reference, cost, min level,
    classes.
  - **Skill** &mdash; type/targets/stat type/duration type, max targets,
    piercing, do-attack, effect name, stat & duration rolls.
  - **Item** &mdash; image (with preview), type, rarity, target, cost, min
    level, charges, skill reference, stat values, slots, classes, and quest
    fields.
  - **Item Definition** &mdash; procedural item type/base stat, equip slots,
    allowed classes, and generated name/image options.
  - **Quest** &mdash; id/name/description, minimum level, XP/gold rewards, reward
    item references, and quest stages.
  - **Dialog** &mdash; dialog ids, quest-conditioned heads, text, choices, quest
    actions, item/monster/map references, and nested response dialogs.
  - **Class** &mdash; free-text class name, first-level XP threshold, fixed stat
    growth rows with initial roll previews, default hero-sheet image, and skill
    unlock references. Spell/item/item-definition class selectors are populated
    from `classlevels.json`.
  - **Stat Names** &mdash; fixed stat rows plus prefix/suffix word pools.
  - **Names** &mdash; male and female character name pools.
  - **Maps** &mdash; map class/properties, object `name`/`class`, NPC/chest/door/warp
    gameplay properties, and per-map random monster encounter sets. Unsupported
    raw TMX property add/remove editing is intentionally hidden so Tiled remains
    responsible for layout/display metadata and the editor only exposes supported
    gameplay fields.
  - **Quest Graph** &mdash; read-only relationship cards for quests, stages, dialog
    heads, choices, nested responses, quest-stage transitions, item rewards,
    fights, object actions, and warps.
  - **Encounter Simulator** &mdash; read-only random encounter simulation for
    overworld biome encounters and per-map random monster tables using the same
    `EncounterRules` weighting as the game.

## JSON schemas

The tool project includes JSON Schemas under `Schemas/` for the Data files it
edits. These schemas document file shape and enum values for editor tooling or
future CI checks:

```text
DungeonEscape.Tools.GameEditor/Schemas/
```

Cross-file reference checks, asset existence checks, and map metadata rules are
still handled by the live Validation panel because those checks need the opened
project's full in-memory dataset.

## Map editing

The **Maps** tab is for gameplay metadata only. Use Tiled for actual map layout,
tiles, object placement, object size, and sprite/tile display. The editor shows
TMX placement fields such as object id, `gid`, `x`, `y`, `width`, and `height`
as read-only context.

Editable map/object metadata includes:

- Map-level `class` and properties such as `biome` and `song`.
- NPC/service NPC fields such as `Text`, `Dialog`, `MoveRadius`, `Collideable`,
  `Direction`/`Facing`, and party-member `Class`, `Gender`, and `Level`.
- Chest/hidden-item fields such as `ItemId`, `Gold`, `Locked`, `Level`,
  `ChestLevel`, `KeyId`, `OpenWithKey`, and `Collideable`.
- Door fields such as `Locked`, `DoorLevel`, `KeyId`, `OpenWithKey`, and
  `Collideable`.
- Warp/spawn fields such as `WarpMap`, `SpawnId`, and `DefaultSpawn`.

Unsupported raw property add/remove controls are intentionally hidden from the
Maps tab. If new map metadata rules are added, expose them through a supported
typed form field and validation rule instead of asking users to edit arbitrary
TMX properties in the Game Editor.

Random encounter sets are saved as JSON files using the runtime-supported path:

```text
DungeonEscape.Unity/Assets/DungeonEscape/Data/maps/{mapId}_monsters.json
```

For example, map id `shrine/first` saves to
`Data/maps/shrine/first_monsters.json`. These files are created only when a
non-overworld map's random monster list is edited. The overworld is special: its
encounters are generated from monster biome lists, so the Maps tab shows that as
read-only guidance.

Only maps changed through the Maps tab are written back on **Save Project**, so
normal JSON-only edits do not rewrite every TMX file.

## Images

- **Monster** images come from `Tilesets/allmonsters.tsx` (each `ImageId` maps
  to a `<tile>` whose `<image>` points at a PNG under `Images/monsters/`).
- **Item** images come from the grid tileset `Tilesets/items2.tsx`.
- **Spell** images come from the grid tileset `Tilesets/items.tsx`.

The tool auto-detects the asset root by walking up from the opened Data folder
to find a folder containing `Tilesets/allmonsters.tsx` and `Images/monsters/`,
then resolves the item/spell tilesets and their PNGs relative to that root.
Image previews use local file URIs or temporary cached PNG files instead of
large base64 `data:` URLs so the Photino/Blazor output window stays readable.

## Audio

Map `song` values are selected from `.ogg` files in:

```text
DungeonEscape.Unity/Assets/DungeonEscape/Audio/music
```

The saved value is the file name without extension. Existing custom or missing
song values are preserved as a temporary dropdown option and still produce a
validation warning until the matching `.ogg` exists.

## Running

```sh
dotnet run --project DungeonEscape.Tools.GameEditor
```

## Publishing a single executable

```sh
dotnet publish DungeonEscape.Tools.GameEditor -c Release -r win-x64 ^
  -p:PublishSingleFile=true --self-contained true
```

The resulting `DungeonEscape.Tools.GameEditor.exe` (under
`bin/Release/net8.0/win-x64/publish/`) can be launched directly.

> Note: the project targets `net8.0` with `RollForward=LatestMajor`, so it will
> run on a newer installed runtime if .NET 8 is not present. Item/spell tile
> cropping uses `System.Drawing.Common`, so this tool is Windows-only.
