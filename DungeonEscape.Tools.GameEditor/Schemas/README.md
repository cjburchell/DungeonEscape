# Dungeon Escape JSON Schemas

These schemas document the JSON files edited by the standalone Game Editor.
They are intentionally kept in the tool project rather than the runtime Data
folder so Unity does not import them as game content.

Use the per-file schemas for editor validation or future CI checks:

| Data file | Schema |
| --- | --- |
| `allmonsters.json` | `allmonsters.schema.json` |
| `spells.json` | `spells.schema.json` |
| `skills.json` | `skills.schema.json` |
| `customitems.json` | `customitems.schema.json` |
| `magicitems.json` | `customitems.schema.json` |
| `nonmagicitems.json` | `customitems.schema.json` |
| `weaponitems.json` | `customitems.schema.json` |
| `armoritems.json` | `customitems.schema.json` |
| `campaigns.json` | `campaigns.schema.json` |
| `Campaigns/*/campaign.json` | `campaign.schema.json` |
| `Campaigns/*/Data/maps.json` | `maps.schema.json` |
| `Campaigns/*/Data/quests.json` | `quests.schema.json` |
| `Campaigns/*/Data/dialog.json` | `dialog.schema.json` |
| `class.json` | `class.schema.json` |
| `backgrounds.json` | `backgrounds.schema.json` |
| `names.json` | `names.schema.json` |
| `Campaigns/*/Data/Encounters/**/*_monsters.json` | `map-monsters.schema.json` |

`dungeonescape-data.schema.json` contains the shared definitions used by the
per-file schemas. Cross-file reference checks, image checks, and map metadata
rules are still handled by the Game Editor validation panel because JSON Schema
cannot easily validate those against the live loaded project.

Campaign-specific items and monsters use the existing item/monster schemas; their IDs must use the selected campaign scope. Shared library files remain under Data/.
