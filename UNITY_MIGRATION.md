# Unity Migration Status

This file tracks the Dungeon Escape Unity migration. Update it as each area moves from pending to implemented, tested, or deliberately deferred.

## Guiding Priority

Keep combat migration last. Finish the map-mode gameplay loop, party systems, UI, persistence, and Unity cleanup first.

## 1. Validate Current Gameplay Loop

Status: Mostly done

- Done: New game starts at the map `DefaultSpawn`.
- Done: Warps with `SpawnId` work.
- Done: Overworld fallback position is remembered when returning without a spawn point.
- Done: Chests generate once when first entering a map.
- Done: Chest contents persist until picked up.
- Done: Chests open visually.
- Done: Chest state survives save/load.
- Done: Dialog choices can trigger recruitment.
- Done: Item transfer/drop/use dialogs work through the game menu overlay.
- Pending: Broader validation that dialog choices advance quests correctly.
- Pending: Broader validation that dialog choices give/take quest items correctly.

## 2. Party Creation

Status: Mostly done

- Done: Starter hero uses class/stat generation from `classlevels.json`.
- Done: Starting equipment added.
- Done: Party member recruitment added.
- Done: Party followers render on the map.
- Done: Followers follow after movement, warp, and load.
- Done: Party members animate.
- Pending: Level-up progression validation.
- Pending: Skill/spell assignment validation after level-up.

## 3. UI Migration

Status: In progress

- Done: Party/status window.
- Done: Inventory view.
- Done: Inventory item detail panel.
- Done: Item rarity text coloring.
- Done: Item and character sprites in UI where available.
- Done: Quest log.
- Done: Settings UI.
- Done: Settings tabs: General, UI, Input Bindings, Debug.
- Done: UI scale setting.
- Done: Configurable UI colors, border thickness, hover color, and highlight color.
- Done: Gamepad navigation through main menu UI.
- Done: Keyboard and gamepad input rebinding.
- Done: Modal overlay for menu actions, including use/transfer/drop/bind prompts.
- Done: Manual save slots in the Save tab.
- Done: New game action in the Save tab.
- Pending: Fuller party/status detail polish.
- Pending: Shop/healer/save NPC UI.
- Pending: Any final UI layout polish after more map gameplay is migrated.

## 4. Map Gameplay

Status: In progress

- Done: NPC dialog works.
- Done: Recruitable NPCs can join the party.
- Done: Static NPCs turn toward the player during dialog and turn back afterward.
- Done: Open chest visuals.
- Done: Layer rendering follows TMX layer order.
- Done: Player renders at the sprite layer level.
- Done: Hidden object debug rendering is controlled by settings.
- Pending: Doors and open-door actions.
- Pending: NPC heal behavior.
- Pending: NPC store behavior.
- Pending: Hidden item quest conditions.
- Pending: Object visuals for opened doors.
- Pending: Object visuals for removed hidden items.

## 5. Movement And Collision Rules

Status: In progress

- Done: Continuous movement while a key or stick is held.
- Done: Turning before moving when changing facing direction.
- Done: Sprint boost setting.
- Done: Smooth viewport scrolling pass.
- Done: Tile/object collision is partially driven from TMX data.
- Done: Damage and biome layers are data-only and do not render.
- Done: Water layer has custom-property support started.
- Pending: Finalize tile collision rules.
- Pending: Finalize object collision rules.
- Pending: Water/boat movement rules.
- Pending: Damage layer gameplay effects.
- Pending: Biome layer gameplay effects.
- Pending: Revisit viewport smoothness/performance if needed.

## 6. Encounter And Combat Migration

Status: Deferred until map mode is mostly complete

- Pending: Random encounters by biome.
- Pending: Monster loading/spawning.
- Pending: Combat UI.
- Pending: Turn system.
- Pending: Skills in combat.
- Pending: Spells in combat.
- Pending: Items in combat.

## 7. Audio

Status: Not started

- Pending: Music by map.
- Pending: Music by biome.
- Pending: Sound effects for chests.
- Pending: Sound effects for warps.
- Pending: Sound effects for dialog.
- Pending: Sound effects for damage.
- Pending: Sound effects for level-up.

## 8. Persistence Hardening

Status: In progress

- Done: Quick save/load.
- Done: Manual save slots.
- Done: New game reset without loading saved level.
- Done: Autosave enabled setting.
- Done: Autosave interval setting.
- Pending: Main title/load game flow.
- Pending: Version migration for old saves.
- Pending: Final autosave policy review.
- Pending: Final transition-save policy review.

## 9. Unity Project Cleanup

Status: In progress

- Done: Unity project folder created.
- Done: Unity asset folders created.
- Done: Unity-compatible assets copied.
- Done: Shared `DungeonEscape.Core` project created.
- Done: Initial portable state/core migration.
- Done: Temporary test-map debug screen removed.
- Pending: Replace remaining runtime filesystem asset loading with Unity-native asset references where appropriate.
- Pending: Confirm build settings.
- Pending: Confirm scenes included in build.
- Pending: Remove remaining temporary/debug code when no longer needed.
