# Bug Backlog

This file tracks bugs and rough edges to investigate later. Items are not triaged unless a priority is listed.

## UI Input And Navigation

- Player selection screen gender dropdown: clicking a gender selection with the mouse also activates the dropdown underneath it.
- Player selection screen name text edit: when the name field is focused, pressing `W`, `A`, `S`, or `D` types into the editor and also moves/selects another UI button/control. Investigate UI input focus handling so text entry captures movement keys.
- Party status window: do not show abilities for a party member when that party member has no abilities.

## Map Movement And NPCs

- NPCs can block doors because the player cannot walk through NPCs, which can trap the player inside buildings.

## Quests

- Finished quests sometimes restart. Known example: `Lost_Sea_Shell`.

## Stores

- Stores should keep a couple common items always in stock.

## Exceptions

- Intermittent `NullReferenceException` when using an item:

```text
NullReferenceException: Object reference not set to an instance of an object
Redpoint.DungeonEscape.Unity.UI.GameMenu.DrawMenuModalOverlay () (at Assets/DungeonEscape/Scripts/Unity/UI/GameMenu.Modals.cs:158)
Redpoint.DungeonEscape.Unity.UI.GameMenu.OnGUI () (at Assets/DungeonEscape/Scripts/Unity/UI/GameMenu.cs:280)
```
