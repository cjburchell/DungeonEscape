# Borderlands Feature Roadmap

Created 2026-10-26. Proposed implementation priorities following the fourteen-map world expansion.

Implementation update: the recommended first BL-01/02 slice is complete. See [QUEST_OBJECTIVES.md](QUEST_OBJECTIVES.md): two independent encounter objectives, saved progress, state-dependent warden dialogue, guarded one-time reward and authoring/validation. This covers encounter-based quest facts, not the entire general world-flag/multi-type objective backlog. Borderlands content is now 0.3. Broader BL-03 journal work and the complete Cave A adventure remain future work.

## Direction

Keep the current combat screen and reuse existing game assets. Focus on authored quests, NPC conversations, exploration and persistent consequences. Complete one substantial cave adventure before populating all remaining caves.

This is a backlog and implementation sequence, not authorization to implement every feature at once. Priorities describe dependencies and player value, not effort estimates. No implementation dates are assigned.

## Already Working

- Campaign selection and campaign-aware saves, including the Borderlands 0.2 content-version guard.
- Starting parties with up to four individually created characters.
- Fourteen connected maps: Keep, overworld, entrance ravine and eleven distinct lairs.
- Exact-count static encounters of one monster type, normal combat, saved victory removal and an optional victory quest stage.
- The warden/four-kobold/return-reward loop in Cave A.
- Existing shops, inn/rest actions, items, chests, doors, branching dialogue and numeric quest stages as reusable foundations. The Borderlands provisioner is currently descriptive rather than an operational shop.
- Game Editor authoring and validation for current campaign data and static encounters.

These foundations need extensions and content, not wholesale replacement.

## Priorities

**P0:** next development work; supports reliable quest authoring. **P1:** needed for a fuller playable campaign. **P2:** usability and polish after the main adventure works. Rows are in recommended implementation order; dependencies can require a P1 item during a P0 content milestone.

| ID | Priority | Feature | How to incorporate it | Done when |
| --- | --- | --- | --- | --- |
| BL-01 | P0 | Persistent quest conditions and world flags | Add campaign-scoped saved facts and reusable conditions for dialogue and map interactions: quest state, item possession, learned clue, rescued NPC, encounter resolution. Build on current quest stages and map state. | A choice changes a later conversation or object, survives reload and does not affect another campaign. |
| BL-02 | P0 | Objective tracking and safe quest outcomes | Add objective definitions/events for defeating groups, collecting items, speaking to NPCs and interacting with locations. Support multiple required objectives, prerequisites and one-time turn-in rewards. Use the existing static encounter callback as the first event producer. | A quest requiring two groups and an item completes in either order; escape, repeated interactions and reload cannot duplicate or prematurely award progress/rewards. |
| BL-03 | P0 | Clear quest journal | Show current objectives, completed objectives, latest useful clue and known destination/map name. Show only clues/locations the party has learned. | Players can resume a saved game and understand where to go and what remains without consulting signs or project notes. |
| BL-04 | P0 | Complete Cave A adventure | Expand the current test quest into an authored cave expedition with several objectives, conversations, fixed treasure and a reason to return to the Keep. Add optional paths or a clue as verified source content allows. Use BL-01/02 and current combat. | A complete Keep-to-cave-to-Keep quest chain works, including retreat/re-entry, missed objectives, save/load and a one-time reward. This becomes the template for later caves. |
| BL-05 | P0 | Working Keep services and campaign rest rules | Configure the existing store system for the provisioner and use existing inn/item services. Add campaign-specific rest permissions checked by every UI/runtime entry point, including Make Camp. Decide bounded short-rest behavior so repeated free healing cannot erase expedition resource costs. | Players can prepare, buy/sell, rest at allowed locations and return to the expedition; disallowed long rests cannot be invoked through a different menu. Original campaign behavior remains intact. |
| BL-06 | P1 | Campaign progression and early balance | Add campaign advancement policy, one-time milestone awards and a level ceiling of 3 for this adventure. Decide the supported party-size/balance policy; use a four-character baseline first and explicitly test any smaller-party support. Apply alongside BL-04 as needed. | Combat and noncombat objective completion advance the party appropriately, retries cannot farm levels, and the first expedition is viable for the supported party configuration. Exact published thresholds need verification. |
| BL-07 | P1 | Richer authored encounter and reward data | Extend the current single-type/count group to an exact roster of multiple monster entries, named opponents and authored rewards. Preserve stable placement identity. Add only the monster mechanics needed by upcoming quests. | One placed group can open a battle with a named leader plus specified followers, grant its configured treasure once and remain resolved after reload. Existing one-type groups continue to work. |
| BL-08 | P1 | Skill checks and noncombat resolutions | Reuse existing D&D check rules in dialogue and exploration. Add actor selection, skill/DC display, success/failure branches and an explicit retry policy. Let persuasion, stealth, bargains or other authored outcomes resolve an objective/group without victory in combat. | A group/objective can be resolved through an alternative route, with correct saved state and comparable progression where intended; combat and quest rewards are not both awarded twice. |
| BL-09 | P1 | Secrets, traps and locked interactions | Extend existing chests/doors/items with discover, disarm, unlock and inspect interactions. Persist discovery and trap state. Reuse BL-01/08 rather than adding cave-specific code. | A discovered passage or disarmed trap stays that way after leaving/reloading; failures have an authored consequence and keys/checks open only the intended object. |
| BL-10 | P1 | NPC identity, clues and reactions | Add reusable NPC definitions/stable placements when actors need repeat appearances, rescue or state-dependent presence. Author Keep rumors and reactive conversations using BL-01. Existing TMX NPC metadata can support the first content slice. | An NPC can give a clue, be rescued or change dialogue based on an expedition outcome without changing unrelated NPC instances. Clues needed for progress have an alternate route where appropriate. |
| BL-11 | P1 | Wilderness events | Add regional tables for authored discoveries, travelers, ambushes, clues and small objectives. Support once-only versus repeatable events and outcome conditions. Use existing biome metadata; do not rely solely on random monster rolls. | The Trail, Woods, Fens and Tamarack Stand offer distinct interactions, and travel events respect prior outcomes and saved state. |
| BL-12 | P1 | Populate the remaining cave adventures | Apply the Cave A checklist to B-J in small batches, then K as part of the finale. Author required monsters/items/dialogues/objectives and adjust rooms where the quest needs it. Use a source inventory for encounter counts, NPCs, clues, rewards and rules. | Each shipped cave has an entry hook, meaningful objective/choice, configured encounters or interactions, reward/consequence, return path and save/load coverage. No copy-pasted kill counter is mistaken for a completed adventure. |
| BL-13 | P1 | Connected story and finale | Plan clue links before populating every cave. Implement the shrine objective, important artifact ownership/state, required destruction interaction and a Keep ending/celebration. A scripted wait-until-night interaction can cover the finale before a general clock exists. | The main adventure is finishable without clearing every cave, essential clues cannot be accidentally lost forever, and the ending persists. Verify exact source requirements before authoring. |
| BL-14 | P2 | Discovery and navigation UI | Add a discovered-location map, region/map names, quest destinations and optional exploration fog. Keep unknown locations/clues hidden until discovered. | Players can orient themselves and revisit known destinations with keyboard/gamepad support. |
| BL-15 | P2 | Presentation and content polish | Replace prototype descriptions, improve cave visual identities with existing tiles, refine NPC portraits/monster stand-ins, add appropriate sound cues and tune readability. | The campaign has clear visual landmarks and consistent text without changing its quest/combat contracts. |

