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
| `itemdef.json` | `itemdef.schema.json` |
| `quests.json` | `quests.schema.json` |
| `dialog.json` | `dialog.schema.json` |
| `classlevels.json` | `classlevels.schema.json` |
| `statnames.json` | `statnames.schema.json` |
| `names.json` | `names.schema.json` |
| `Data/maps/**/*_monsters.json` | `map-monsters.schema.json` |

`dungeonescape-data.schema.json` contains the shared definitions used by the
per-file schemas. Cross-file reference checks, image checks, and map metadata
rules are still handled by the Game Editor validation panel because JSON Schema
cannot easily validate those against the live loaded project.