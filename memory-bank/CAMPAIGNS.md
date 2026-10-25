# Campaigns And Stable Content IDs

Investigation: 2026-10-24. Implementation started with stable monster and encounter IDs. The remaining campaign design below is proposed.

## Requirements

- New Quest selects a campaign before character creation when multiple campaigns are available. With one campaign, select it automatically. With none, explain why a new game cannot start.
- Each campaign owns its maps, quests, and dialog trees.
- The Game Editor can create campaign-specific monsters, NPCs, and items alongside reusable base content.
- Display-name changes must not change identity or break references.

## Existing Implementation

- `DungeonEscape.Core/Data/DungeonEscapeDataSet.cs` represents one content set; `Link()` resolves item/spell skill references.
- Quests, dialogs, and items already expose `Id`. Monsters expose `Name` without a definition ID. `RandomMonster.Name` and `EncounterRules` resolve monsters by name.
- `GameDataCache.TryGetCustomItem` accepts either ID or name. `DataSourceCatalog` supplies names for monster/item/spell/skill dropdowns. Adding IDs alone will not remove name coupling.
- NPC behavior, recruitment metadata, sprites, and dialog references currently live on TMX objects. There is no reusable NPC definition catalog in the inspected core data contracts.
- `DataFolderService` loads and saves a single flat Data folder. `AssetContext` assumes shared Data, Maps, Tilesets, Images, and Audio directories.
- `Bootstrap` wires the existing data assets and initial map. `GameState` contains literal `overworld` checks for travel, return positions, encounters, and transition saves. These need campaign-aware behavior, not just a campaign dropdown.
- Runtime files are staged into StreamingAssets; campaign manifests, maps, data, and dependent assets must join that packaging path.

## Recommended Content Model

Load shared base definitions plus exactly one selected campaign into a resolved content context. Keep that context separate from mutable party/save state. Runtime and editor should share the resolver and validation rules in Core.

Proposed layout beneath `Assets/DungeonEscape/`:

```text
Content/
  Base/
    Data/                 # reusable monsters/items/spells/skills and character rules
  Campaigns/
    original/
      campaign.json
      Data/               # quests, dialog, NPCs, custom monsters/items, encounter tables
      Maps/
      Images/             # optional campaign assets
      Tilesets/           # optional campaign assets
      Audio/              # optional campaign assets
```

Shared visual assets can stay in their existing directories initially. Resolve each campaign's explicit asset references against known content roots; do not concatenate IDs into file paths. Retain valid TMX/TSX relative asset links when moving maps.

The campaign manifest needs a schema version, immutable campaign ID, display name, description, content version, starting map ID/spawn ID, designated overworld map ID, and explicit map-ID-to-file mappings. Map filenames and display names can then change independently of map identity. Initially support the current single-overworld gameplay model per campaign.

Base content contains reusable definitions, not the current world's quests or maps. Convert the existing adventure into the first campaign. Load campaign-owned narrative/maps plus base definitions and campaign-specific additions. Do not load another campaign's definitions or implicitly override a base ID. To customize a base monster, duplicate it into a new campaign ID. Campaign encounter tables must explicitly select the available roster; base monsters must not all enter encounters automatically.

## Stable IDs

Use permanent strings with the shape `<kind>/<scope>/<key>`:

| Definition | Example |
| --- | --- |
| Shared monster | `monster/base/001` |
| Campaign monster | `monster/original/crypt-sentinel` |
| Shared item | `item/base/longsword` |
| Campaign NPC | `npc/original/harbor-master` |
| Campaign quest | `quest/original/find-the-ship` |
| Campaign dialog | `dialog/original/harbor-master` |
| Campaign map | `map/original/coast` |

`base` is reserved. Campaign scope uses an immutable campaign key, never its display name. Keys may be numbers or descriptive slugs, but remain unchanged after creation even if their wording becomes outdated. Generate an ID once when adding content; never regenerate it from `Name` on save. Routine editor forms show IDs read-only with a copy action. Duplication creates a fresh ID.

Use canonical lowercase ASCII IDs with ordinal comparison and a defined character grammar. Reject missing IDs, duplicates, wrong kinds/scopes, and references outside the active base-plus-campaign context. Show `Name` in selectors, store `Id`, and show scope/ID to distinguish identical names. Do not interpret these strings as filesystem paths.

Extend this consistently to spell/skill references, prepared/known spell state, rewards, drops, stores, starting equipment, quest conditions, dialog actions, NPC references, encounter tables, warps, and saved map locations. Audit name-based gameplay special cases separately: a display rename must not silently change a spell's behavior. Existing explicit behavior fields/enums should remain authoritative.

