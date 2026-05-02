# Unity Migration Status

This file tracks the Dungeon Escape Unity migration. Update it as each area moves from pending to implemented, tested, or deliberately deferred.

## Guiding Priority

Keep combat migration after map-mode gameplay, party systems, UI, persistence, Unity cleanup, and audio.

## 1. Validate Current Gameplay Loop

Status: Done

- Done: New game starts at the map `DefaultSpawn`.
- Done: Warps with `SpawnId` work.
- Done: Overworld fallback position is remembered when returning without a spawn point.
- Done: Map changes use a fade-out/fade-in transition.
- Done: Chests generate once when first entering a map.
- Done: Chest contents persist until picked up.
- Done: Chests open visually.
- Done: Chest state survives save/load.
- Done: Dialog choices can trigger recruitment.
- Done: Dialog choices advance quests, including nested dialog choices.
- Done: Dialog choices can give items.
- Done: Dialog choices can take items when the party has the required item.
- Done: Item transfer/drop/use dialogs work through the game menu overlay.
- Done: `Outside` spell returns to the last remembered overworld position without party-member targeting.
- Done: `Return` spell and `Wings` item show a visited-location picker on the overworld.
- Done: Play-tested the `Lost_Sea_Shell` dialog path end to end in Unity.
- Done: Play-tested the `Find_Ship` dialog path end to end in Unity, including the ship deed reward.

## 2. Party Creation

Status: Done

- Done: Starter hero uses class/stat generation from `classlevels.json`.
- Done: Starting equipment added.
- Done: Party member recruitment added.
- Done: Party followers render on the map.
- Done: Followers follow after movement, warp, and load.
- Done: Party members animate.
- Done: Dead active party members render with the old coffin visual instead of their hero sprite.
- Done: Dead active party members are visually followed behind living active party members and before the cart, without changing saved party order.
- Done: Level-up progression validated with shared core tests.
- Done: Skill/spell progression validated with shared core tests. Class skills are assigned from `classlevels.json`; spells unlock dynamically by level and class.

## 3. UI Migration

Status: Done

- Done: Party/status window.
- Done: Inventory view.
- Done: Party and Inventory are combined into one Party tab with member detail sub-tabs for Status, Equipment, Items, Skills, and Spells.
- Done: Inventory item detail panel.
- Done: Item rarity text coloring.
- Done: Item and character sprites in UI where available.
- Done: Spell icons are shown in the Party spell tab using each spell's `ImageId` from the old spell icon tileset `items.tsx`.
- Done: Quest log.
- Done: Settings UI.
- Done: Settings tabs: General, UI, Input Bindings, Debug.
- Done: UI scale setting.
- Done: Hidden settings can show/hide the UI and Debug settings tabs.
- Done: Configurable UI colors, border thickness, hover color, and highlight color.
- Done: Gamepad navigation through main menu UI.
- Done: Keyboard and gamepad input rebinding.
- Done: Modal overlay for menu actions, including use/transfer/drop/bind prompts.
- Done: Manual save slots in the Save tab.
- Done: New game action in the Save tab.
- Done: Fuller party/status detail polish pass with clearer vitals, attributes, XP-to-next, equipment, skills, and spells.
- Done: Initial shop/healer/save NPC UI through map interaction message boxes.
- Done: Inventory UI icon assets are prewarmed so the first Inventory open does not stall.
- Done: Current UI migration manual tests passed.
- Done: Fullscreen setting is applied at Unity startup and exposed on Settings > General.
- Reviewed: Old direct map command shortcuts remain consolidated into the tabbed Menu action for now; direct Inventory/Quest/Settings shortcuts can be reintroduced later if playtesting shows the tab flow is too slow.
- Follow-up: Any future UI layout polish discovered during later gameplay migration.

## 4. Map Gameplay

Status: Done

