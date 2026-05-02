# Manual Test Plan

This file tracks manual Unity play-test checks for migrated gameplay. Update it after each implemented migration step so verification stays tied to the work that changed.

Status legend:

- `[x]` Tested and passed.
- `[ ]` Not tested yet, or needs another pass after recent changes.

## Movement And Collision

### [x] Collision Blocks Walls, Water Edges, Doors, Chests, And NPCs

- Start or load a game on a town or dungeon map.
- Walk into walls, water edges, counters, closed doors, closed chests, and NPCs.
- Expected: the player cannot enter blocked tiles or occupied NPC tiles.
- Open a door that should be openable.
- Expected: after opening, the door no longer blocks movement and is not rendered.

### [x] Object Bounds Match Tiled Rectangles

- Find larger objects or wide hidden, warp, or static object rectangles.
- Try walking through every tile covered by the object.
- Expected: if `Collideable=true`, every covered tile blocks movement.
- Try non-blocking warps and exits.
- Expected: `Collideable=false` objects do not block movement.

### [x] Water Blocks Movement Before Ship Deed

- Start a new game or load a save before getting `Deed to the ship`.
- Go to the overworld or any map with water.
- Try walking onto water.
- Expected: movement is blocked.

### [x] Water Allows Movement After Ship Deed

- Load a save after completing `Find_Ship` and receiving `Deed to the ship`.
- Go to overworld or coast water.
- Try walking onto water.
- Expected: movement onto water is allowed.
- Try walking from water back to land.
- Expected: movement back to land is allowed.

### [x] Damage Layer Applies Step Damage

- Find a known damage tile area, likely swamp or damage-marked overworld tiles.
- Note active party members' HP.
- Step onto the damage tile.
- Expected: active living party members lose HP and no blocking terrain dialog appears.
- Step again.
- Expected: damage applies once per completed step, not while standing still.
- Continue stepping on damage tiles until a party member reaches `0` HP.
- Expected: a terrain message appears only when a party member dies.

### [x] Biome Layer Updates Current Biome

- Walk across overworld terrain types: grassland, forest, hills, desert, and water.
- Expected: no visible error or movement hitch.
- Expected: the debug window shows the current biome changing as the party enters each biome.

### [x] Camera Window Scrolling Is Smooth In Every Direction

- Hold a movement key or stick for 20-30 seconds on the overworld.
- Move near viewport edges horizontally and vertically.
- Expected: the camera scrolls smoothly when moving up, down, left, and right.
- Expected: the map does not jump by a full tile when the visible tile window advances.
- Expected: NPCs, objects, party followers, and animated tiles stay aligned while the camera scrolls.
- Hold sprint while moving across viewport edges horizontally and vertically.
- Expected: player animation and viewport scrolling stay in sync without visible stutter from mismatched movement/scroll timing.

### [x] Viewport Shows Old 32 By 18 Tile Baseline

- Start or load a game in Play Mode or a built player at a 16:9 resolution.
- Compare the visible map area against the old MonoGame version.
- Expected: Unity shows roughly 32 columns by 18 rows at 16:9, matching the old map scene zoom level.
- Resize the window wider than 16:9.
- Expected: Unity shows extra horizontal columns instead of stretching tiles.
- Resize the window taller than 16:9.
- Expected: tile aspect ratio remains correct and the map remains centered.

### [x] Tile Seams Do Not Flicker During Movement

- Start or load a game on the overworld or a map with repeated terrain tiles.
- Stand still and inspect tile edges near the center and viewport edges.
- Expected: no white/black/transparent lines appear between adjacent tiles.
- Hold movement horizontally and vertically, including near viewport scroll edges.
- Expected: tile-edge artifacts do not appear and disappear while the viewport scrolls.
- Toggle fullscreen/windowed mode and resize the window.
- Expected: tile edges remain stable and tiles keep the correct aspect ratio.

### [x] Turn Delay Only Applies When Changing Facing

- Open Settings > Debug and adjust Turn Delay.
- Press a direction different from the current facing direction.
- Expected: changing direction only turns first, then moves after the configured delay.
- Hold movement while already facing the movement direction.
- Expected: moving while already facing that direction has no extra turn delay.

## Map Objects And Interactions

### [x] Chest Collision And Opened Chest Behavior

- Load a map with chests, such as `dungeon/first`, `tunnel/area1`, or `towns/isis`.
- Try walking directly onto a closed chest tile.
- Expected: movement is blocked.
- Stand next to the chest and face it.
- Press interact.
- Expected: the chest opens and gives its contents or says it is empty.
- Try walking onto the opened chest tile.
- Expected: movement is still blocked because the chest remains a physical object.

