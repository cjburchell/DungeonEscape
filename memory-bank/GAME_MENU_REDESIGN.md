# Game Menu Redesign

This document tracks the in-game menu redesign that replaces the old tab-first menu with a keyboard/gamepad-first action flow.

## Goal

Make the game menu usable primarily with keyboard and gamepad. Mouse support remains secondary.

The menu should open from the bound `Menu` command, default `E`, and present a simple vertical action list rather than a row of tabs.

## Current Flow

### Top-Level Menu

- Status summary is shown at the top, matching the map status window style.
- Gold is shown at the bottom, matching the map gold window style.
- Main actions:
  - Items
  - Spells, only shown when at least one party member has a usable map spell.
  - Equipment
  - Abilities, only shown when at least one party member has a usable map ability.
  - Status
  - Party, only shown when there is more than one party member.
  - Misc.

Navigation:

- Up/down moves through the current visible list.
- Gamepad D-pad, left stick, and right stick should all be supported where normal menu navigation is supported.
- Action selects the highlighted row.
- Cancel backs out one level, or closes the menu from the top-level menu.
- `[` / `]` page through paged item/spell/ability/equipment lists.

## Implemented

- Top-level action list replaces the old visible tab row.
- Party status summary appears at the top of the menu.
- Gold summary appears at the bottom of the menu.
- Items screen:
  - Left: party members.
  - Middle: selected member's items.
  - Right: selected item details.
  - Action from member list moves focus to item list.
  - Action from item list opens the item action modal.
- Spells screen:
  - Left: party members who have usable map spells.
  - Middle: selected member's usable map spells.
  - Right: selected spell details.
  - Action from spell list casts/opens the existing target flow.
- Abilities screen:
  - Left: party members who have usable map abilities.
  - Middle: selected member's usable map abilities.
  - Right: selected ability details.
- Equipment screen:
  - Left: party members.
  - Middle: equipment slots with each equipped item name and icon.
  - Right: compatible equipment for the selected slot.
  - Action from equipment slot moves focus to compatible equipment and selects the currently equipped item when present.
  - Action on the currently equipped item unequips it; action on a different compatible item equips it.
- Status screen:
  - Left: party members.
  - Right: detailed status.
  - HP and MP use the same progress-bar styling as other status values.
- Party screen:
  - Left: party members.
  - Action opens existing party action modal for move/reserve/add.
- Misc. screen:
  - Save
  - Load
  - Settings
  - Exit to Main
  - Quit
- Save, Load, and Settings reuse existing behavior.
- Menu modal confirm-release guard remains active to avoid action carryover.

## Pending Polish

- Verify all screens in Play Mode with keyboard and gamepad.
- Confirm page state resets correctly when switching members and screens.
- Confirm long item/spell/ability names fit without clipping.
- Confirm the status and gold summaries fit at all supported UI scale values.
- Update README keybind/menu instructions after the new flow is play-tested.

## Manual Tests

The primary manual test is tracked in:

- `memory-bank/MANUAL_TESTS.md`

Section:

- `Redesigned Game Menu Action Flow`
