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
- Expected: active living party members lose HP and no blocking terrain dialog appears.
- Step again.
- Expected: damage applies once per completed step, not while standing still.
- Continue stepping on damage tiles until a party member reaches `0` HP.
- Expected: a terrain message appears only when a party member dies.

### Biome Tracking

- Walk across overworld terrain types: grassland, forest, hills, desert, and water.
- Expected: no visible error or movement hitch.
- Expected: the debug window shows the current biome changing as the party enters each biome.

### Performance And Scrolling

- Hold a movement key or stick for 20-30 seconds on the overworld.
- Move near viewport edges horizontally and vertically.
- Expected: no major hitching from layer parsing, and scrolling should be at least as smooth as before.
- Hold sprint while moving across viewport edges horizontally and vertically.
- Expected: player animation and viewport scrolling stay in sync without visible stutter from mismatched movement/scroll timing.
- Open Settings > General and adjust Turn Delay.
- Expected: changing direction only turns first, then moves after the configured delay; moving while already facing that direction has no extra turn delay.

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

- Add or find a door with `Locked=false`.
- Stand next to it, face it, and press interact.
- Expected: the door opens without requiring a key.
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

## Locked And Unlocked Chests

Status: Done

- Open an existing chest that does not define `Locked`.
- Expected: the chest opens without a key, matching existing chest behavior.
- Add or find a chest with `Locked=false`.
- Expected: the chest opens without a key.
- Add or find a chest with `Locked=true` and `ChestLevel=0`.
- Stand next to it without a level-0 key item and press interact.
- Expected: the chest stays closed and says the party does not have a key for the chest.
- Give the party an `Iron Key`, stand next to the same chest, and press interact.
- Expected: the chest opens and gives its normal persisted contents.
- Add or find a chest with `Locked=true` and `KeyItemId` set to a specific key item.
- Expected: only that key item opens the chest.

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

## Cart Follower Visual

Status: Done

- Start or load a game on the overworld with no reserve party members.
- Expected: no cart is shown.
- Recruit enough party members that at least one member is in reserve, or move an active member to reserve from the Party UI.
- Expected: a cart appears behind the active party followers on the overworld.
- Hold movement for several tiles in each direction.
- Expected: the cart follows behind the last visible active follower, or behind the player if there are no active followers after the leader.
- Walk behind and in front of NPCs, walls, and tall sprite objects.
- Expected: the cart sorts like other sprite-layer characters and does not draw over objects it should be behind.
- Move onto water after obtaining `Deed to the ship`.
- Expected: the player changes to the ship visual and the cart is hidden.
- Move from water back to land.
- Expected: the cart returns if a reserve party member still exists.
- Warp from the overworld to a town, dungeon, or other non-overworld map.
- Expected: the cart is hidden.
- Warp back to the overworld.
- Expected: the cart appears again if a reserve party member still exists.
- Save and load while the party has reserve members.
- Expected: cart visibility is restored according to the loaded map and current tile.

## Dead Party Member Coffin Visual

Status: Pending

- Load or create a party with at least two active members.
- Reduce one active follower's HP to `0`, for example by stepping on damage tiles until that party member dies.
- Expected: that party member's map follower changes from the hero sprite to the coffin visual.
- Continue moving in all four directions.
- Expected: the coffin follows in the normal party order and animates/faces consistently with the movement direction.
- Reduce the active leader's HP to `0`, if possible.
- Expected: the player visual changes to the coffin visual while on land.
- Move onto water after obtaining `Deed to the ship`.
- Expected: the ship visual still takes precedence over the coffin/player visual while on water.
- Visit a healer and revive the dead member.
- Expected: the revived member changes back from the coffin to their class/gender hero sprite.
- Save and load with a dead active party member.
- Expected: the coffin visual is restored after loading.

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

## Store, Healer, And Object Target Parity

Status: Done

### Regular Stores

