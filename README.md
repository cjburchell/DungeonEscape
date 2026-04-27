# Dungeon Escape

Dungeon Escape is a retro-inspired 2D RPG in the spirit of classic SNES-era Dragon Quest and Final Fantasy games. It is an updated version of an older project, DE15, built with **MonoGame** and the **Nez** 2D framework.

The game is largely data-driven. Quests, dialog, classes, skills, items, spells, monsters, map encounters, and default settings are defined in JSON under `DungeonEscape/Content/data/`. Maps are authored in **Tiled** (`.tmx` and `.tsx`) and loaded at runtime.

## Download / Play

Download the latest Windows build from GitLab artifacts:

https://gitlab.com/cjburchell/DungeonEscape/-/jobs/4012464295/artifacts/download

## Saves & Settings

The game stores saves and settings in:

```text
%AppData%\Redpoint\DungeonEscape\
  save.json
  settings.json
```

## Controls

Exploration controls are handled by `PlayerComponent`.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | Arrow keys or WASD | Left stick or D-pad |
| Interact / confirm | Space | A |

## Build & Run

### Prerequisites

- .NET SDK 10.0
- A MonoGame-compatible desktop runtime environment
- Windows is the primary tested platform

### Restore and Build

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

### High-level Runtime Flow

```text
Program.cs
  -> Game (Nez.Core, implements IGame)
       -> loads Settings and Content/data/*.json
       -> manages Party, MapStates, and Save/Load
       -> handles scene transitions
            SplashScreen
              -> MainMenu
                   -> CreatePlayerScene
                   -> ContinueQuestScene
                   -> MapScene
                   -> FightScene
```

### Key Concepts

**`Game` (`DungeonEscape/DungeonEscapeGame.cs`)**

Owns global game state and services through `IGame`, including party state, settings, content lists, sounds, save/load, map loading, and scene transitions.

**Scenes (`DungeonEscape/Scenes/*`)**

- `SplashScreen`: shows the splash image and transitions to the main menu.
- `MainMenu`: exposes New Game, Load Game, Settings, and Quit.
- `MapScene`: handles exploration, Tiled map rendering, object and sprite loading, interactions, and random encounters.
- `FightScene`: handles turn-based combat, action selection, target selection, damage resolution, and rewards.

**State (`DungeonEscape/State/*`)**

Contains serializable game state such as party members, quests, items, map/object state, and `GameSave`.

**Content (`DungeonEscape/Content/*`)**

- `data/*.json`: gameplay definitions, including quests, dialog, items, skills, spells, monsters, names, stat names, and default settings.
- `data/maps/**/*.json`: map-specific monster encounter data.
- `maps/**/*.tmx` and tilesets (`.tsx`): Tiled maps and map metadata such as `overworld`, `song`, and `biome`.
- `images`, `sound`, and `fonts`: game assets.

## Project Layout

```text
DungeonEscape/
  Program.cs                      # entry point
  DungeonEscapeGame.cs             # Game : Nez.Core, IGame implementation
  Scenes/                          # game flow, UI screens, exploration, combat
  State/                           # domain entities and save data structures
  Content/                         # images, audio, maps, Tiled tilesets, JSON data
Nez.Portable/                      # vendored Nez framework portable build
DungeonEscape.Test/                # xUnit tests
```

## Roadmap

### Features

- Escort quests
- Kill monster quests
- More quests
- Improved inventory window

### Improvements

- Balance pass