- Done: NPC dialog works.
- Done: Recruitable NPCs can join the party.
- Done: Static NPCs turn toward the player during dialog and turn back afterward.
- Done: Open chest visuals.
- Done: Chest loot is explicit: `ItemId="#Random#"` generates random loot, fixed `ItemId` gives a fixed item, and missing `ItemId` means empty.
- Done: Layer rendering follows TMX layer order.
- Done: Player renders at the sprite layer level.
- Done: Hidden object debug rendering is controlled by settings.
- Done: Doors and open-door actions.
- Done: Direct door interaction uses matching party keys.
- Done: Doors can be explicitly unlocked with `Locked=false`; unlocked doors open without a key.
- Done: Opened doors stop blocking movement and are hidden from the map.
- Done: NPC heal behavior.
- Done: NPC store buy/sell behavior.
- Done: Store UI uses a persistent tabbed Buy/Sell window with item icons, party-member sell tabs, selected purchase recipient, and optional equip-after-buy prompt.
- Done: NPC save behavior through quick-save service interaction.
- Done: Hidden item quest conditions using the hidden item's quest/stage metadata.
- Done: Removed hidden items no longer render or interact after pickup.
- Done: Store parity with old map stores: support `NpcKey`, fixed `Items`, persistent generated inventory, `WillBuyItems`, item removal after buy, and old sell-price behavior.
- Done: Healer parity with old healers: paid heal one, heal all, renew magic, cure status, and revive options based on healer `Cost`.
- Done: Object-target item/spell actions, especially old `Target.Object` behavior for using map-facing skills/items on nearby objects.
- Done: `Open` skill/items use the object the player is facing directly, without party-member targeting, and can open doors or chests.
- Done: Non-combat party spell targeting filters invalid dead/living targets; only revive spells can target dead party members.
- Done: Chests support optional locking with `Locked=true`; current chests remain unlocked by default.
- Done: Chest and hidden-item level gating parity with old `Party.CanOpenChest(level)` behavior.
- Done: Old cart follower visual migrated. The cart appears behind the party on the overworld when reserve party members exist, and hides while on water.
- Done: Manual Unity play-test pass for store, healer, object-target use, and level-gated pickups.
- Done: Manual Unity play-test pass for cart follower visibility and following behavior.

## 5. Movement And Collision Rules

Status: Done

- Done: Continuous movement while a key or stick is held.
- Done: Turning before moving when changing facing direction.
- Done: Sprint boost setting, exposed on Settings > Debug.
- Done: Turn delay setting, exposed on Settings > Debug.
- Done: Smooth viewport scrolling pass.
- Done: Tile collision rules use TMX `Collideable` layer data.
- Done: Object collision rules use TMX `Collideable` object bounds and persisted open/removed object state.
- Done: Damage and biome layers are data-only and do not render.
- Done: Water movement uses the TMX `Water` layer property and allows travel only on the overworld when the party has the ship deed.
- Done: When the party has the ship deed and is on overworld water, followers are hidden and the player renders as the ship.
- Done: Damage layer tile properties apply step damage to active party members.
- Done: Biome layer tile classes update the party's current biome for future encounter logic.
- Done: Cached TMX layer tile data for movement/gameplay queries to reduce repeated CSV parsing during movement.
- Done: Tiled flipped object GIDs are normalized for map parsing, rendering, collision, and gameplay queries.
- Done: Manual collision, water, damage, biome, stairs, doors, ship visual, save/load, and scrolling checks passed.

## 6. Audio

Status: Done

- Done: Startup music begins on the recreated splash/title flow using `first-story`.
- Done: Music by map through each TMX map's `song` property.
- Done: Biome music hooks for town, cave, tower, and desert biome transitions, with other biomes falling back to map music.
- Done: Separate settings-backed music and sound effect volume sliders on Settings > General.
- Done: Sound effects for chests.
- Done: Sound effects for warps.
- Done: Sound effects for dialog.
- Done: Sound effects for damage.
- Done: Sound effects for level-up.
- Done: Manual Unity audio validation passed for current map-mode music and sound effects.

## 7. Persistence Hardening

Status: Done

- Done: Quick save/load.
- Done: Variable manual saves without fixed save slots.
- Done: New game reset without loading saved level.
- Done: Autosave enabled setting.
- Done: Autosave interval setting.
- Done: Main title/load game flow with Continue, New Quest, Load Quest, and Quit.
- Done: Unity splash screen recreated using `Images/ui/splash.png` before the title menu.
- Done: Title/startup UI draws over a black backdrop instead of showing the map behind it.
- Done: Hidden `SkipSplashAndLoadQuickSave` setting for fast test startup without exposing it in the Settings UI.
- Done: Unity equivalent for old main menu/continue quest flow, including Continue, New Quest, Load Quest, and Quit.
- Done: New Quest create-player flow with player name, random-name generation, gender/class dropdowns, character preview, starter stat panel, Re-roll, and selected player name/class/gender.
- Done: In-game menu can return to the main menu or quit the game.
- Done: Title Load Quest screen lists only existing manual saves, hides when no manual saves exist, and supports loading/deleting old manual saves.
- Done: Title Load Quest supports per-save Delete buttons, click-to-load, Enter-to-load, and gamepad confirm-to-load.
- Done: Main Menu, Load Quest, and Create Quest title screens support keyboard/gamepad navigation across actionable controls.
- Done: Continue is hidden when no quick save exists.
- Done: Save summaries no longer display map, position, gold, or steps.
- Done: Unity save version policy: unsupported versions are archived and ignored; forward migrations will be added only when the Unity save schema changes.
- Done: Final autosave policy: timer autosave is kept, but autosave is skipped while title/menu/store/dialog UI is active; combat can also block autosave through `DungeonEscapeGameState.AutoSaveBlocked`.
- Done: Final transition-save policy: transition saves only occur when moving to or from the overworld.

