# Dungeon Escape - Monster Editor

A standalone desktop tool for editing the game's monster JSON files. It is a
[Photino.Blazor](https://github.com/tryphotino/photino.Blazor) app (HTML/CSS UI
hosted in a native OS window) that references `DungeonEscape.Core`, so the JSON
it writes matches the game's data format exactly (Newtonsoft serialization,
`Spells`/`Skills` property remapping, string enums for `Rarity`/`Biomes`).

## Features

- A single **File** dropdown menu in the toolbar with **New**, **Open…**,
  **Save**, and **Save As…**.
- Remembers the last opened/saved file and **auto-loads it on startup**
  (stored in `%AppData%/DungeonEscape.MonsterEditor/settings.json`).
- Open **any** monster JSON file (an array of monsters), or create a new one.
- Searchable monster list with image thumbnails.
- **Add**, **Duplicate**, and **Remove** monsters.
- Full property editor for the selected monster:

  - Name, Image (dropdown of tileset images with live preview), Rarity.
  - Min Level, Group Size, Gold, XP.
  - Combat stats: Attack, Defence, Magic Defence, Agility.
  - Health / Magic (Const, Random, Times).
  - Biomes (multi-select).
  - Spells / Skills / Items (dropdowns sourced from the sibling
    `spells.json`, `skills.json`, and `customitems.json`, with free-text
    fallback).
- Unsaved-change tracking with a save prompt when opening/creating files.

## Monster images

Images come from the Tiled tileset `Tilesets/allmonsters.tsx` (the `ImageId`
maps to a `<tile>` whose `<image>` points at a PNG under `Images/monsters/`).

The tool auto-detects the asset root by walking up from the opened JSON file to
find a folder containing `Tilesets/allmonsters.tsx` and `Images/monsters/`. To
get thumbnails, open a monster file from within the game's asset tree, e.g.
`DungeonEscape.Unity/Assets/DungeonEscape/Data/allmonsters.json`.

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
> run on a newer installed runtime if .NET 8 is not present.
