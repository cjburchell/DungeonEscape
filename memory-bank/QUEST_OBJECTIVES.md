# Encounter Objectives And Conditional Dialogue

Implemented 2026-10-26 as the first quest-state slice for Borderlands. Keep normal combat and existing map artwork.

## Playable Quest

Heroes of the Borderlands content **0.3** expands the warden's quest to **Clear the Kobold Lair**:

- Four entrance guards in Cave A's northern chamber, retaining `encounter/borderlands/entrance-kobolds`.
- Four store-room guards in the eastern chamber, using `encounter/borderlands/store-kobolds`.
- Either order works. Each group disappears permanently on victory; partial progress survives leaving and save/load.
- Warden dialogue distinguishes neither group cleared, entrance only, store room only, both cleared and reward already collected.
- Returning after both groups grants the existing 20 gold and 50 quest XP once. The party may postpone collecting the reward. Normal combat rewards remain separate.
- Groups cleared before the initial conversation still count, without automatically granting the return reward.
- The journal lists each objective with its completion marker.

Start a new Borderlands game for content 0.3. Existing campaign compatibility rejects older 0.2 saves; no migration was added. Other campaigns retain their existing version and stage-based quest behavior. The shared save format remains 1.3 with additive objective state.

## Data Contract

`Quest.Objectives` contains entries with:

| Field | Meaning |
| --- | --- |
| `Id` | Stable lowercase local ID, unique within the quest. Never regenerate it when changing the description. |
| `Description` | Journal/editor text. |
| `MapId` | Campaign map containing the placed encounter. |
| `EncounterId` | Permanent encounter identity on that map. |

`Quest.ObjectivesCompleteStage` selects a non-completing stage reached when every objective is satisfied. A separate `CompleteQuest=true` stage is used for turn-in and rewards. A quest cannot advance to its ready or completion stage until all objectives are complete.

Example:

```json
{
  "ObjectivesCompleteStage": 2,
  "Objectives": [
    { "Id": "entrance-guards", "Description": "Clear the entrance guards.", "MapId": "map/borderlands/cave", "EncounterId": "encounter/borderlands/entrance-kobolds" },
    { "Id": "store-guards", "Description": "Clear the store-room guards.", "MapId": "map/borderlands/cave", "EncounterId": "encounter/borderlands/store-kobolds" }
  ]
}
```

The encounter outcome is emitted by `GameState.CompleteStaticEncounter` after its existing one-time victory check. `QuestObjectiveRules.RecordEncounterVictory` matches map plus encounter identity, records each objective once and selects the ready stage only after all matches are complete. It records progress even when the quest has not yet been accepted, preventing a dead end if enemies were defeated earlier.

Objective-based groups do not need `VictoryQuestId`/`VictoryQuestStage` on the map: the quest definition owns the bindings. Existing direct stage-based encounters remain supported. Do not configure a single group to report the whole objective quest as ready prematurely.

## Saved State And Reward Safety

`ActiveQuest.CompletedObjectiveIds` stores local objective IDs. Existing `MapState.DefeatedEncounterIds` independently stores which map groups are gone. `QuestRules.AdvanceQuest` checks objectives before changing to ready/completion stages, and the existing completed-quest guard prevents repeat reward grants. Save validation rejects duplicate/unknown objective IDs and completed quests missing their required objectives.

This slice supplies reusable saved quest facts. It does not yet implement arbitrary world flags, inventory/rescue objectives, generic scripting or a general condition-expression tree.

## Dialogue Conditions

`DialogHead` supports:

- `Quest`: the quest whose state is tested.
- `QuestCompleted`: optional boolean; false means active, true means completed, absent means no completion-status restriction.
- `RequiredObjectives`: all listed IDs must be complete.
- `MissingObjectives`: all listed IDs must remain incomplete.
- Existing quest-stage filters may be combined with these conditions.

Explicit conditional heads are evaluated in authored order, then the existing start/stage/default behavior is used. Conditions require an existing active/completed quest and cannot be combined with StartQuest on the same head. A completed head can now acknowledge a prior reward explicitly.

Runtime uses shared Core `DialogRules.SelectHead`, allowing the dialogue selection to be exercised without Unity. Existing dialogue without the new conditions keeps its previous selection behavior.

## Authoring And Validation

- Game Editor Quests: add encounter objectives, select the map and placed encounter, and set the ready stage.
- Dialog head editor: select active/completed status plus required and missing objective IDs from the referenced quest.
- Quest Graph: objective bindings and dialogue conditions are displayed alongside existing stages/choices.
- Shared runtime/editor validation rejects duplicate local IDs, duplicate placement bindings within a quest, wrong campaign scopes, missing placements, invalid ready stages and unknown/contradictory dialogue requirements.
- JSON schema includes the new fields. Editor pruning preserves explicit nullable false/zero values: `QuestCompleted=false` must not become an omitted restriction, and ready stage zero must not become absent.

## Verification

Core tests cover both orders, reload, early encounters, wrong-map/duplicate events, premature turn-in, one-time XP/gold/item rewards, malformed definitions/conditions, invalid saved progress and editor serialization. Native Play Mode tests cover both actual quest routes, partial warden dialogue, deferred collection, reward idempotency and persisted group removal; escape/cancellation/defeat leave objectives unchanged. Painted layers and tileset references in the user's Cave A map were verified unchanged; only an additional encounter object and its quest wiring were added.

Validation passed: Core 309/309, Unity Edit Mode 17/17 and Play Mode 21/21; solution build has zero warnings/errors. Unity tests used an isolated 6000.4.4f1 copy because the main project is open. Human input/readability and combat balance checks remain in MANUAL_TESTS.md.

Final validation also reran Edit Mode 17/17 and the four focused quest/world Play Mode cases after tightening invalid-ID validation. The cave entrance sign now directs players to both groups.