### [x] Chest Loot Uses Fixed, Random, Or Empty Item Rules

- Open a chest with `ItemId="#Random#"`.
- Expected: the chest gives random loot or gold generated once for that map/save.
- Open a chest with a fixed `ItemId`, such as an `Iron Key` chest.
- Expected: the chest gives that fixed item.
- Inspect an object named `Open Chest`.
- Expected: it has no `ItemId`, starts/appears empty, and gives no random loot.

### [x] Locked And Unlocked Chests Use Key Rules

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

### [x] Doors Open With Correct Lock And Key Behavior

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

### [x] Ship Quest Door Opens Only Through Quest Handoff

- Go to the locked ship door in `towns/coast` before completing the old man's key handoff.
- Press interact while carrying a normal key.
- Expected: the message says the door is locked and the door does not open.
- Complete the old man's key handoff.
- Expected: the old man's dialog opens the ship door.

### [x] Stairs And Flipped Tiled Objects Render And Warp Correctly

- Load maps with stairs that previously looked misplaced, such as `dungeon/first`, `forest_tower/floor1`, `forest_tower/floor2`, `island_tower/floor1`, `pyramid/basement`, `tunnel/area1`, or `tunnel/area2`.
- Compare the stair sprite location in Unity with the same `.tmx` map in Tiled.
- Expected: stair objects render on the same tile as Tiled, not as hidden markers or one tile off.
- Step onto each stair/warp tile.
- Expected: the warp triggers from the visible stair tile.

### [x] Hidden Item Quest Conditions And Removal Persist

- Pick up a hidden item or open a door.
- Save.
- Load.
- Expected: removed/open object state still affects rendering and collision.

### [x] Pickup Level Gating Matches Party Level

- Find a chest or hidden item with a `ChestLevel` or `Level` above the active party's level.
- Press interact while facing it.
- Expected: the pickup does not open and the message says the party is not experienced enough.
- Raise the party level or test with a lower-level object.
- Expected: the same object can be opened or picked up when a living active member meets the required level.

## Party And Map Visuals

### [x] Active Party Status Window

- Start or load a game with at least one active party member.
- Stop moving.
- Expected: a compact status window appears in the top-left corner and shows active party members only, not reserve members.
- Expected: HP and MP are shown as compact progress bars rather than text-only values.
- Hold movement in any direction.
- Expected: the status window hides while the player is moving, then returns after movement stops.
- Open the title menu, game menu, store window, or a dialog/message box.
- Expected: the status window is hidden while blocking UI is visible.
- Damage a party member, spend magic, or revive/death-test a member if available.
- Expected: HP, MP, class, level, dead red text, and low-health orange text update without reopening the scene.

### [x] Gold Status Window

- Start or load a game.
- Stop moving.
- Expected: a compact gold window appears in the bottom-left corner and shows the current party gold.
- Hold movement in any direction.
- Expected: the gold window hides while the player is moving, then returns after movement stops.
- Open the title menu, game menu, store window, or a dialog/message box.
- Expected: the gold window is hidden while blocking UI is visible.
- Buy, sell, receive quest rewards, or otherwise change party gold.
- Expected: the gold value updates without reopening the scene.

### [x] Ship Visual Replaces Party While On Water

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

### [x] Cart Follower Appears For Reserve Party Members

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

### [x] Dead Party Member Uses Coffin Visual

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

## UI, Inventory, Stores, And Healers

### [x] Fullscreen Setting Applies

- Open the in-game menu and go to Settings > General.
- Toggle `Fullscreen`.
- Expected: the Unity player switches between fullscreen and windowed mode.
- When windowed, resize the game window by dragging the window edge or corner.
- Expected: the built player window resizes and the map/UI remain visible without changing tile aspect ratio.
- Close and restart Play Mode or the built player.
- Expected: the saved fullscreen setting is applied during startup.
- Navigate Settings > General with keyboard or gamepad.
- Expected: Fullscreen participates in the normal settings row navigation and does not prevent changing Autosave or Autosave Period.

### [ ] Hidden Settings Tabs Can Be Disabled

- Start with default settings.
- Expected: Settings does not show the UI or Debug tabs.
- In the settings file, set `ShowUiSettingsTab` to `true` and restart the game.
- Expected: Settings shows the UI tab.
- Set `ShowDebugSettingsTab` to `true` and restart the game.
- Expected: Settings shows the Debug tab.
- Restore both values to `false`.
- Expected: Settings shows only General and Input Bindings tabs again.