Definition identity and instance identity are different: multiple placed NPCs can share an NPC definition, while each placement needs its own persistent identity for recruitment/removal state. Map objects need stable placement IDs scoped by map; layer-local Tiled object numbers must not collide in save keys. Item instances retain their own mutable state and definition ID; a catalog ID alone does not identify one owned sword.

## NPC Authoring

Add `NpcDefinition` for reusable name, appearance, dialog, role/service metadata, and optional recruit character data. TMX objects reference `NpcId` and retain placement, facing, and persistent instance identity. Start with ordinary talking NPCs and recruitable NPCs; preserve existing specialized service object behavior until migrated deliberately. Define any allowed placement overrides explicitly rather than copying whole NPC records into every map.

The editor needs campaign create/open/select, manifest/start-location editing, campaign NPC authoring, and base/campaign provenance in existing tabs. Base definitions should be read-only while editing a campaign, with a duplicate-to-campaign action. Saving a campaign writes its owned files only. Reference validation, map previews, image catalogs, Quest Graph, Encounter Simulator, and JSON schemas all need the same selected context.

## Saves And Campaign Switching

Persist save schema version, campaign ID, and campaign content version. Load the required campaign context before resolving party references or loading its map. Load/Continue uses the saved campaign, never the last New Quest selection. Save summaries identify the campaign. A missing or incompatible campaign produces a clear load error without overwriting the save or falling back to another world.

Build and validate a replacement context before switching it into the runtime. On new game/load, clear campaign-dependent map, encounter, shop, dialog, and asset caches; canceling campaign selection must not mutate an existing game. Manual saves can remain in a shared list with campaign labels, but quick/autosaves should be partitioned by campaign to prevent one adventure replacing another's progress.

User direction: do not implement old-save migration for the content ID conversion. Save version bumps use the existing unsupported-version archive policy. Do not retain one-time conversion scripts or name-based runtime fallback. Content version remains diagnostic metadata until campaign compatibility policy is implemented.

## Implementation Sequence

1. **Stable IDs and migration tooling.** Inventory reference paths, add missing definition IDs, convert checked-in JSON/TMX and runtime/editor lookups, update schemas, and produce a dry-run migration report with duplicate/unresolved references. Preserve existing quest/dialog/item IDs through an explicit mapping if namespacing them. Fail migration on ambiguous names. Remove runtime name fallback after supported migrations exist.
2. **Campaign loading and original-content conversion.** Add manifest/context/resolver and campaign map registry, split base from original adventure, replace hard-coded overworld behavior, and stage all content/dependencies into builds. Keep existing gameplay working with the single original campaign.
3. **Campaign persistence and New Quest selection.** Add campaign-aware save metadata/migration and cache lifecycle, then add selection before the existing character flow. Support keyboard/gamepad and cancellation. Verify Continue/load across campaigns.
4. **Campaign editor and NPC definitions.** Add create/open/save ownership rules, NPC catalog/reference editing, custom monster/item assets, scope-aware dropdowns, graph/simulator context, and validation.
5. **Second small test campaign.** Add a separate start map, quest/dialog, NPC, monster, and item, including duplicate display names across scopes. Use it to prove content and save isolation in editor and a packaged game.

These are proposed work packages, not completed features. Stable IDs should land before mass campaign authoring to avoid rewriting new references twice.

## Acceptance And Validation

- Renaming referenced monsters, items, spells/skills, NPCs, quests, dialogs, maps, or campaign display names leaves behavior/references intact.
- Same-name definitions remain selectable and resolve to their exact IDs. Duplicate IDs and cross-campaign references fail validation.
- Zero/one/multiple-campaign title flows behave as specified; cancellation preserves current state and input navigation works.
- A second campaign uses only its narrative/maps and configured encounters. Base content is available without modifying it; campaign saves cannot write base files.
- Start/warp/Outside/Return/Wings, overworld return positions, random encounters, map object persistence, recruitment, and transition saves work independently for both campaigns.
- Campaign A save loads correctly after playing B; missing campaigns and failed migrations do not corrupt saves. Quick/autosave ownership is verified.
- Custom sprites, map tilesets, music, and data load in a Windows build as well as the editor.
- Run Core tests for resolver/IDs/migrations/state/view models and solution build for tool changes. Run Unity Edit Mode and Play Mode tests using Unity `6000.4.4f1` for runtime implementation; update MANUAL_TESTS with implemented workflows. This investigation changes documentation only, so no runtime tests are required yet.

## Implementation Progress (2026-10-24)

