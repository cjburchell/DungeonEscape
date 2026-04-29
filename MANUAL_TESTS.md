# Manual Test Plans

This file tracks manual Unity play-test checks for migrated gameplay. Update it after each implemented migration step so verification stays tied to the work that changed.

## Movement And Collision Rules

Status: Done

### Collision

- Start or load a game on a town or dungeon map.
- Walk into walls, water edges, counters, closed doors, and NPCs.
- Expected: the player cannot enter blocked tiles or occupied NPC tiles.
- Open a door that should be openable.
- Expected: after opening, the door no longer blocks movement and is not rendered.

### Object Bounds

- Find larger objects or wide hidden, warp, or static object rectangles.
- Try walking through every tile covered by the object.
- Expected: if `Collideable=true`, every covered tile blocks movement.
- Try non-blocking warps and exits.
- Expected: `Collideable=false` objects do not block movement.

### Water Without Ship

- Start a new game or load a save before getting `Deed to the ship`.
- Go to the overworld or any map with water.
- Try walking onto water.
- Expected: movement is blocked.

### Water With Ship

- Load a save after completing `Find_Ship` and receiving `Deed to the ship`.
- Go to overworld or coast water.
- Try walking onto water.
- Expected: movement onto water is allowed.
- Try walking from water back to land.
- Expected: movement back to land is allowed.

### Damage Layer

- Find a known damage tile area, likely swamp or damage-marked overworld tiles.
- Note active party members' HP.
- Step onto the damage tile.
- Expected: active living party members lose HP and a terrain message appears.
- Step again.
- Expected: damage applies once per completed step, not while standing still.

### Biome Tracking

- Walk across overworld terrain types: grassland, forest, hills, desert, and water.
- Expected: no visible error or movement hitch.
- Expected: the debug window shows the current biome changing as the party enters each biome.

### Performance And Scrolling

- Hold a movement key or stick for 20-30 seconds on the overworld.
- Move near viewport edges horizontally and vertically.
- Expected: no major hitching from layer parsing, and scrolling should be at least as smooth as before.

### Save And Load

- Pick up a hidden item or open a door.
- Save.
- Load.
- Expected: removed/open object state still affects rendering and collision.

## Chest Collision

Status: Done

- Load a map with chests, such as `dungeon/first`, `tunnel/area1`, or `towns/isis`.
- Try walking directly onto a closed chest tile.
- Expected: movement is blocked.
- Stand next to the chest and face it.
- Press interact.
- Expected: the chest opens and gives its contents or says it is empty.
- Try walking onto the opened chest tile.
- Expected: movement is still blocked because the chest remains a physical object.
- Open a chest with `ItemId="#Random#"`.
- Expected: the chest gives random loot or gold generated once for that map/save.
- Open a chest with a fixed `ItemId`, such as an `Iron Key` chest.
- Expected: the chest gives that fixed item.
- Inspect an object named `Open Chest`.
- Expected: it has no `ItemId`, starts/appears empty, and gives no random loot.

## Stairs And Flipped Tiled Objects

Status: Done

- Load maps with stairs that previously looked misplaced, such as `dungeon/first`, `forest_tower/floor1`, `forest_tower/floor2`, `island_tower/floor1`, `pyramid/basement`, `tunnel/area1`, or `tunnel/area2`.
- Compare the stair sprite location in Unity with the same `.tmx` map in Tiled.
- Expected: stair objects render on the same tile as Tiled, not as hidden markers or one tile off.
- Step onto each stair/warp tile.
- Expected: the warp triggers from the visible stair tile.

## Door Key Interaction

Status: Done

- Load a map with key-locked doors, such as `towns/walled`, `tunnel/area1`, or `island_tower/floor1`.
- Stand next to a closed door without a matching key and press interact.
- Expected: a message says the party does not have a key for the door, and the door stays closed.
- Pick up an `Iron Key` from a key chest.
- Stand next to the closed door, face it, and press interact.
- Expected: the door opens, disappears from the map view, and no longer blocks movement.
- Save and reload after opening a door.
- Expected: the door remains open and non-blocking.
- Go to the locked ship door in `towns/coast` before completing the old man's key handoff.
- Press interact while carrying a normal key.
- Expected: the message says the door is locked and the door does not open.
- Complete the old man's key handoff.
- Expected: the old man's dialog opens the ship door.

## Ship Over Water Visual

Status: Done

- Load a save before receiving `Deed to the ship`.
- Try moving onto water.
- Expected: movement is blocked and the player sprite remains the normal party leader.
- Load or play to a save after receiving `Deed to the ship`.
- Move from land onto water.
- Expected: the party followers are hidden and the player is rendered as the ship sprite while on water.
- Move from water back onto land.
- Expected: the normal player sprite and followers return.
- Change facing direction while on water.
- Expected: the ship sprite changes to match the current direction.

## Inventory First-Open Performance

Status: Done

- Start the Unity scene and wait a few seconds after the map appears.
- Open the game menu and switch to Inventory for the first time.
- Expected: the Inventory tab opens without a long first-time stall from item icon loading.
- Close and reopen the menu, then switch to Inventory again.
- Expected: repeated opens remain quick and item/hero icons still render.

## Map Fade Transitions

Status: Done

- Step onto a warp or stairs that changes maps.
- Expected: the screen fades to black, the new map loads while black, then fades back in.
- During the fade, hold a movement key or stick.
- Expected: the player does not move or trigger another interaction until the transition completes.
- Test returning to the overworld through a warp that uses the remembered overworld position.
- Expected: the fade still runs and the party appears at the remembered overworld position.
- Trigger a dialog choice that warps to another map, if available.
- Expected: the same fade transition is used.
