# AI Context

This repository contains **Dungeon Escape**, a retro-inspired 2D RPG whose active runtime is the Unity project in `DungeonEscape.Unity/`. Shared engine-neutral gameplay, state, rules, and view-model logic live in `DungeonEscape.Core/` with regression coverage in `DungeonEscape.Core.Test/`.

## Read First

Always read these files before making changes:

- `AGENTS.md` — repository-specific operating rules, Unity version, safe edit locations, and validation commands.
- `memory-bank/activeContext.md` — current focus, recent validation, and near-term constraints.
- `memory-bank/progress.md` — completed work, active work, and backlog summary.

## Read By Task

- **Architecture or refactors**: `memory-bank/architecture.md`, `memory-bank/systemPatterns.md`, `memory-bank/ARCHITECTURE_BACKLOG.md`
- **Build, test, CI, or environment work**: `memory-bank/techContext.md`, `.gitlab-ci.yml`, `scripts/`
- **Gameplay, UI, save/load, or user-facing behavior**: `memory-bank/MANUAL_TESTS.md`, relevant docs in `memory-bank/` such as `UI_TOOLKIT_MIGRATION.md`, `GAME_MENU_REDESIGN.md`, `BUGS.md`, or `FUTURE_FEATURES.md`
- **Project direction / quick repo orientation**: `README.md`, `memory-bank/projectbrief.md`, `memory-bank/README.md`

## Active Source Directories

- `DungeonEscape.Unity/Assets/` — Unity scenes, assets, and gameplay/runtime scripts
- `DungeonEscape.Unity/Packages/` — Unity package configuration
- `DungeonEscape.Unity/ProjectSettings/` — Unity project settings
- `DungeonEscape.Core/` — shared portable domain/state/rules/view-model code
- `DungeonEscape.Core.Test/` — shared automated tests
- `scripts/` — restore/build/test/validation automation
- `memory-bank/` — durable project context and manual testing notes

## Do Not Edit

- Generated Unity/build folders: `DungeonEscape.Unity/Library/`, `DungeonEscape.Unity/Temp/`, `DungeonEscape.Unity/Obj/`, `DungeonEscape.Unity/Build/`, `DungeonEscape.Unity/Builds/`, `DungeonEscape.Unity/Logs/`, `DungeonEscape.Unity/UserSettings/`
- Generated Unity `.csproj` files unless explicitly requested
- Other transient or tool-generated folders unless the task specifically targets them, such as `.vs/`, `TestResults/`, `temp/`, or package caches

## Build, Test, And Validation

Use the repository scripts and exact commands from `AGENTS.md`:

- Restore: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-restore.ps1`
- Build: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-build.ps1 -NoRestore`
- Core tests: `powershell -ExecutionPolicy Bypass -File scripts\dotnet-test.ps1 -NoRestore`
- ReSharper scan: `powershell -ExecutionPolicy Bypass -File scripts\run-resharper.ps1`
- Unity validation: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-validate.ps1`
- Unity Edit Mode tests: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-editmode-tests.ps1`
- Unity Play Mode tests: `powershell -ExecutionPolicy Bypass -File scripts\run-unity-playmode-tests.ps1`

Run the smallest relevant validation for the change:

- Shared core changes: run core tests
- Unity runtime/UI/gameplay changes: run Unity validation and the relevant Unity tests when available
- Risky/refactor-heavy changes: also run the build and, when useful, ReSharper or `git diff --check`

## Architecture Boundaries

- `DungeonEscape.Unity` is the active runtime and owns Unity-specific input, rendering, scene flow, assets, and UI.
- `DungeonEscape.Core` must stay Unity-free and hold portable rules, state, data contracts, and view models when they are engine-neutral.
- Prefer extending existing helpers and view-model patterns instead of adding duplicate logic in Unity UI code.
- The old MonoGame/Nez implementation is not on this branch; use `main` only if historical reference is needed.

## Coding And Workflow Expectations

- Start in planning mode for broad, risky, or unclear tasks.
- Inspect the relevant code and docs before proposing edits.
- State deliverables, success criteria, and constraints.
- Make focused changes that preserve existing patterns and architecture boundaries.
- Add or update tests when practical.
- Update `memory-bank/MANUAL_TESTS.md` after gameplay or UI behavior changes.
- Update durable context docs in `memory-bank/` when architecture, workflow, progress, or backlog meaningfully changes.

## Root Context Strategy

Use this file as the routing entrypoint. Do not duplicate the full memory bank into new docs; prefer updating the existing `memory-bank/` files and linking to them.