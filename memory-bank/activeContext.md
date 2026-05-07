# Active Context

## Current Focus

The migration is past the first playable Unity map-mode loop. Recent work focused on map object interaction parity:

- `Open` spell/items act on the object the player is facing.
- `Open` does not ask for a party-member target.
- Doors and chests use a shared facing-object open path.
- Chests support `Locked=true`; current chests are explicitly `Locked=false`.
- Doors are explicitly `Locked=true` unless intended to be unlocked.
- Unity TMX object metadata is normalized to `class="..."`; object `type="..."` should not be used.
- Startup now shows the old splash image before the title menu.
- Splash/title UI draws on a black backdrop so the map is not visible until gameplay starts.
- Hidden `SkipSplashAndLoadQuickSave` can be enabled in the settings file for fast Play Mode testing.
- Title flow now includes create-player, variable manual-save load/delete, hidden Continue/Load Quest buttons when unavailable, and in-game return-to-main-menu/quit actions.
- New Quest create-player now lets the player choose a hero-sheet image independently from class and gender.
- Hero save data now persists explicit sprite selection fields. UI portraits, player map sprite, and party followers resolve from that stored sprite data.
- Recruited `NpcPartyMember` heroes copy their sprite from the map object's referenced Tiled sprite `gid`, storing the resolved tileset path and local tile id.

## Important Current Constraint

The old MonoGame/Nez project was removed from this branch. Use `main` if old implementation reference is needed. Work should target:

- `DungeonEscape.Unity/`
- `DungeonEscape.Core/`
- tests/docs/CI as needed

## Recently Touched Areas

- `DungeonEscape.Core/State/TiledMapInfo.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Maps/**/*.tmx`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/GameMenu.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/GameState.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/PlayerGridController.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/Rendering/HeroSpriteResolver.cs`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/Unity/TitleMenu.cs`
- `DungeonEscape.sln`
- `memory-bank/MANUAL_TESTS.md`
- `memory-bank/UNITY_MIGRATION.md`
- `memory-bank/`

## Recent Validation

Recent checks passed:

- `dotnet build DungeonEscape.sln`
- `dotnet test DungeonEscape.sln --no-restore`
- `git diff --check`

Latest local verification for hero sprite selection/recruit sprite work:

- `dotnet test DungeonEscape.sln`
- Unity batch validation was blocked because another Unity editor instance already had `DungeonEscape.Unity` open.

Unity map metadata validation showed:

- Chests: `109`
- Doors: `5`
- Missing `Locked`: `0`
- Wrong `Locked`: `0`
- Object-level `type=` count: `0`

## Next Likely Work

Continue from `memory-bank/UNITY_MIGRATION.md`. Likely upcoming work is still in:

- Persistence/title/create-player polish.
- Unity cleanup.
- Build/test automation expansion.
- Remaining map/UI/combat polish and cleanup listed in `memory-bank/UNITY_MIGRATION.md`.

After every implemented gameplay step, update `memory-bank/MANUAL_TESTS.md` with manual verification steps.
