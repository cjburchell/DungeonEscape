# Heroes of the Borderlands: research and engine gap analysis

Investigation date: 2026-10-26. Status: first persistent encounter/quest prototype implemented; broader conversion remains proposed.

## Implementation update

World expansion: [BORDERLANDS_WORLD.md](BORDERLANDS_WORLD.md) now describes fourteen connected maps: Keep, overworld, entrance ravine and all eleven lairs. Campaign title is Heroes of the Borderlands, content version 0.2. Original layouts and existing art support exploration and the existing Cave A quest; B–K still need their authored encounters and quests.

User confirmed retaining the normal combat presentation and prioritizing quests. Static `Monster` map objects now define a permanent EncounterId, exact MonsterId/Count and optional victory quest/stage. Victory removes the rendered group/collision and saves defeated state per map; leaving, escaping and defeat do not complete it. Game Editor Maps exposes the fields and shares runtime validation. The installed **Borderlands: First Expedition** prototype uses existing tiles/sprites/audio, a warden quest, and a cave with four simplified kobolds. The original campaign remains the default. This authored test section is not a claim of accurate published room/stat-block conversion. The historical gap analysis below describes the pre-implementation investigation.

Validation: Core 284/284, Unity Edit Mode 17/17 and Play Mode 19/19 passed; solution builds with zero warnings/errors. Unity tests used an isolated copy because the main project is open. Map previews were rendered and visually inspected, including fresh/cached four-figure counts. Map figures reuse an existing guard sprite; combat reuses existing monster artwork. Manual controller/readability/balance checks remain in MANUAL_TESTS.md.

## Scope and confidence

The intended reference is the 2025 D&D Starter Set **Heroes of the Borderlands**, inspired by the older B2 adventure. Do not substitute encounters or maps from older editions as though they were the 2025 version.

This investigation uses public publisher material, the public adventure contents page, and the current working tree. Full adventure chapters could not be retrieved in this session. Consequently this is an architecture and scope assessment, not an exhaustive room-by-room conversion specification. Exact room populations, DCs, treasure, NPC cards, encounter probabilities, progression thresholds, and special rules remain to be checked against an accessible copy. Existing uncommitted campaign infrastructure changes were inspected as current code, not assumed to be committed or independently validated.

## What the adventure contains

