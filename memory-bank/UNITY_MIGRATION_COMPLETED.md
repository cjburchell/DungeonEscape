# Completed Unity Migration

This file archives the completed Dungeon Escape Unity migration. Active post-migration work belongs in `UNITY_MIGRATION.md`, `ARCHITECTURE_BACKLOG.md`, `FUTURE_FEATURES.md`, or `BUGS.md`.

## Guiding Status

The Unity migration is complete. Map-mode gameplay, party systems, UI, persistence, audio, combat, build automation, and Unity cleanup are migrated enough to move on to feature and architecture work.

## 1. Gameplay Loop

- New game starts at the map `DefaultSpawn`.
- Warps with `SpawnId` work.
- Overworld fallback position is remembered when returning without a spawn point.
- Map changes use a fade-out/fade-in transition.
- Chests generate once when first entering a map.
- Chest contents persist until picked up.
- Chests open visually.
- Chest state survives save/load.
- Dialog choices can trigger recruitment.
- Dialog choices advance quests, including nested dialog choices.
- Dialog choices can give items.
- Dialog choices can take items when the party has the required item.
- Item transfer/drop/use dialogs work through the game menu overlay.
- `Outside` spell returns to the last remembered overworld position without party-member targeting.
- `Return` spell and `Wings` item show a visited-location picker on the overworld.
- Play-tested the `Lost_Sea_Shell` dialog path end to end in Unity.
- Play-tested the `Find_Ship` dialog path end to end in Unity, including the ship deed reward.

## 2. Party Creation

- Starter hero uses class/stat generation from `classlevels.json`.
- Starting equipment added.
- Party member recruitment added.
- Party followers render on the map.
- Followers follow after movement, warp, and load.
- Party members animate.
- Dead active party members render with the old coffin visual instead of their hero sprite.
- Dead active party members are visually followed behind living active party members and before the cart, without changing saved party order.
- Level-up progression validated with shared core tests.
- Skill/spell progression validated with shared core tests. Class skills are assigned from `classlevels.json`; spells unlock dynamically by level and class.

## 3. UI Migration

- Party/status window.
- Inventory view.
- Party and Inventory are combined into one Party tab with member detail sub-tabs for Status, Equipment, Items, Skills, and Spells.
- Inventory item detail panel.
- Item rarity text coloring.
- Item and character sprites in UI where available.
- Spell icons are shown in the Party spell tab using each spell's `ImageId` from the old spell icon tileset `items.tsx`.
- Quest log.
- Settings UI.
- Settings tabs: General, UI, Input Bindings, Debug.
- UI scale setting.
- Hidden settings can show/hide the UI and Debug settings tabs.
- Configurable UI colors, border thickness, hover color, and highlight color.
- Standard UI selection and confirm sounds use `select.wav` and `confirm.wav` through common UI controls and keyboard/gamepad navigation paths.
- Gamepad navigation through main menu UI.
- Keyboard and gamepad input rebinding.
- Modal overlay for menu actions, including use/transfer/drop/bind prompts.
- Manual save slots in the Save tab.
- New game action in the Save tab.
- Fuller party/status detail polish pass with clearer vitals, attributes, XP-to-next, equipment, skills, and spells.
- Initial shop/healer/save NPC UI through map interaction message boxes.
- Inventory UI icon assets are prewarmed so the first Inventory open does not stall.
- Current UI migration manual tests passed.
- Fullscreen setting is applied at Unity startup and exposed on Settings > General.

## 4. Map Gameplay