### [x] Inventory First Open Is Responsive

- Start the Unity scene and wait a few seconds after the map appears.
- Open the game menu and switch to Inventory for the first time.
- Expected: the Inventory tab opens without a long first-time stall from item icon loading.
- Close and reopen the menu, then switch to Inventory again.
- Expected: repeated opens remain quick and item/hero icons still render.

### [x] Inventory Detail Shows Only Available Actions

- Open the Party tab, select a member, and switch to the Items sub-tab with a mix of equippable, non-equippable, usable, quest, and regular items.
- Select a non-equippable item.
- Expected: Equip and Unequip buttons are not shown.
- Select an equipped item.
- Expected: Unequip is shown and Equip is not shown.
- Select an equippable item that is not equipped.
- Expected: Equip is shown and Unequip is not shown.
- Select an item that cannot currently be used.
- Expected: Use is not shown.
- Select a quest item.
- Expected: Drop is not shown.
- Inspect the item detail panel.
- Expected: internal slot/class metadata is not shown in the right panel.

### [x] Combined Party And Inventory Menu

- Open the in-game menu.
- Expected: there is no separate top-level Inventory tab.
- Select different active and reserve party members in the Party tab.
- Expected: the detail panel on the right updates for the selected member.
- Expected: the party member selection list shows only member names, plus reserve labels where applicable.
- Use the detail sub-tabs: Status, Equipment, Items, Skills, and Spells.
- Expected: each sub-tab shows only that selected member's information.
- Expected: each detail sub-tab uses a consistent height matching the menu body.
- Expected: the selected sub-tab text is bold.
- Expected: the selected member's name is shown once above the detail sub-tabs and is not repeated in Status.
- Expected: HP, MP, and XP show progress bars with current/max text.
- Expected: HP, MP, and XP progress bar fill is white on black, with text shown to the right of the bar.
- Expected: Skills and Spells tabs are hidden for members that do not know any.
- Expected: Effects is hidden when the selected member has no status effects.
- On the Items sub-tab, select items with a mix of equippable, usable, quest, and regular items.
- Expected: item names in the item selection list are left-aligned.
- Expected: the Equipment sub-tab shows item images beside equipped item names.
- Expected: item icons, rarity coloring, details, and available actions match the old Inventory behavior.
- Expected: Use actions are hidden when the selected item cannot currently be used.
- Expected: spell rows show only the spell name and a Cast button; Cast is disabled when the member does not have enough MP or cannot currently cast it.
- Expected: party ordering buttons are labelled `Up` and `Down`.
- Use Transfer, Drop, Equip, Unequip, and Use where available.
- Expected: actions still work through the existing modal overlays and update the selected member afterward.
- Use keyboard or gamepad left/right while on the Party tab.
- Expected: the selected detail sub-tab changes without changing the selected party member.

### [x] Regular Store Buy Flow Uses Tabbed Store UI

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

### [x] Key Store Buy Flow Lists Only Keys

- Visit an `NpcKey` merchant, such as `towns/oasis` or `towns/walled`.
- Expected: only key items are listed.
- Expected: the Sell tab is disabled.
- Buy a key.
- Expected: gold is deducted and the key is added to the party inventory.

### [x] Store Sell Flow Uses Party Member Tabs

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

### [x] Healer Services Use Cost And Relevant Options

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

## Object-Target Skills And Map Skills

### [x] Open Skill And Open Items Target Facing Objects

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

### [ ] Outside, Return, And Wings Travel Skills

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

## Persistence, Startup, And Save Flow

### [x] Map Fade Transitions Wrap Map Changes

- Step onto a warp or stairs that changes maps.
- Expected: the screen fades to black, the new map loads while black, then fades back in.
- During the fade, hold a movement key or stick.
- Expected: the player does not move or trigger another interaction until the transition completes.
- Test returning to the overworld through a warp that uses the remembered overworld position.
- Expected: the fade still runs and the party appears at the remembered overworld position.
- Trigger a dialog choice that warps to another map, if available.
- Expected: the same fade transition is used.

### [x] Splash And Title Startup Flow

- Start Play Mode from `Boot.unity`.
- Expected: the old `splash.png` image fades in centered on a black screen, holds briefly, then fades out.
- Expected: the map is not visible behind the splash.
- Wait for the title menu.
- Expected: the title menu appears over `mainmenue.png`, with no separate game title text and with menu buttons vertically centered in the bottom half of the screen.
- Choose Continue, New Quest, or Load Quest if those options are available.
- Expected: the title menu closes and the map becomes visible only after entering gameplay.
- Use File > Build And Run.
- Expected: the built player follows the same splash, image-backed title background, and map reveal behavior.
- Set `SkipSplashAndLoadQuickSave` to `true` in the user settings file.
- Expected: Play Mode starts directly from the quick save without showing the splash or title menu.
- Set `SkipSplashAndLoadQuickSave` back to `false`.
- Expected: normal splash/title startup returns.