## Implementation Order And Milestones

1. **Quest foundation:** BL-01 and BL-02, with matching editor controls and validation. Begin BL-03 against the same objective data. Agree the main story/clue outline now, while deferring finale implementation.
2. **First complete expedition:** BL-04 and BL-05, plus enough BL-06 to balance the supported party. Finish and play-test Cave A before making more quest chains. Prefer existing interactions first; pull BL-07/08 forward only where this adventure needs them.
3. **Demonstrate different quest types:** add BL-07/08/09 and one later cave that proves a genuinely different interaction, such as negotiation, discovery or a rescue. Choose that cave from verified source requirements, not an assumed difficulty ranking.
4. **Make the world reactive:** BL-10/11, connecting Keep rumors, wilderness outcomes and cave clues. Do not require a full faction simulation to author a handful of meaningful reactions.
5. **Expand in batches:** BL-12, usually one or two caves per batch. Adjust order to source difficulty, mechanical dependencies and clue placement. Verify each batch before increasing scope.
6. **Complete the adventure:** BL-13, then a full main-path and alternate-path playthrough. Finish BL-14/15 as needed for usability and release quality.

## Content Inventory In Parallel

The full source chapter/card inventory remains incomplete. For each area being authored, record its NPCs, objectives, checks, encounters, treasure, clues and required mechanics before claiming fidelity to the published adventure. Distinguish verified source material from original adaptation decisions. This does not block engine work or an explicitly original test quest.

Create a per-cave checklist: hook -> objectives/choices -> interactions/encounters -> rewards/consequences -> clue links -> return/ending -> validation. Do not assume all caves must be cleared or assign a definitive level order until their source requirements are checked.

## Rules For Every Feature

- Put portable quest/rules/state behavior in Core; keep Unity responsible for presentation and map interaction.
- Ship data models, Game Editor controls, reference/schema validation and meaningful tests together. Avoid narrative behavior hidden in names or one-off Unity branches.
- Test alternate completion order, repeat interaction, retreat/escape/defeat, turn-in before/after requirements, and save/load. Rewards and permanent outcomes must be applied at most once.
- Preserve the original campaign and immutable definition/placement IDs. Apply the existing save/content-version policy; do not add old-save migration.
- Review balance after every cave batch, including party size, rests, loot/economy and noncombat progression. Balance is ongoing work, not only a final phase.
- Before calling the campaign complete, run Core, Unity Edit Mode and Play Mode checks, human keyboard/gamepad playthroughs, and a staged Windows build smoke test.

## Deferred Scope

Tactical-grid combat, new combat presentation, a full day/night simulation, general faction AI, procedural dungeons and new artwork are not prerequisites for this quest-focused campaign. They require separate user direction if pursued.

## Recommended Next Task

Implement **BL-01/02 together as a small quest-state slice**: saved conditions plus a quest requiring two independently placed groups, with a one-time return reward and state-dependent NPC dialogue. Extend the editor and test both completion orders and reload. This proves the reusable behavior needed to turn Cave A from the current single-group test into a complete expedition.