The product introduces play through level 3, with Fighter, Cleric, Rogue, and Wizard options and two subclass choices per class. It includes modular character components, three adventure booklets, reference cards, maps, and handouts. See the [publisher overview](https://www.dndbeyond.com/posts/2031-what-is-heroes-of-the-borderlands-your-gateway-to).

The [official contents](https://www.dndbeyond.com/sources/dnd/hotb) confirms:

- **Keep:** 14 keyed areas covering gates, accommodation, commerce, temple, guild, and fortress.
- **Wilderness:** Trail, Woods, Fens, and Tamarack Stand, with separate regional encounter tables.
- **Caves A–K:** Kobold Lair, Mephit Lair, Nothic Lair, Goblin Lair, Ogre Lair, Hobgoblin Lair, Shunned Caves, Bugbear Lair, Minotaur Lair, Gnoll Lair, and Shrine of Evil Chaos.
- Equipment/magic items, NPCs, monsters, and handouts have dedicated appendices.

### Campaign loop and story spoilers

The publisher describes a sandbox: social activity at the Keep, wilderness discovery, and cave expeditions. Suggested opening: encounter a looted wagon, meet Haldryck, face Pral's bandits, then reach the Keep. Bartho and Gala greet arrivals; Castellan Winvarle directs attention toward the caves. Rumors provide multiple leads.

The Cult of Chaos and its Chaos Bell connect the locations. Ivlis and Narthus are central antagonists. Clues can come from a cave library, inhabitants, or the Keep's priest. The finale involves the shrine and destroying the bell at the Sacred Spring under moonlight, then returning for a celebration. Cave completion drives advancement; visiting every cave is unnecessary. Long rests are restricted to the Keep. These details come from [official running guidance](https://www.dndbeyond.com/posts/2062-behind-the-screen-tips-for-running-heroes-of-the); its suggested connective sequence should not be mistaken for a mandatory linear quest chain.

The starter rules deliberately simplify some full D&D procedures. For example, the publisher describes fixed short-rest healing on a class board, and level-3 subclass selection. Verify the actual boards before choosing healing values; current percentage healing is not evidence of starter-rule fidelity. See [class-board guidance](https://www.dndbeyond.com/posts/2045-class-boards-building-your-character-in-heroes-of).

There is also a later short adventure, **Borderlands Quest: Dagger Danger**, announced as claimable with a free account. Treat it as optional expansion content rather than part of the initial box conversion. See the [publisher announcement](https://www.dndbeyond.com/posts/2178-claim-a-new-adventure-to-celebrate-international). No claim or account operation was performed.

## What the engine already provides

Source paths below are relative to the repository root.

| Capability | Evidence and practical value |
| --- | --- |
| Separate campaigns | `DungeonEscape.Core/Data/CampaignDefinition.cs`, `Rules/CampaignContentLoader.cs`, `Rules/CampaignContentResolver.cs`, and Unity `Core/CampaignRuntime.cs`: manifest, scoped definitions, map registry, explicit roster, runtime switching. |
| Campaign-aware saves | `State/GameSave.cs` and `Rules/CampaignSaveRules.cs`: campaign identity/version and per-campaign quick-save behavior. |
| Authoring foundation | `DungeonEscape.Tools.GameEditor/Services/CampaignProjectService.cs`, `Rules/CampaignProjectTemplate.cs`, campaign/map forms, schemas, graph and simulator. New campaign scaffolding already exists. Tile painting remains in Tiled. |
| Maps and services | TMX maps, warps/spawns, collision, doors, chests, hidden items, talking NPCs, shops, healing and recruitment. Useful for the Keep and connected caves. |
| Narrative foundation | `Data/DialogText.cs`, `Data/Choice.cs`, `Rules/QuestRules.cs`: nested conversations, quest-stage variants, item exchanges, gold/XP/item rewards. |
| D&D foundation | Shared ability scores, AC, proficiency, attack/damage dice, initiative, saving throws, skill rolls, spell slots and preparation, XP progression and manual level-up. |
| Noncombat encounter resolution | `Rules/EncounterAvoidanceRules.cs` and Unity `Core/GameState.cs`: stealth and social alternatives to random combat. |
| Existing persistence | `State/MapState.cs`, `State/ObjectState.cs`: map object activity/open state, positions and sprites; this is useful existing infrastructure, not a general world-state system. |

The new campaign does **not** require another campaign-loader rewrite. Most work should extend the existing contracts and editor.

## Missing or incomplete capabilities

P0 means required for a coherent first playable adaptation. P1 means important for broad campaign coverage. P2 means fidelity/polish, depending on the chosen scope.

| Priority | Gap | Evidence / proposed change |
| --- | --- | --- |
| P0 | Authored encounters | Current `EncounterRules` generates level/biome-weighted random battles. Add explicit encounter definitions: exact mixed roster/counts, activation conditions, stable placement, rewards, and resolution outcomes. Room guards and named opponents must not depend on a travel roll. |
| P0 | Dialogue-to-combat bridge | `QuestAction.Fight` and `Choice.Monster` exist, but `PlayerGridController.ProcessDialogChoice` has no Fight branch. Implement start/resume flow and report victory, defeat, escape, or negotiated outcome back to narrative state. |
| P0 | Reliable persistent outcomes | Extend beyond numeric quest stages with typed conditions/effects and scoped flags: met NPC, learned clue, encounter resolved, prisoner rescued, bell recovered/destroyed. Reuse existing map state where appropriate. Stable placements must distinguish multiple instances and layers. |
| P0 | Campaign rest policy | `GameState.MakeCamp()` allows free overworld long rests; `LongRest(int)` has no location policy. Add campaign/map rest rules checked by every entry point. Borderlands safe locations should be explicit. Preserve current behavior for the original campaign. |
| P0 | Campaign progression rules | `QuestRules` grants XP and the current hero progression uses XP thresholds. Add campaign-configured milestones/level ceiling and one-time cave-resolution awards. Exact advancement thresholds need source verification. Reward diplomacy as well as kills. |
| P0 | Four-character early-game balance | Current creation starts with a hero and recruitment supplies companions. Provide an intentional starting party or early recruits/presets. Existing random scaling, gold, drops and merchant prices need a level-1–3 pass. |
| P1 | Authored checks and interactions | `DndStatRules` and `GameState` expose checks, but `Choice` has no DC, actor selection, success/failure branches, or general condition tree. Add those plus bounded retry policy and persistent trap/secret state. |
| P1 | Regional story encounters | Regional biome support is a starting point. Add tables containing discoveries, travelers, clues and choices, with repeat/once rules and conditions; monster-only rolls cannot cover this. |
| P1 | NPC identity and reactions | Reusable NPC definitions are still backlog; TMX holds NPC details. Add definition references and stable placements, then conditional presence/dialogue. Use a small set of authored allegiances before designing a large faction simulation. |
| P1 | Special monster mechanics | `MonsterTrait` holds descriptive text. `MonsterAction` supports attack/damage, count and bonus-action fields, but lacks structured save DC/effect, recharge and area rules. Audit and implement the mechanics actually needed by selected encounters. Descriptions are not executable traits. |
| P1 | Character features | Existing class/slot progression does not provide the complete starter class-board/subclass feature set. Inventory Fighter/Cleric/Rogue/Wizard abilities at levels 1–3, then implement or explicitly adapt each required feature. |
| P1 | Finale conditions | Time/night is not a general campaign clock in the inspected contracts. A scripted wait-until-night interaction can implement the finale initially; a complete day/night simulation can wait. Handle bell ownership, loss/recovery and ending state. |
| P1 | Authoring and validation | Extend forms, schemas, graph inspection and reference validation alongside each new definition; detect unreachable quest states, invalid outcomes, duplicate placements and dangling encounter references. |
| P2 | Tactical D&D combat | The current combat UI selects targets from sprites rather than a tactical movement board. Grid movement, range/line-of-sight, cover, area placement and opportunity attacks would be a separate major project for close tabletop fidelity. |
| P2 | Exploration presentation | Fog of war, discovery map, rumor/clue journal and quest destinations improve navigation. Secret interactions can initially use the existing tile interaction presentation. |

### Monster and item audit

The inspected shared roster contains 50 monsters. Exact-name overlap with the adventure contents includes Giant Centipede and Giant Fire Beetle. Similar names such as Goblin Archer, Gray Ooze, Minotaur, and Green Dragon Wyrmling must not be treated as matching the adventure's variants without comparing mechanics.

Examples of content still to author or verify include kobolds, hobgoblins, bugbears, gnolls, ogres, cultists, skeletons, zombies, a nothic, a steam mephit, and named NPC opponents. The [adventure index](https://www.dndbeyond.com/sources/dnd/hotb) identifies the source roster, but does not establish encounter counts or complete stat blocks. Make a full per-definition comparison once the source is available.

Existing monster `MinLevel`, `GroupSize`, gold, drops and legacy compatibility fields also need review: having a D&D-shaped stat block does not make it appropriate for this low-level campaign. Reuse suitable base definitions, create campaign variants with new IDs, and avoid retuning original-campaign encounters accidentally.

Equipment requires a separate source inventory: starting kits, armor behavior, consumables, keys, treasure, plot items and magic-item effects. Generic equipment data and quest-item support already exist; exact catalog coverage and active effects are unverified. Art, tiles, portraits, audio and readable handouts are content production tasks in addition to programming.

## Recommended adaptation scope

Start with a **Borderlands adventure using Dungeon Escape's existing combat presentation**. Keep the hub/travel/expedition loop, meaningful choices, level range and major objectives. Record each rules simplification. Full tabletop combat parity is optional and should not delay proving the campaign loop.

Initially choose either starter-style rules or the project's broader SRD-inspired rules and document the deviations. Do not silently mix short-rest, subclass and encounter assumptions from different rulesets.

Suggested campaign key: `borderlands`, with `campaign/borderlands/main` and scoped IDs such as `map/borderlands/keep`. Follow the established `Campaigns/<key>/` layout. These are proposals only; no campaign was registered by this research task.

## Implementation sequence and completion gates

1. **Source inventory and scope.** Obtain access to the adventure chapters/cards needed for conversion. Record each location, room, actor, encounter, check, clue, reward and ending requirement in a traceability sheet. Distinguish confirmed source behavior from adaptation decisions. Resolve use/distribution of source-specific text and assets before packaging them; the public overview does not provide an asset license.
2. **Reusable engine foundation.** Add encounter definitions/results, stable placement state, conditions/effects, rest policy and progression policy in Core. Add save fields/version handling under the existing no-old-save-migration policy. Add matching validation and editor controls. Gate: victory, negotiated success, escape and reload produce correct state and cannot duplicate rewards.
3. **First playable section.** Author an approach map, a small Keep hub with inn/merchant/rumor giver, and one cave with a clue, encounter and return reward. Give the player a viable party. Gate: New Quest through expedition, return, save, quit and reload all work; cave progress persists and long rests are restricted correctly.
4. **Exploration and social coverage.** Add regional event tables, checks, secrets/traps, additional Keep conversations and alternative resolutions. Gate: at least one combat objective succeeds through a noncombat route with equivalent progression.
5. **Complete campaign content.** Build remaining cave areas and wilderness sites, required monsters/items/abilities, finale and return celebration. Gate: the main route is completable without visiting every cave; alternate clue sources prevent an accidental story dead end.
6. **Balance and release verification.** Test supported party compositions and levels, economy, resource pressure, retreat/re-entry and repeat interactions. Verify every campaign asset is staged in the Windows build and the original campaign still works.

Do not estimate the full campaign by map count alone: keyed rooms, encounters, branches and mechanics drive effort. A reliable estimate needs the source inventory and agreement on combat fidelity.

## Validation plan

- Core tests for conditions/effects, deterministic checks, encounter outcomes, reward idempotency, rest/milestone policies and save round trips.
- Unity Edit Mode tests for content/reference validation and supported map metadata.
- Unity Play Mode tests for dialogue-triggered fights, outcome callbacks, party setup, travel, rest restrictions, cave persistence and ending flow.
- Manual keyboard/gamepad checks, alternative quest routes, retreat/reload and a staged Windows build smoke test.
- Use Unity `6000.4.4f1` and repository scripts. No build or tests were run for this documentation-only investigation; existing runtime behavior was assessed by code inspection.

## Remaining decisions and unknowns

- Exact adaptation versus a looser original campaign inspired by the setting; existing combat presentation versus tactical redesign.
- Source chapter access and the complete room/encounter/item/ability inventory.
- Starting party composition, recruitment and supported party size.
- Starter-rule fidelity versus existing SRD-inspired character rules.
- Whether original authored art/maps will be used and how the resulting campaign will be distributed.

These do not block starting the reusable foundation design, but they do block claiming a faithful, fully specified conversion.