### [x] Title Menu Hides Unavailable Continue And Load Quest

- Start Play Mode from `Boot.unity` with `SkipSplashAndLoadQuickSave` set to `false`.
- Expected: title menu appears after the splash.
- If no quick save exists, inspect the title menu.
- Expected: Continue is hidden.
- If no manual saves exist, inspect the title menu.
- Expected: Load Quest is hidden.
- If a quick save exists, select Continue.
- Expected: the quick save loads and the title menu closes.

### [x] New Quest Create Player Flow

- Return to the title menu from the in-game menu.
- Select New Quest.
- Expected: New Quest screen appears over `menu2.png` with vertically centered compact Name/Gender/Class controls, portrait, and stat panel inside a black panel with a white border.
- Enter a player name or press Random.
- Expected: Random fills the name field with a generated name for the selected gender.
- Open the Gender dropdown and choose a gender.
- Open the Class dropdown and choose a class.
- Expected: dropdown choices overlay the screen without shifting the rest of the UI.
- Expected: long dropdowns show a scrollbar when not all choices fit.
- Use keyboard or gamepad up/down and interact on Name, Generate Name, Gender, Class, Re-roll, Start, and Back.
- Expected: each Create Quest control can be selected and activated without using the mouse.
- Press down from Generate Name.
- Expected: selection moves to Re-roll.
- Press left from Generate Name.
- Expected: selection moves to Name.
- Press left/right on Start or Back.
- Expected: selection moves horizontally between Start and Back.
- Press up/down from Start or Back.
- Expected: selection follows the same visual column instead of moving through every control linearly.
- With a dropdown open, use keyboard or gamepad up/down and interact.
- Expected: dropdown choices can be selected without using the mouse.
- Expected: the character preview updates for the selected gender/class and keeps the sprite aspect ratio.
- Press Re-roll.
- Expected: the displayed starter stats change without changing the selected name, gender, or class.
- Select Start.
- Expected: a new game starts with the chosen player name/class/gender and the title menu closes.

### [x] Variable Manual Save Flow

- Open the in-game Save tab.
- Expected: existing manual saves are listed, plus a `New Save` row.
- Select `New Save` and save.
- Expected: a new manual save is appended; no fixed numbered save slot is required.
- Expected: the `New Save` detail only shows Save; it does not show Load, Delete, or New Game.
- Select an existing manual save.
- Expected: the detail panel shows only the save name, save time, and level; it does not show map, position, gold, or steps.
- Save over an existing manual save.
- Expected: the save is overwritten after confirmation.

### [x] Load Quest Screen Flow

- Return to the title menu again and select Load Quest.
- Expected: Load Quest screen appears over `menu2.png` with vertically centered title, manual saves on the left, and a Delete button beside each save inside a black panel with a white border.
- Expected: the centered Back button is fully visible below the save list.
- Press up/down while a save or Delete button is selected.
- Expected: selection moves to the previous/next save row, not sideways into Delete.
- Press right on a selected save row.
- Expected: selection moves to that save's Delete button.
- Press left on a selected Delete button.
- Expected: selection moves back to that save row.
- Click a populated manual save once with the mouse.
- Expected: that save loads and the title menu closes.
- Select a populated manual save and press Enter, Interact, or the gamepad confirm button.
- Expected: that save loads and the title menu closes.
- Select a populated manual save's Delete button by keyboard/gamepad or click it with the mouse.
- Expected: the save is removed from the list.
- Delete all manual saves and return to the title menu.
- Expected: Load Quest is hidden.

### [x] In-Game Main Menu And Quit Flow

- Open the in-game menu while playing.
- Expected: Main Menu and Quit buttons appear in the header.
- Select Main Menu and confirm.
- Expected: the confirmation buttons are side by side and labelled `OK` and `Back`.
- Expected: the game returns to the black-background title menu.
- Select Quit and confirm.
- Expected: the confirmation buttons are side by side and labelled `Quit` and `Back`.
- Expected: built player exits. In the editor, Unity may ignore `Application.Quit()`.

### [x] Title Controls Block Gameplay And Support Keyboard/Gamepad

