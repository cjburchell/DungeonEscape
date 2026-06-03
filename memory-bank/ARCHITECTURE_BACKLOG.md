# Architecture Backlog

This file tracks active and future architecture cleanup ideas. Completed architecture work is archived in `ARCHITECTURE_COMPLETED.md`.

## Current Status

- Core gameplay rule extraction is complete for the current planned scope.
- UI drawing/logic split is complete for the current planned scope.
- Remaining items are next-phase architecture work, not unfinished cleanup from the completed extraction pass.

## Next Architecture Phase

- Add Unity edit/play mode tests for high-risk UI flows:
  - title create/load/delete navigation
  - game menu tabs, modals, settings changes, item/spell use
  - combat action and target selection
  - store/healer selection and transaction flows
- Add data validation tooling for JSON content:
  - item/spell/skill/monster references
  - quest/dialog links
  - map/warp/object metadata
  - missing sprite/icon/audio assets
- Consider battle tactics now that combat round/action selection rules are testable in core.
- Consider small view models for `PartyStatusWindow`, `GoldWindow`, and `MessageBox` only if they gain logic beyond rendering.

## Keep Unity-Side For Now

- IMGUI drawing/layout code in `GameMenu`, `TitleMenu`, `StoreWindow`, `HealerWindow`, and `CombatWindow`.
- Unity sprite, texture, and asset loading code.
- Audio playback code.
- File IO/autosave timing and Unity lifecycle orchestration.
