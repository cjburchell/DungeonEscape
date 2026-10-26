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

Campaign manifests optionally set `MaxStartingPartyMembers` (integer >= 1, default 1). New Quest allows up to that limit, also capped by the game's active party capacity.

## Static map encounters

Place a tile object with `class="Monster"` in Tiled, using an existing sprite `gid`. The Maps object form exposes these properties:

| Property | Meaning |
| --- | --- |
| `EncounterId` | Permanent `encounter/<campaign-scope>/<key>` identity, unique across all object layers in this map. Never regenerate it when renaming or renumbering objects. |
| `MonsterId` | A shared or selected-campaign monster definition ID. |
| `Count` | Exact number of opponents, 1–10; random group-size and level filtering do not apply. |
| `VictoryQuestId`, `VictoryQuestStage` | Optional pair; advance this quest stage only after victory. Omit both for a standalone encounter. |
| `Dialog` | Optional branching approach conversation. A choice with `Actions: ["Fight"]` starts this placed group. Fight uses the group's roster and victory fields; give rewards or advance stages in its victory outcome rather than on the Fight choice. |
| `Text` | Approach text when no Dialog is supplied; runtime supplies Fight and Leave choices. |
| `Collideable` | Whether the group's authored footprint blocks movement. |

The renderer arranges Count sprites within the object's bounds. For four 32px figures use a 64×64 tile object; all four share one interaction, encounter identity and defeated state. Victory removes both art and collision, and persists in map state. Leaving, player escape, defeat or cancellation do not complete the encounter. Re-entry starts a fresh full roster. Every placed group has independent state, even if its monster definition is shared.

Runtime campaign loading and Game Editor validation reject duplicate identities, counts outside the supported range, missing monsters and invalid victory quest/stage references. These are XML map metadata rules rather than a separate JSON file.

The registered `borderlands` prototype demonstrates a warden quest and a four-kobold cave group using existing game art. It is an initial quest prototype, not a complete conversion of the published adventure.
