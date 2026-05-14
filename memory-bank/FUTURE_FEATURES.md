# Future Features

This file tracks post-migration feature and architecture ideas. Items here are not committed implementation plans until they are promoted into active work.

## Gameplay And Content

- Add more quests.
- Create a basic storyline.
- Grow the world map with more places.
- Add follower quest types.
- Add random dungeon generation.
- Tune random monster encounters.
- Update biomes.
- Improve the quest journal with current objective, last clue, known destination, and completed quest history.
- Add multi-step quest chains with branching outcomes.
- Add NPC relationship, reputation, town, or faction flags.
- Add world events triggered by quest progress, such as changed NPCs, dialog, routes, or encounters.
- Add a rumor system in towns to point players toward secrets, dungeons, quest hints, or landmarks.
- Add discoverable landmarks and region names for the overworld.
- Add optional treasure maps or clue items that point to rough search areas instead of exact tiles.
- Add fast-travel restrictions based on story progress, danger level, or discovered locations.
- Add secret doors, hidden passages, and map notes.
- Add a day/night cycle.
- Add time-of-day dependent NPC schedules, store hours, quests, events, and town dialog.
- Add time-of-day dependent world state, such as different music, lighting, encounters, and blocked or opened routes.

## Inventory And Items

- Revamp inventory so the party has one shared inventory instead of each party member owning a separate inventory.
- Show which classes can equip an item in the item UI.
- Keep equipment member-specific even if general inventory becomes party-shared.
- Add item comparison UI for equipment upgrades.
- Add item tags and filters for weapons, armor, quest items, consumables, keys, and valuables.
- Add shops with limited stock, restocks, town-specific goods, and price modifiers.
- Consider crafting or item upgrading if it fits the old-school RPG feel.

## Tools And Data Editing

- Create editors for JSON data files:
  - Monsters.
  - Spells.
  - Skills.
  - Items.
  - Dialogs.
  - Quests.
- Add JSON validation schemas for items, spells, skills, quests, dialogs, and monsters.
- Add in-editor data validation for missing item IDs, broken dialog links, invalid quest stages, bad map references, and missing assets.
- Add a quest/dialog graph viewer before or alongside full editors.
- Add an encounter simulator for testing random monster tables and reward pacing.
- Add a map validator for warps, spawn IDs, object classes, locked-door/chest metadata, and missing assets.

## Map And Exploration UI

- Add a minimap screen.
- Add fog to selected maps so the player only sees areas that have been uncovered.
- Update the `Return` and `Wings` location-selection UI to show a map of the overworld.
- Add a larger map screen with discovered towns, dungeons, return points, and optional quest markers.
- Show day/night state and time-of-day on the map or HUD if a time system is added.
- Add a bestiary unlocked as monsters are encountered or defeated.
- Add better save summaries with party level, location name, play time, and quest progress.
- Add an on-screen recent message log.
- Add accessibility settings such as text speed, larger UI scale presets, and colorblind-safe status colors.

## Visual Assets

- Replace ripped or placeholder graphics with original, licensed, or generated assets.
- Update monster sprites.
- Update NPC sprites.
- Update party member sprites.
- Update map tiles.
- Update item icons.
- Update spell icons.
- Define a consistent art direction, palette, tile size, and sprite scale before replacing assets broadly.
- Track asset provenance and licensing so future builds are safe to publish.

## Dungeons

- Prefer procedural dungeon templates over fully random generation so random dungeons still feel authored.
- Add dungeon floors with keys, switches, locked rooms, traps, and shortcuts.
- Add boss rooms and mini-boss encounters.
- Add light or darkness mechanics for selected dungeons, especially if fog-of-war is added.
- Add dungeon-specific objectives such as rescue an NPC, recover an artifact, seal a portal, or clear a monster nest.

## Combat And Monsters

- Add monster families with behavior differences, not just stat differences.
- Add rare monster variants with better drops.
- Add encounter zones with spawn weights by biome, time, quest state, or party level.
- Add night-only or day-only monsters and encounter tables.
- Add boss abilities with visible round-based patterns.
- Add more status effects that matter outside combat too, such as poison, curse, fatigue, or blessing.
- Add battle tactics for party members.
- Allow each party member to choose normal manual control or an automatic tactic stance.
- Suggested tactic stances: aggressive, balanced, and defensive.
- Let tactic stance influence automatic combat choices, target priority, item/spell use, and defensive behavior.

## Party And Classes

- Improve class identity with passive traits, class-specific actions, or unique equipment perks.
- Add a class preview in character creation showing equipment, spells, and role.
- Make party formation or marching order affect targeting, traps, or surprise attacks.
- Add companion or follower loyalty quests.
- Add reserve party member activities, such as training, crafting, or gathering while inactive.

## Notes

- No older future-feature document was found in the current project-owned markdown files.
- Vendor/package markdown and generated Unity cache files were not treated as project planning docs.
