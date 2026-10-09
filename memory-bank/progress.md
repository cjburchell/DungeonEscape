# Progress

## Done

- Created Unity project structure and imported Unity-compatible assets.
- Created `DungeonEscape.Core` shared project.
- Migrated major portable state/domain models into shared core.
- Built Unity map loading/rendering from TMX/TSX files.
- Implemented map layer ordering and sprite/player sorting.
- Implemented movement, collision, continuous movement, sprint, water/ship rules, biome/damage data layers.
- Implemented map warps, default spawns, overworld return fallback, and fade transitions.
- Implemented chests, hidden items, opened doors, removed objects, object persistence, and save/load behavior.
- Implemented quest/dialog item give/take/progression paths for known starter quests.
- Implemented party creation, starter equipment, recruitment, followers, cart visual, coffin visual, party animations.
- Implemented persisted hero sprite selection independent from class/gender, including New Quest image selection and map-gid-based recruit sprites.
- Implemented party, inventory, quest, settings, save, store, healer, title/load UI.
- Implemented configurable UI scale/style and gamepad/keyboard input rebinding.
- Implemented `Outside`, `Return`, `Wings`, and `Open` map-mode behavior.
- Implemented combat target selection through displayed monster sprites for enemy targets and the always-visible party status window for party targets.
- Recreated the old splash screen in Unity and hid the map behind a black startup/title backdrop.
- Added hidden fast-start setting to skip splash/title and load the quick save for testing.
- Added title New Quest create-player flow with random names/dropdowns/portrait/stats/re-roll, variable manual-save load/delete, and in-game Main Menu/Quit actions.
- Added GitLab CI solution build/test and Unity validation/build artifact support.
- Added memory-bank docs.
- Removed the old `DungeonEscape.Test` project from the solution; migration tests should live in `DungeonEscape.Core.Test` or future Unity test assemblies.
- Removed the old MonoGame/Nez project and `Nez.Portable` from this branch; use `main` if old implementation reference is needed.
- Unity migration is considered complete; remaining automation and warning-review items are accepted as post-migration backlog.
- First core extraction completed: Tiled map path helpers, tile data parsing, and object bounds tile math now live in `DungeonEscape.Core` with unit tests.
- Save/location formatting extraction completed: save title/summary, usable-save checks, and return-location display names now live in `DungeonEscape.Core` with unit tests.
- Store/economy extraction completed: store metadata, inventory selection, buy/sell rules, sale prices, and sellable item filtering now live in `DungeonEscape.Core` with unit tests.
- Core data/state split completed: file-backed data contracts and parsed Tiled map contracts now live under `DungeonEscape.Core/Data`; runtime/save objects remain under `State`.
- Random item, quest progression/reward, and encounter generation extractions completed with focused core tests.
- Combat round rules extraction completed: action choice, target resolution/fallback, run outcomes, and execution dispatch now live in core with tests.
- UI drawing/logic split started with `StoreViewModel` in the core `ViewModels` namespace; store selection, filtering, metadata, and price decisions have matching core tests.
- `HealerViewModel` added under the core `ViewModels` namespace; healer service availability, costs, target filtering, metadata, and selection state have matching core tests.
- `TitleViewModel` and `GameMenuViewModel` added under the core `ViewModels` namespace; title navigation/create state, save-slot display rows, game-menu screen state, selection clamping, action availability, member filtering, row counts, detail counts, equipment candidate selection, item/spell use routing, item action labels, modal state, and settings adjustment/change effects have matching core tests.
- `CombatViewModel` added under the core `ViewModels` namespace; combat UI state, selected-index movement, action/menu display rows, spell/item labels, selected target lookup, and target candidate/type checks have matching core tests.
- Future feature backlog has been captured in `memory-bank/FUTURE_FEATURES.md`.
- Known bugs and rough edges are tracked in `memory-bank/BUGS.md`.
- The planned UI Toolkit migration was abandoned; the active direction is to keep the current IMGUI runtime UI and improve it incrementally.
- Added first-pass Unity UI Play Mode regression coverage for boot/runtime roots, title opening, create-game flow, and game-menu open/close behavior, plus a shared Play Mode helper for scene/object/reflection setup.
- Expanded Unity UI Play Mode regression coverage to combat open/message/autosave blocking, action-selection transition, target-selection return-to-action behavior, and combat close cleanup.
- Removed stale Toolkit-oriented Unity UI tests that no longer match the active IMGUI direction.
- Added `DungeonEscape.Tools.GameEditor`, a standalone Photino.Blazor desktop tool for editing monster JSON files (open/new/save any monster array file, searchable list with image thumbnails, add/duplicate/remove, and a full property editor with dropdowns for image/rarity/biomes/spells/skills/items). It references `DungeonEscape.Core` so saved JSON matches the game format.
- Game Editor: consolidated New/Open/Save/Save As into a single **File** dropdown menu, and added an `EditorSettingsService` that persists the last opened/saved file (`%AppData%/DungeonEscape.GameEditor/settings.json`) and **auto-loads it on startup**.
- Data Editor: expanded the Photino tool into a full Data-folder editor covering monsters, spells, skills, static item catalogs, quests, dialogs, class levels, and names. Added a collapsible validation panel for duplicate identifiers, required name/id checks, broken cross-references, invalid image IDs, missing class-level definitions, and dialog nesting/reference issues.
- Data Editor/Core data contracts: class entries and spell/item class references now use string class names from `class.json`; runtime hero state still uses the existing `Class` enum and compares against those strings by name.
- Data Editor: renamed the class-level tab to **Class** and made class stats a fixed normalized list of HP, Attack, Defence, MagicDefence, Agility, and Magic rather than add/remove rows.
- Data Editor: the Class tab now exposes D&D-facing class metadata, default hero image, skill proficiencies, skill options, and skill choice count from `class.json`; hidden legacy class stat rows and `classlevels.json` have been removed.
- Data Editor: dialog choice `NextQuestStage` is now shown only when an effective quest is available, uses a dropdown of that quest's stages, and treats `0` as none.
- Data Editor: save output is now sparse JSON, omitting null/default values, zeroes, `false`, empty strings, empty arrays, and empty objects while keeping root files valid.
- Data Editor/Unity title flow: class definitions now support a default hero-sheet image index, the Class editor exposes a visual picker for it, and New Quest class selection applies that default image automatically.
- Data Editor: added a **Maps** tab that auto-detects Unity TMX maps from the opened Data folder's asset root and edits gameplay metadata without taking over Tiled-owned layout/display data. It supports map properties, object `name`/`class`, friendly NPC/chest/door/warp property forms, and per-map random monster JSON files under `Data/maps/{mapId}_monsters.json`; overworld encounters remain biome-driven from monster data. The map root `class` is edited as an Overworld checkbox only: checked saves `class="Overworld"`, unchecked saves no map class. Advanced raw TMX property add/remove editors are hidden to avoid unsupported map metadata edits. Spawn objects only expose `DefaultSpawn`; warp objects only expose `WarpMap` and `SpawnId`, with empty `SpawnId` labeled as the target map's default spawn. Chest/hidden item forms hide unused `MoveRadius` and the legacy `Gold` fallback; runtime still supports `Gold` when no `ItemId` is present, but current maps use `ItemId=#Random#`, which can generate gold. Integer-valued map metadata uses number inputs while saving as TMX property strings. Map validation now covers unsupported map root classes, broken dialog/item/key/warp/class/monster references, duplicate TMX object ids within the same object layer, and missing explicit chest/door lock metadata. Map object/dialog reference fields now use shared styled selects, with `WarpMap`/dialog `MapId` linked to target-map `SpawnId` options while preserving unknown custom values. The Maps tab object selector uses a fixed-height scrolling list so it does not grow with large object counts. Item and monster reference dropdowns show thumbnails when possible.
- Roadmap cleanup: Game Editor coverage, in-editor validation, and map validator work have been moved out of the open future-feature list as completed/mostly completed. Remaining tool backlog now focuses on polish, JSON schemas, graph/simulator tooling, missing-asset validation, and future metadata rules.
- Data Editor tools: added checked-in JSON Schemas under `DungeonEscape.Tools.GameEditor/Schemas/`; added read-only **Quest Graph** and **Encounter Simulator** tabs; expanded validation for map `song` audio files, map biome values, tileset source image files, and missing monster PNGs.
- Data Editor Maps tab: added a cached read-only map preview and used-tile gallery that lists each rendered tile gid/local tileset tile with usage counts.
- D&D monster data pass: updated all monster stat blocks to listed D&D-style HP/Hit Dice/AC/ability scores/CR/XP/action dice, including 2024 Twig Blight `7 (2d6)`, and removed old monster HP roll fields from data and runtime loading.
- D&D combat/data pass: monster instances now roll or average HP from `HitDice` unless `HitPoints` is explicitly set; monster AI now queues D&D `Actions` rather than legacy monster `Skills`; monster `Skills` were removed from data/editor validation; and static weapon/armor catalogs gained first-pass D&D weapon damage dice, weights, and armor fields.
- D&D UI cleanup: New Quest character creation and the in-game Status/character info detail now show D&D-facing HP, AC, proficiency, attack/damage/initiative bonuses, species/class, and ability scores instead of JRPG Attack/Defence/MagicDefence/Agility rows.
- D&D spell-slot pass: player spells now use spell slots instead of MP costs; spell data was renamed/mapped to D&D-style spell names, levels, schools, and class lists; the old `Spell.Cost` model/editor surface was removed; monster spell lists and magic-roll fields were removed from data in favor of innate action text; healer/item recovery now restores slots; generated equipment no longer rolls Magic or MagicDefence bonuses.
- D&D item alignment pass: added explicit D&D-style item categories (`Weapon`, `Armor`, `Potion`, `Ring`, `WondrousItem`, etc.), magic-item flags, attunement metadata, and weights to the shared `Item` data model so future item rules can target authentic D&D item families instead of generic RPG stat sticks.
- D&D catalog cleanup: removed the legacy item-definition/stat-name editor flow, deleted the old procedural `CreateRandomEquipment` path, and kept the canonical static D&D catalogs (`customitems.json`, `magicitems.json`, `nonmagicitems.json`, `weaponitems.json`, `armoritems.json`) as the active runtime source of truth for items and item images.
- D&D attunement pass: magic items requiring attunement now block equipping until the hero is eligible and attunes them, and the inventory UI exposes an explicit attune action flow.
- D&D saving throw/skill check pass: added shared `DndStatRules` helpers for saving throws and skill checks using the D&D ability modifier formula plus proficiency when trained, with skill-to-ability mapping for the common SRD skills.
- D&D class-skill cleanup: removed legacy JRPG class skill grants from class data and replaced them with D&D skill proficiencies/options in `class.json`. New/setup heroes copy those proficiencies for D&D skill checks; Rogue `Sleight of Hand` proficiency grants the combat `Steal` pickpocket action without restoring the old class `Skills` list.
- D&D skill-name cleanup: renamed the active `skills.json` effect rows and all spell/item references from legacy labels (`Heal`, `Upper`, `Sap`, `Chaos`, `Open`, `Lighting`, etc.) to D&D-facing names such as `Cure Wounds`, `Shield of Faith`, `Bestow Curse`, `Confusion`, `Knock`, and `Lightning Bolt`. Runtime behavior still keys off `SkillType`, so map/open/return/heal behavior is preserved.
- D&D condition cleanup: removed legacy JRPG condition types from the active concentration/condition rules and from active skill classification while preserving compatibility shims for older save data, so gameplay now relies on D&D conditions rather than `Sleep`/`Confusion`/`StopSpell`-style effects.
- D&D monster/player progression pass: audited monsters with leftover player-spell action names and removed/replaced them with SRD-native actions where available; added a shared D&D level advancement table; hero level-up now uses D&D XP thresholds, max level 20, hit-die HP, proficiency refreshes, spell-slot refreshes, and no JRPG random stat growth; `FirstLevel` was removed from class data/editor/schema.
- D&D prepared-spell pass: heroes now persist prepared spell IDs, known spells remain based on class/level/slot capability, combat/map casting uses prepared spells only, and the Party Spells screen can prepare/unprepare spells with prepared count/limit feedback.
- D&D encounter-avoidance pass: random encounters now open in the combat view with enemies visible before the player chooses how to respond. Initial spotting uses the worst living party member's Stealth roll, spotted hostile monsters can immediately attack, and avoidance checks use only the first living party member's stats with labels such as `Persuade (+2)`, `Deceive (+0)`, and `Intimidate (-1)`. Parties can sneak away with partial XP, reason with monsters explicitly marked `CanBeReasonedWith` for full encounter XP, leave monsters explicitly marked `NonAggressive` alone for no XP, or fight normally. The monster data/editor/schema now support explicit `Hostile`, `CanBeReasonedWith`, and `NonAggressive` encounter flags, and the current monster roster has been classified with those fields.
- D&D character creation pass: New Quest creation now uses a three-page BG3-inspired flow with race/species, class, background, name/image selection first, D&D point-buy ability scores plus a reset button and assignable `+2`/`+1` ability bonuses second, locked race/background proficiencies plus class-limited D&D skill choices, and a final review page with appearance, origin, stats, spell slots, and proficiencies before starting. Backgrounds are persisted on heroes and data-driven from `backgrounds.json`; classes and species are data-driven from `class.json` and `species.json` with Game Editor tabs, validation, and JSON schemas.
- D&D recruit metadata pass: recruitable `NpcPartyMember` map objects now define Species, Background, `+2`/`+1` ability bonus targets, and optional skill proficiency overrides. Runtime recruitment applies those before derived stats/level-up, the Maps editor exposes the fields, validation checks their references, and the three current recruits have intentional D&D identities.
- Narrative data pass: improved quest journal descriptions, branching dialog copy, and direct map NPC text for the seashell/ship quests, town guards, shrine NPCs, recruitable NPCs, and Estark hints without changing quest IDs, stages, rewards, or map hooks.
- Interactive NPC conversation pass: replaced all ordinary map NPC `Text` prompts with referenced branching dialogs across the pyramid, shrines, Coast, Forest Palace, Isis, Oasis, and the walled city.
- Recruit conversation pass: implemented the existing dialog `Join` action in Unity and converted the dungeon, Healing Shrine, and Isis recruits from static Yes/No prompts to distinct branching conversations with background, skills, recruit, and leave choices.
- Began the D&D 5.5e / 2024 SRD-inspired combat migration: added optional ability scores, armor class, proficiency, attack bonus, weapon damage dice, damage bonus, and monster challenge rating fields; normal combat attacks now resolve with a d20 attack roll against AC and damage dice/critical dice; existing JRPG stats are still used as fallback bridge values for old data.
- Mapped the existing monster roster to explicit D&D-style stat-block fields in `allmonsters.json`, including ability scores, AC, proficiency, attack bonus, damage dice, damage bonus, and challenge rating. Added species-aware character creation support with Human/Elf/Dwarf/Halfling, persisted `Hero.Species`, role-to-D&D-class labels for existing roles, and class metadata for D&D class, primary ability, and hit die.
- Updated the role-to-D&D-class mapping to: Soldier=Fighter, Hero=Paladin, Cleric=Cleric, Wizard=Wizard, Fighter=Monk, Merchant=Warlock, Bard=Bard, Thief=Rogue, and Sage=Sorcerer.
- Replaced the separate `DndClass` type with the existing `Class` enum updated to D&D class names only. Current data stores D&D class names directly.
- Added a shared D&D stat rules layer for ability modifiers, proficiency, AC, attack bonus, damage bonus/dice, initiative, and class hit-die HP. Hero setup/level-up now refreshes D&D-derived proficiency and HP from class hit dice plus Constitution modifier, combat attacks use the shared stat rules, monster damage bonuses remain explicit stat-block values, and round action order now rolls initiative using d20 plus Dexterity modifier.
- Added `memory-bank/DND_MONSTER_MAPPING.md`, mapping all 50 current monsters to suggested D&D/SRD anchors or custom stat-block inspirations for the monster balance pass.
- Renamed active monsters toward D&D/SRD-style equivalents, populated monster D&D identity fields, traits, and actions from SRD anchors where available, updated random encounter/dialog/map references, and removed monster-side legacy `Attack`, `Defence`, `MagicDefence`, and `Agility` data fields. `MonsterInstance` derives old compatibility values from D&D stats for remaining legacy skill/effect code.
- Added a party short-rest action in the in-game misc menu, plus a long-rest inn flow that restores hit points/status/spell slots and charges the inn cost when the party can afford it.

