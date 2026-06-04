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

The last opened folder is remembered and **auto-loaded on startup** (stored in
`%AppData%/DungeonEscape.MonsterEditor/settings.json`).

## Features

- Tabs for **Monsters**, **Spells**, **Skills**, and **Items**, each with a
  searchable list and **Add**, **Duplicate**, and **Remove** actions.
- All four lists share a single in-memory dataset, so cross-references update
  **live** &mdash; e.g. add a new item on the Items tab and it immediately
  appears in a monster's drop list and any item dropdown; rename a skill and the
  Spell/Item skill dropdowns update without reloading.
- **File &rarr; Save Project** writes every data file back at once; a single
  unsaved-changes indicator drives the save prompt.
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