- Visit a regular `NpcStore`, such as a merchant in `towns/isis`, `towns/coast`, or `towns/walled`.
- Expected: a store window opens directly with `Buy` and `Sell` tabs instead of a buy/sell message prompt.
- Expected: item rows show item icons, names, stats, type/level, and price.
- Expected: the store shows a persistent stock list of roughly 10 generated items, or the fixed `Items` list if the map object defines one.
- Expected: generated regular stores do not show `Gold`, quest items, or hidden/chest reward items as store stock.
- Buy an item.
- Expected: the store asks which party member should carry it.
- Choose a party member.
- Expected: gold is deducted, the item goes to that party member, and the item is removed from that store's stock.
- If the purchased item can be equipped by that party member, choose Equip.
- Expected: the item is equipped immediately and the store window remains open.
- Leave the store and talk to the same merchant again.
- Expected: the purchased item is still gone from that store's stock.
- Save and reload.
- Expected: the store stock remains the same for that save.

### Key Stores

- Visit an `NpcKey` merchant, such as `towns/oasis` or `towns/walled`.
- Expected: only key items are listed.
- Expected: the Sell tab is disabled.
- Buy a key.
- Expected: gold is deducted and the key is added to the party inventory.

### Store Selling

- Visit a regular store with a non-quest, sellable item in inventory.
- Open the Sell tab.
- Expected: party members are shown as tabs.
- Select each party member.
- Expected: only that member's sellable items are shown, and each row has an item icon.
- Expected: the sell price is 75% of item cost, rounded down with a minimum of 1 gold.
- Sell an equipped item.
- Expected: the item is unequipped, removed from the hero inventory, gold is added, and the item can appear in the store stock if the store has room.
- Visit a store with `WillBuyItems=false`, if one exists.
- Expected: the Sell tab is disabled.
- While the store window is open, complete a buy or sell confirmation.
- Expected: confirmation messages appear over the store, and the store window does not close until Close or Cancel is used.

### Healers

- Damage one active party member, then visit an `NpcHeal`.
- Expected: Heal is shown with the healer's `Cost` property or 25 gold by default.
- Heal one member.
- Expected: only that member's HP is restored and gold is deducted.
- Damage multiple active party members.
- Expected: Heal All appears and costs `Cost * wounded member count`.
- Spend magic, add a negative status, or kill a member using test setup if available.
- Expected: Renew Magic, Cure, and Revive appear only when relevant and charge `Cost * 2`, `Cost * 2`, and `Cost * 10` respectively.
- Try a service without enough gold.
- Expected: the healer refuses and no party state changes.

### Object-Target Items And Spells

- Stand next to a closed door and face it.
- Open Inventory and use a key item or other item with the `Open` skill.
- Expected: no party-member target picker appears.
- Expected: the facing door opens, collision/rendering refreshes, and the inventory item is consumed if it is a one-use item.
- Stand facing empty space and use the same kind of object-target item or spell.
- Expected: the message says there is nothing there.
- If a hero has an `Open` spell, stand next to a closed door and cast it from the Party spell list.
- Expected: no party-member target picker appears.
- Expected: MP is deducted, the door opens, and the map refreshes.
- Stand next to a closed chest and face it.
- Use the `Open` spell or an item with the `Open` skill.
- Expected: the facing chest opens with the same contents, level-gating, and persistence behavior as pressing interact.
- Stand facing an NPC, hidden item, warp, or other non-door/non-chest object and use `Open`.
- Expected: the message says the object cannot be opened.

### Pickup Level Gating

- Find a chest or hidden item with a `ChestLevel` or `Level` above the active party's level.
- Press interact while facing it.
- Expected: the pickup does not open and the message says the party is not experienced enough.
- Raise the party level or test with a lower-level object.
- Expected: the same object can be opened or picked up when a living active member meets the required level.

## Unity Build And Run

Status: Done

- Open Unity and wait for scripts to finish compiling.
- Open `File > Build Settings`.
- Expected: `Assets/DungeonEscape/Scenes/Boot.unity` is listed and checked.
- Use `File > Build And Run`.
- Expected: the built Windows player opens to the same map view as Play Mode, not only the debug window.
- Expected: terrain, objects, NPC sprites, player sprites, item icons, and UI images are visible.
- Move, warp to another map, and open the inventory.
- Expected: map TMX files, TSX files, tileset images, character images, and item images continue loading in the built player.
- Result: Confirmed. `File > Build And Run` renders the map correctly outside the editor.

## Splash And Title Startup

Status: Pending