- NPC dialog works.
- Recruitable NPCs can join the party.
- Static NPCs turn toward the player during dialog and turn back afterward.
- Open chest visuals.
- Chest loot is explicit: `ItemId="#Random#"` generates random loot, fixed `ItemId` gives a fixed item, and missing `ItemId` means empty.
- Layer rendering follows TMX layer order.
- Player renders at the sprite layer level.
- Hidden object debug rendering is controlled by settings.
- Doors and open-door actions.
- Direct door interaction uses matching party keys.
- Doors can be explicitly unlocked with `Locked=false`; unlocked doors open without a key.
- Opened doors stop blocking movement and are hidden from the map.
- NPC heal behavior.
- NPC store buy/sell behavior.
- Store UI uses a persistent tabbed Buy/Sell window with item icons, party-member sell tabs, selected purchase recipient, and optional equip-after-buy prompt.
- NPC save behavior through quick-save service interaction.
- Hidden item quest conditions using the hidden item's quest/stage metadata.
- Removed hidden items no longer render or interact after pickup.
- Store parity with old map stores: support `NpcKey`, fixed `Items`, persistent generated inventory, `WillBuyItems`, item removal after buy, and old sell-price behavior.
- Healer parity with old healers: paid heal one, heal all, renew magic, cure status, and revive options based on healer `Cost`.
- Object-target item/spell actions, especially old `Target.Object` behavior for using map-facing skills/items on nearby objects.
- `Open` skill/items use the object the player is facing directly, without party-member targeting, and can open doors or chests.
- Non-combat party spell targeting filters invalid dead/living targets; only revive spells can target dead party members.
- Chests support optional locking with `Locked=true`; current chests remain unlocked by default.
- Chest and hidden-item level gating parity with old `Party.CanOpenChest(level)` behavior.
- Old cart follower visual migrated. The cart appears behind the party on the overworld when reserve party members exist, and hides while on water.
- Manual Unity play-test pass for store, healer, object-target use, and level-gated pickups.
- Manual Unity play-test pass for cart follower visibility and following behavior.

## 5. Movement And Collision Rules

- Continuous movement while a key or stick is held.
- Turning before moving when changing facing direction.
- Sprint boost setting, exposed on Settings > Debug.
- Turn delay setting, exposed on Settings > Debug.
- Smooth viewport scrolling pass.
- Tile collision rules use TMX `Collideable` layer data.
- Object collision rules use TMX `Collideable` object bounds and persisted open/removed object state.
- Damage and biome layers are data-only and do not render.
- Step/distance status effects are checked and updated after map movement, matching the old map-step status path.
- Water movement uses the TMX `Water` layer property and allows travel only on the overworld when the party has the ship deed.
- When the party has the ship deed and is on overworld water, followers are hidden and the player renders as the ship.
- Damage layer tile properties apply step damage to active party members.
- Biome layer tile classes update the party's current biome for future encounter logic.
- Cached TMX layer tile data for movement/gameplay queries to reduce repeated CSV parsing during movement.
- Tiled flipped object GIDs are normalized for map parsing, rendering, collision, and gameplay queries.
- Manual collision, water, damage, biome, stairs, doors, ship visual, save/load, and scrolling checks passed.

## 6. Audio

- Startup music begins on the recreated splash/title flow using `first-story`.
- Music by map through each TMX map's `song` property.
- Biome music hooks for town, cave, tower, and desert biome transitions, with other biomes falling back to map music.
- Separate settings-backed music and sound effect volume sliders on Settings > General.
- Sound effects for chests.
- Sound effects for warps.
- Sound effects for dialog.
- Sound effects for damage.
- Sound effects for level-up.
- Manual Unity audio validation passed for current map-mode music and sound effects.

## 7. Persistence Hardening

- Quick save/load.
- Variable manual saves without fixed save slots.
- New game reset without loading saved level.
- Autosave enabled setting.
- Autosave interval setting.
- Main title/load game flow with Continue, New Quest, Load Quest, and Quit.
- Unity splash screen recreated using `Images/ui/splash.png` before the title menu.
- Title/startup UI draws over a black backdrop instead of showing the map behind it.
- Hidden `SkipSplashAndLoadQuickSave` setting for fast test startup without exposing it in the Settings UI.
- Unity equivalent for old main menu/continue quest flow, including Continue, New Quest, Load Quest, and Quit.
- New Quest create-player flow with player name, random-name generation, gender/class dropdowns, character preview, starter stat panel, Re-roll, and selected player name/class/gender.
- In-game menu can return to the main menu or quit the game.
- Title Load Quest screen lists only existing manual saves, hides when no manual saves exist, and supports loading/deleting old manual saves.
- Title Load Quest supports per-save Delete buttons, click-to-load, Enter-to-load, and gamepad confirm-to-load.
- Main Menu, Load Quest, and Create Quest title screens support keyboard/gamepad navigation across actionable controls.
- Continue is hidden when no quick save exists.
- Save summaries no longer display map, position, gold, or steps.
- Unity save version policy: unsupported versions are archived and ignored; forward migrations will be added only when the Unity save schema changes.
- Final autosave policy: timer autosave is kept, but autosave is skipped while title/menu/store/dialog UI is active; combat can also block autosave through `GameState.AutoSaveBlocked`.
- Final transition-save policy: transition saves only occur when moving to or from the overworld.

## 8. Build And Test Automation