- Added permanent Monster.Id values (name-derived slugs such as `monster/base/sea-ooze`) for the 50 existing definitions. Encounter tables now use MonsterId and ordinal ID resolution; encounter group uniqueness and simulator aggregation use definition IDs.
- Updated editor monster selectors to display name plus ID, retain ID on rename, assign fresh IDs on add/duplicate, and validate missing/duplicate/malformed IDs and missing references. Monster IDs are read-only in the form. Schemas reflect the new contract.
- Completed the one-time conversion of all 50 monsters and 13 map encounter tables. Removed the temporary migration script and legacy lookup table after conversion; only the migrated data is retained. Runtime does not fall back to monster names.
- Added regression tests for rename-safe resolution, identical display names, and clearing stale resolved references.
- Validation: solution build passed with zero warnings/errors, Core 218/218, Unity Edit Mode 16/16. Unity Play Mode 10/10 after updating a stale combat target-selection test to the current fight entry point. All 51 checked-in map encounter references resolve; migration rerun reports zero changes.
- Historical remaining phase 1 after the monster pass: spell/item/narrative/map identity and shared validation. See the later progress entries below for completed follow-up work. Object placement identity and reusable NPC definitions still need work. Campaign manifests, selection, NPC catalogs, and save isolation are not implemented yet.

- Follow-up: replaced initial numeric IDs with slugs based on current monster names, updating all encounter references. IDs remain permanent after this one-time conversion; subsequent display-name edits do not regenerate them.

## Skill Reference IDs (2026-10-24)

- Monster foundation was committed and pushed to origin/main as `9463052`.
- Converted all 28 skill definitions to permanent `skill/base/<current-name-slug>` IDs and all authored spell/item effect references to those IDs. Runtime Setup and hero action lookup resolve IDs only; Rogue Sleight of Hand grants, lock checks, and skill icon lookup use IDs.
- Editor skill forms show read-only IDs, add/duplicate assign fresh IDs, effect selectors display names plus IDs, and validation/schema checks require valid unique IDs. Duplicate display names are allowed for skills.
- Added regression coverage for identical names, rename-safe item/spell/hero resolution, stale link clearing, and every checked-in spell/item effect reference.
- User explicitly declined save compatibility work. No migration scripts or runtime save migration were retained; save format is now 1.1 and the existing unsupported-version archive policy applies to 1.0 saves.
- Validation: solution build passed with zero warnings/errors; Core 221/221; Unity Edit Mode 16/16; Unity Play Mode 10/10.
- Spell definition/preparation IDs and item definition references remain the next portions of phase 1, followed by map/NPC identity and campaign loading.

## Consistent Content IDs (2026-10-24)

- Standardized 58 unique item definitions, 26 spells, 3 quests, 24 dialogs, 10 backgrounds, and 23 maps using `<kind>/<scope>/<key>`, alongside the existing monster/skill IDs. Reusable definitions use `base`; maps, narrative, and quest items use `original`.
- Converted authored JSON/TMX references together. Spell preparation, monster spell references, item rewards/consumption, ship ownership, item icons, background selection/defaults, and starting equipment use explicit definition IDs. D&D spell behavior metadata stays separate from display names.
- Added shared `ContentId` validation/creation and `MapDefinition`/`MapCatalog`. `Data/maps.json` explicitly maps each stable map ID to a display name, TMX asset path, and encounter asset path. Runtime and Game Editor resolve that registry; moving/renaming files requires updating paths there without changing IDs or warp references. Filesystem-path inputs remain supported internally for Bootstrap/map previews.
- Editor definition IDs are read-only; add/duplicate creates fresh namespaced IDs. Item/skill selectors display names plus IDs. Validation and JSON schemas enforce the format. Item catalogs merge by definition ID, preserving the existing overlapping catalog arrangement.
- Map files, spawn IDs, Tiled object numbers, item instance GUIDs, Unity GUIDs, sprite/tile IDs, class/species enums, and D&D skill proficiency labels retain their distinct purposes. Reusable NPC definitions and stable map-placement identity are future campaign work.
- Save version is now 1.2. Old versions follow the existing archive policy; no save migration code or one-time migration tools are retained, per user direction.
- Added content graph validation for every authored JSON/TMX reference, map registration/file coverage, map rename/path/duplicate checks, and item/prepared-spell rename/identical-name regression coverage.
- Validation: solution build zero warnings/errors; Core 234/234; Unity Edit Mode 17/17; Unity Play Mode 10/10 after updating the expected overworld ID in the title-flow test. Packaged Windows verification and the listed manual authoring/gameplay checks remain unperformed.
- Next campaign step: add campaign manifest/context and split the original adventure from base definitions. Map/NPC placement identity remains a separate follow-up.
