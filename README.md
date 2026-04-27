# Dungeon Escape

Dungeon Escape is a retro-inspired 2D RPG (in the spirit of classic SNES-era Dragon Quest / Final Fantasy). It is an updated version of an older project (DE15) and is built with **MonoGame** + the **Nez** 2D framework.

The game is largely **data-driven**: quests, dialogs, classes, skills, items, spells, and monsters are defined in JSON under `DungeonEscape/Content/data/`. Maps are authored in **Tiled** (`.tmx/.tsx`) and loaded at runtime.

## Download / Play

- Download the latest Windows build (GitLab artifacts):
  - https://gitlab.com/cjburchell/DungeonEscape/-/jobs/4012464295/artifacts/download

### Saves & settings location

The game stores saves/settings in your AppData folder:

```text
%AppData%\Redpoint\DungeonEscape\
  save.json
  settings.json
```

## Controls

Exploration controls are handled in `PlayerComponent`.

- **Move**: Arrow keys / WASD / Gamepad left stick / D-pad
- **Action / Interact**: Space / Gamepad A

## Build & Run (Developers)

### Prerequisites

- **.NET SDK 7.0** (CI builds with 7.0)
- A MonoGame-compatible runtime environment (primarily tested on Windows)

### Build

```bash
dotnet restore
dotnet build
```

### Run

```bash
dotnet run --project DungeonEscape/DungeonEscape.csproj
```

### Test

```bash
dotnet test
```

## Architecture

### High-level runtime flow

```text
Program.cs
  └─ Game (Nez.Core, implements IGame)
       ├─ loads Settings + Content/data/*.json (quests, items, monsters, etc.)
       ├─ manages Party + MapStates + Save/Load
       └─ scene transitions
            SplashScreen → MainMenu
                         → CreatePlayerScene / ContinueQuestScene
                         → MapScene (exploration)
                         → FightScene (turn-based combat)
```

### Key concepts & responsibilities

- **`Game` (`DungeonEscape/DungeonEscapeGame.cs`)**
  - Owns global game state and services via `IGame` (party, settings, content lists, sounds, save/load).
  - Loads data-driven gameplay definitions from `Content/data/*.json`.
  - Loads Tiled maps from `Content/maps/*.tmx`.
  - Creates and transitions between scenes (fade/transform transitions).

- **Scenes (`DungeonEscape/Scenes/*`)**
  - `SplashScreen` → shows the splash image and transitions to the main menu.
  - `MainMenu` → “New game / Load game / Settings / Quit”.
  - `MapScene` → exploration: renders the Tiled map layers, spawns the player party, loads map objects/sprites, runs interactions and random encounters.
  - `FightScene` → turn-based combat with UI windows (select action, target selection, damage resolution, rewards).

- **Domain model / State (`DungeonEscape/State/*`)**
  - Serializable game state (party members, quests, items, map/object state).
  - `GameSave` is persisted to JSON and loaded on startup.

- **Content (`DungeonEscape/Content/*`)**
  - `Content/data/*.json` → gameplay definitions (quests/dialog/items/skills/spells/monsters/names/etc.).
  - `Content/maps/*.tmx` and tilesets (`.tsx`) → maps and map metadata (properties like `overworld`, `song`, `biome`).
  - `Content/images`, `Content/sound`, `Content/fonts` → assets.

### Project layout (key paths)

```text
DungeonEscape/
  Program.cs                     # entry point
  DungeonEscapeGame.cs            # Game : Nez.Core, IGame implementation
  Scenes/                         # game flow, UI screens, exploration, combat
  State/                          # domain entities + save data structures
  Content/                        # images, audio, maps (Tiled), JSON data
Nez.Portable/                     # vendored Nez framework (portable build)
DungeonEscape.Test/               # unit tests
```

## TODO

### Features

- Escort quests
- Kill monster quests
- Create more quests
- Improved inventory window

### Improvements

- Balance pass
 

 