- Start Play Mode from `Boot.unity`.
- Expected: the old `splash.png` image fades in centered on a black screen, holds briefly, then fades out.
- Expected: the map is not visible behind the splash.
- Wait for the title menu.
- Expected: the title menu appears on a black background, not over the rendered map.
- Choose Continue, New Game, or Load Game.
- Expected: the title menu closes and the map becomes visible only after entering gameplay.
- Use File > Build And Run.
- Expected: the built player follows the same splash, black title background, and map reveal behavior.
- Set `SkipSplashAndLoadQuickSave` to `true` in the user settings file.
- Expected: Play Mode starts directly from the quick save without showing the splash or title menu.
- Set `SkipSplashAndLoadQuickSave` back to `false`.
- Expected: normal splash/title startup returns.

## Title And Load Flow

Status: Pending

- Start Play Mode or launch a built player.
- Expected: a `Dungeon Escape` title window appears before map controls are usable.
- Try moving while the title window is open.
- Expected: the player does not move.
- If no quick save exists, inspect the Continue button.
- Expected: Continue is disabled.
- Choose New Game.
- Expected: the title window closes, a fresh party starts at the map default spawn, and map controls work.
- Save into a manual slot from the in-game Save tab, then return to the title flow by restarting Play Mode or the built player.
- Choose Load Game.
- Expected: manual save slots are listed with save time, level, and map.
- Select a populated manual slot.
- Expected: the title window closes and the saved map, position, party, inventory, quest, and object state are restored.
- Create or wait for a quick save, then restart.
- Choose Continue.
- Expected: the title window closes and the quick save is loaded.
- Use keyboard or gamepad up/down and interact on the title and load screens.
- Expected: selection changes visibly and activates the selected command.
- Choose Quit in a built player.
- Expected: the player application exits.

## Unity Save Version Policy

Status: Pending

- Locate the Unity save file under `%APPDATA%/Redpoint/DungeonEscape/save.json`.
- Back up the file manually before testing this case.
- Edit the save JSON `Version` value to an unsupported value such as `0.0`.
- Start Play Mode or a built player.
- Expected: the game logs that the save version is unsupported.
- Expected: the original save is copied to a file named like `save.unsupported-0.0-YYYYMMDDHHMMSS.json`.
- Expected: the game creates a fresh Unity save file instead of trying to migrate old or unsupported save data.

## Autosave And Transition Save Policy

Status: Pending

- Enable autosave and set a short autosave interval in Settings.
- Move around on one non-overworld map without opening dialogs.
- Expected: timer autosave still occurs after the configured interval.
- Open a dialog or message box and wait longer than the autosave interval.
- Expected: no autosave happens while the dialog is visible.
- Close the dialog and keep playing.
- Expected: autosave can occur again after gameplay resumes.
- Open the title menu, game menu, or store window and wait longer than the autosave interval.
- Expected: no autosave happens while that UI is open.
- Warp between two non-overworld maps.
- Expected: no transition save occurs only because of that warp.
- Warp from the overworld into a town, dungeon, or other map.
- Expected: a transition save occurs after the destination map and final player position are applied.
- Warp from a town, dungeon, or other map back to the overworld.
- Expected: a transition save occurs after the party reaches the overworld.

## Outside And Return Skills

Status: Pending

- Get access to the `Outside` spell on a party member.
- Enter a non-overworld map, then open the party menu and cast `Outside`.
- Expected: no party member target picker appears.
- Expected: the party is warped to the last remembered overworld position.
- Expected: the previous non-overworld map is added to the return location list.
- Try casting `Outside` while already on the overworld.
- Expected: the spell is disabled, or if triggered directly, it reports that the party is already outside.
- Visit multiple non-overworld locations from the overworld.
- Return to the overworld and cast `Return`.
- Expected: the user is shown a list of visited locations.
- Select one location.
- Expected: the party is transported to that map and the remembered location on that map.
- Try casting `Return` while not on the overworld.
- Expected: the spell is disabled, or if triggered directly, it reports that Return can only be used outside.
- Use a `Wings` item from the inventory on the overworld after visiting at least one return location.
- Expected: the same visited-location picker appears, and selecting a location consumes the item and transports the party.