## 8. Build And Test Automation

Status: In progress

- Done: Add GitLab CI pipeline for solution restore/build/test.
- Done: Add Unity project validation to CI by default. Disable with `UNITY_CI_ENABLED=false` if needed.
- Done: Add Unity Windows artifact build to CI by default. Disable with `UNITY_BUILD_WINDOWS_ENABLED=false`; output is stored as a downloadable GitLab artifact.
- Done: Stage runtime map, tileset, image, and data files into `StreamingAssets` during Unity builds.
- Done: Removed the old `DungeonEscape.Test` project from the solution; migration tests now run through `DungeonEscape.Core.Test` and future Unity test assemblies.
- Pending: Expand shared core unit tests beyond level-up and skill/spell progression.
- Pending: Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Pending: Add regression tests for quest dialog actions and item rewards.
- Pending: Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.

## 9. Unity Project Cleanup

Status: In progress

- Done: Unity project folder created.
- Done: Unity asset folders created.
- Done: Unity-compatible assets copied.
- Done: Shared `DungeonEscape.Core` project created.
- Done: Initial portable state/core migration.
- Done: Temporary test-map debug screen removed.
- Done: `Boot.unity` added to Unity Build Settings.
- Done: Built player runtime asset paths now resolve through `StreamingAssets`.
- Done: Confirmed File > Build And Run renders the map correctly outside the editor.
- Pending: Replace remaining runtime filesystem asset loading with Unity-native asset references where appropriate.
- Pending: Remove remaining temporary/debug code when no longer needed.
- Pending: Decide whether old developer/debug console commands should be recreated with Unity tooling.

## 10. Encounter And Combat Migration

Status: Started

- Partial: Biome random encounter check runs after completed map steps and logs selected monsters to the Unity Console.
- Done: Carry forward old biome encounter metadata, including min/max monster level, for random encounter filtering.
- Partial: Random encounters by biome. Current pass logs encounters and opens the combat screen.
- Partial: Per-map random encounter tables from `Content/data/{mapId}_monsters.json`. Current pass loads copied Unity data from `Assets/DungeonEscape/Data/maps/{mapId}_monsters.json`.
- Partial: Monster loading/spawning. Current pass resolves monster images from `allmonsters.tsx`, displays each monster instance, rolls monster stats through `MonsterInstance`, and keeps relative monster sprite sizes.
- Partial: Biome combat backgrounds. Current pass displays the old fight background image for the selected biome.
- Partial: Combat layout shows full-screen aspect-preserved biome backgrounds with a bottom message box.
- Done: Gold window is hidden during combat, and the party status window remains visible during combat.
- Done: Autosave is blocked while combat is open.
- Partial: Combat UI. Current pass supports encounter message, old-style action menu, spell/item icon lists, target buttons, HP bars, and round messages.
- Partial: Turn system. Current pass rolls initiative and resolves hero Fight, Spell, Skill, Item, Run, and monster attack turns.
- Partial: Combat rewards. Current pass awards XP, gold, monster item drops, rare chest-style drops, and level-up messages on victory.
- Partial: Skills in combat. Current pass can run encounter skills through shared core `Skill.Do`; needs broader effect play-testing.
- Partial: Spells in combat. Current pass can cast encounter spells through shared core `Spell.Cast`; non-revive spells target living members only, revive spells target dead members, and broader effect play-testing is still needed.
- Partial: Items in combat. Current pass can use encounter items with item icons and shared core item/skill effects; needs broader charge/consume play-testing.
- Pending: Combat music selection, including `battleground` or map/encounter-specific combat tracks.
- Pending: Restore the correct map or biome music after combat ends.
- Pending: Combat sound effects for attacks, misses, spells, item use, victory, defeat, and monster/player damage.
- Pending: Confirm combat audio respects the existing Music Volume and Sound Effects Volume settings.