- Start Play Mode or launch a built player.
- Expected: a `Dungeon Escape` title window appears before map controls are usable.
- Try moving while the title window is open.
- Expected: the player does not move.
- Choose New Quest.
- Expected: the title window closes, a fresh party starts at the map default spawn, and map controls work.
- Save from the in-game Save tab, then return to the title flow by restarting Play Mode or the built player.
- Choose Load Quest.
- Expected: manual saves are listed with save time and level.
- Select a populated manual save.
- Expected: the title window closes and the saved map, position, party, inventory, quest, and object state are restored.
- Create or wait for a quick save, then restart.
- Choose Continue.
- Expected: the title window closes and the quick save is loaded.
- Use keyboard or gamepad up/down and interact on the Main Menu, Load Quest, and Create Quest screens.
- Expected: every actionable item can be selected visibly and activated.
- Choose Quit in a built player.
- Expected: the player application exits.

### [x] Unity Save Version Policy Archives Unsupported Saves

- Locate the Unity save file under `%APPDATA%/Redpoint/DungeonEscape/save.json`.
- Back up the file manually before testing this case.
- Edit the save JSON `Version` value to an unsupported value such as `0.0`.
- Start Play Mode or a built player.
- Expected: the game logs that the save version is unsupported.
- Expected: the original save is copied to a file named like `save.unsupported-0.0-YYYYMMDDHHMMSS.json`.
- Expected: the game creates a fresh Unity save file instead of trying to migrate old or unsupported save data.

### [x] Autosave And Transition Save Policy

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

## Build And Runtime Packaging

### [x] Unity Build And Run Loads Runtime Assets

- Open Unity and wait for scripts to finish compiling.
- Open `File > Build Settings`.
- Expected: `Assets/DungeonEscape/Scenes/Boot.unity` is listed and checked.
- Use `File > Build And Run`.
- Expected: the built Windows player opens to the same map view as Play Mode, not only the debug window.
- Expected: terrain, objects, NPC sprites, player sprites, item icons, and UI images are visible.
- Move, warp to another map, and open the inventory.
- Expected: map TMX files, TSX files, tileset images, character images, and item images continue loading in the built player.
- Result: Confirmed. `File > Build And Run` renders the map correctly outside the editor.

## Audio

### [x] Startup Music Plays Through Splash And Title

- Start Play Mode or launch a built player with `SkipSplashAndLoadQuickSave` disabled.
- Expected: `first-story` music starts while the splash screen is shown.
- Wait for the title menu.
- Expected: the same music continues on the title menu without restarting repeatedly.

### [x] Map Music Follows TMX Song Property

- Start or load a quest and enter the overworld.
- Expected: overworld music plays from the map `song` property.
- Warp into a town, dungeon, tower, or shrine with a different `song` property.
- Expected: the music changes to that map's configured song.
- Warp back to the previous map.
- Expected: the previous map music resumes.

### [x] Biome Music Hook Does Not Interrupt Normal Movement

- Move across the overworld and watch the Unity Console.
- Expected: no missing-audio warnings appear while crossing grassland, forest, hills, swamp, or water.
- Enter a map or biome identified as town, cave, tower, or desert.
- Expected: the configured biome music may play, and repeated steps on the same biome do not restart the same track.

### [x] Sound Effect Volume Controls Gameplay Effects

- Open Settings > General.
- Set Sound Effects Volume to `0.00`.
- Open a chest, warp, trigger dialog, step on damage terrain, and trigger a level-up if available.
- Expected: sound effects are muted.
- Set Sound Effects Volume to `1.00` and repeat the available actions.
- Expected: chest, warp, dialog, damage, and level-up effects are audible.

### [x] Music Volume Controls Current Track

- Open Settings > General while music is playing.
- Set Music Volume to `0.00`.
- Expected: music becomes silent without stopping the game.
- Set Music Volume to `1.00`.
- Expected: the currently playing track becomes audible again.

## Encounter And Combat

### [ ] Random Encounter Logs Monsters By Biome

- Start or load a quest and walk around the overworld across grassland, forest, hills, swamp, or water.
- Expected: random encounters occasionally log to the Unity Console in the format `Random encounter in <Biome> on <MapId>: <monster list>`.
- Walk inside a dungeon, tower, tunnel, shrine, or other map with a copied `*_monsters.json` table.
- Expected: random encounters use that map's monster table instead of the overworld biome monster list.
- Keep walking after an encounter log appears.
- Expected: no combat UI opens yet; this first pass only logs selected monsters.
- If no logs appear after many steps, confirm `NoMonsters` is `false` in the settings file.