## In Progress

- Feature development and next-phase architecture planning.
- D&D-style combat migration phase 1 is started but not complete; encounter CR balancing, fuller D&D item/armor semantics, saving throws, and dialogue ability checks still need dedicated follow-up passes.
- Active architecture ideas are tracked in `memory-bank/ARCHITECTURE_BACKLOG.md`.
- Completed architecture work is archived in `memory-bank/ARCHITECTURE_COMPLETED.md`.

## Deferred

- Expand shared core unit tests beyond level-up and skill/spell progression.
- Add Unity-side edit mode tests for map loading, hidden item conditions, and save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues where they improve correctness or maintainability.
- Expand Unity Play Mode UI regression coverage to deeper game-menu tabs/modals/settings plus store and healer flows.

## Current Known Backlog Items

See `memory-bank/UNITY_MIGRATION_COMPLETED.md` for the final migration record and `memory-bank/UNITY_MIGRATION.md` for active post-migration follow-up. Main backlog groups:

- Expand shared core unit tests.
- Add Unity edit mode tests for map loading, hidden item conditions, save/load behavior.
- Add regression tests for quest dialog actions and item rewards.
- Review ReSharper warnings and fix actionable issues.
- Replace remaining runtime filesystem asset loading with Unity-native references where appropriate.
- Remove remaining temporary/debug code when no longer needed.
- Decide whether old developer/debug console commands should be recreated.
- Review and prioritize core extraction work in `memory-bank/ARCHITECTURE_BACKLOG.md`.
- Review and prioritize future feature ideas in `memory-bank/FUTURE_FEATURES.md`.
- Triage and prioritize known issues in `memory-bank/BUGS.md`.
- Remove or archive leftover experimental UI Toolkit code and notes when they are confirmed unused.
- Manually validate implemented-but-unchecked workflows in `memory-bank/MANUAL_TESTS.md`, especially Game Editor workflows, recruitable NPC map sprites, travel skills, store keyboard/gamepad navigation, in-game save modal flow, and combat edge cases.
