# Dungeon Escape - Data Editor

A standalone desktop tool for editing the game's data JSON files. It is a
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

The last opened folder is remembered and **auto-loaded on startup** (stored in
`%AppData%/DungeonEscape.MonsterEditor/settings.json`).

## Features

- Tabs for **Monsters**, **Spells**, **Skills**, **Items**, **Item Definitions**,
  **Quests**, **Dialogs**, **Class**, **Stat Names**, and **Names**. Array-backed tabs include a
  searchable list and **Add**, **Duplicate**, and **Remove** actions; `names.json`
  is edited as a single document.
- A collapsible **Validation** panel flags duplicate identifiers, empty required
  names/IDs, broken spell/skill/item/quest/monster references, invalid image IDs,
  missing class-level definitions, and dialog nesting issues.
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
    growth rows with initial roll previews, and skill unlock references.
    Spell/item/item-definition class selectors are populated from
    `classlevels.json`.
  - **Stat Names** &mdash; fixed stat rows plus prefix/suffix word pools.
  - **Names** &mdash; male and female character name pools.

## Images

- **Monster** images come from `Tilesets/allmonsters.tsx` (each `ImageId` maps
  to a `<tile>` whose `<image>` points at a PNG under `Images/monsters/`).
- **Item** images come from the grid tileset `Tilesets/items2.tsx`.
- **Spell** images come from the grid tileset `Tilesets/items.tsx`.

The tool auto-detects the asset root by walking up from the opened Data folder
to find a folder containing `Tilesets/allmonsters.tsx` and `Images/monsters/`,
then resolves the item/spell tilesets and their PNGs relative to that root.

## Running

```sh
dotnet run --project DungeonEscape.Tools.MonsterEditor
```

## Publishing a single executable

```sh
dotnet publish DungeonEscape.Tools.MonsterEditor -c Release -r win-x64 ^
  -p:PublishSingleFile=true --self-contained true
```

The resulting `DungeonEscape.Tools.MonsterEditor.exe` (under
`bin/Release/net8.0/win-x64/publish/`) can be launched directly.

> Note: the project targets `net8.0` with `RollForward=LatestMajor`, so it will
> run on a newer installed runtime if .NET 8 is not present. Item/spell tile
> cropping uses `System.Drawing.Common`, so this tool is Windows-only.