- Add GitLab CI pipeline for solution restore/build/test.
- Add Unity project validation to CI by default. Disable with `UNITY_CI_ENABLED=false` if needed.
- Add Unity Windows artifact build to CI by default. Disable with `UNITY_BUILD_WINDOWS_ENABLED=false`; output is stored as a downloadable GitLab artifact.
- Stage runtime map, tileset, image, and data files into `StreamingAssets` during Unity builds.
- Removed the old `DungeonEscape.Test` project from the solution; migration tests now run through `DungeonEscape.Core.Test` and future Unity test assemblies.

## 9. Unity Project Cleanup

- Unity project folder created.
- Unity asset folders created.
- Unity-compatible assets copied.
- Shared `DungeonEscape.Core` project created.
- Initial portable state/core migration.
- Temporary test-map debug screen removed.
- `Boot.unity` added to Unity Build Settings.
- Built player runtime asset paths now resolve through `StreamingAssets`.
- Static UI and combat image loading uses Unity editor asset references where appropriate, with `StreamingAssets` fallback retained for built players.
- Remaining file-backed asset loading is intentional for external TMX, TSX, JSON, audio, and image assets staged into `StreamingAssets`.
- Confirmed File > Build And Run renders the map correctly outside the editor.
- Unity scripts reorganized into `Core`, `Map`, `Rendering`, `Map/Tiled`, and `UI` folders with matching namespaces.
- Most Unity script class/file names no longer carry the `DungeonEscape` prefix; Tiled-specific script class/file names no longer carry the `Tiled` prefix and live under `Redpoint.DungeonEscape.Unity.Map.Tiled`.
- Removed noisy temporary migration/debug logs from audio playback, startup data loading, splash startup, and random encounter startup.

## 10. Encounter And Combat Migration

- Biome random encounter check runs after completed map steps and opens combat with the selected monsters.
- Carry forward old biome encounter metadata, including min/max monster level, for random encounter filtering.
- Random encounters by biome open the combat screen.
- Per-map random encounter tables from `Content/data/{mapId}_monsters.json`. Current pass loads copied Unity data from `Assets/DungeonEscape/Data/maps/{mapId}_monsters.json`.
- Monster loading/spawning. Current pass resolves monster images from `allmonsters.tsx`, displays each monster instance, rolls monster stats through `MonsterInstance`, and keeps relative monster sprite sizes.
- Biome combat backgrounds. Current pass displays the old fight background image for the selected biome.
- Combat layout shows full-screen aspect-preserved biome backgrounds with a bottom message box.
- Gold window is hidden during combat, and the party status window remains visible during combat.
- Autosave is blocked while combat is open.
- Current combat UI pass supports encounter message, old-style action menu, spell/item icon lists, direct monster-sprite enemy target selection, party-status-window target selection, HP bars, and round messages.
- Combat round flow now matches the old game model: monster actions and all living hero actions are chosen first, then queued actions resolve in agility order.
- Combat rewards award XP, gold, monster item drops, rare chest-style drops, and level-up messages on victory.
- Skills in combat can run encounter skills through shared core `Skill.Do`.
- Spells in combat can cast encounter spells through shared core `Spell.Cast`; non-revive spells target living members only and revive spells target dead members.
- Items in combat can use encounter items with item icons and shared core item/skill effects.
- Manual Unity combat validation passed for random encounters, biome backgrounds, monster display, Fight loop, rewards, action menu, spell/item icons, spell targeting, skills, items, and Run.
- Combat music selection uses the old battle track pool: `battleground`, `like-totally-rad`, `sword-metal`, and `unprepared`.
- Combat close restores the current map or biome music.
- Combat victory and successful Run play the old end-fight track `not-in-vain` while the result message is shown, then restore map/biome music after closing combat.
- Combat defeat shows `Everyone has died!` and returns to the title menu with title music instead of returning to map play.
- Round-duration status effects are cleared from party members when combat exits.
- Attack-style skills now run the old normal attack hit/damage step before applying the skill effect when `DoAttack` or `SkillType.Attack` requires it.
- Combat sound effects use existing audio for attacks, misses, spells, item use, victory, defeat, and monster/player damage where matching assets exist.
- Combat audio flows through the existing `Audio` service, so Music Volume and Sound Effects Volume apply to combat.
- Monster encounters can be enabled/disabled from Settings > Debug through the existing `NoMonsters` setting.
- Combat UI polish pass after the combat rules settled.
- Combat target selection now uses the rendered combat battlefield/status UI instead of opening a separate target list for monsters or party members.
