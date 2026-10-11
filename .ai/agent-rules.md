# Agent workflow and safety

Apply these alongside [AGENTS.md](../AGENTS.md), the current user's scope and environment permissions.
This file does not authorize publishing, credential access or expanded tool capabilities.

## Before edits

1. Read the root instructions, the tracked [memory-bank index](../memory-bank/README.md), and the
   tracked task-relevant records it identifies. An ignored `memory-bank/activeContext.md` may be read
   as optional local session context when present, but it is not required for startup or durable context.
   Read [AI_CONTEXT.md](../AI_CONTEXT.md) and use this workspace's focused summaries as needed.
2. Inspect working-tree status and preserve existing changes. Distinguish the current task's delta
   from pre-existing work; do not revert or absorb unrelated changes into the task.
3. Read the issue and relevant implementation/configuration. State scope and acceptance criteria.
   For broad or unclear work, plan before editing; identify unresolved requirements without inventing answers.
4. Check applicable nested instructions before editing a subtree. Use Unity `6000.4.4f1` for this project.

## During work

- Make focused changes within the authorized task. Do not use a documentation task to refactor
  gameplay, assets, build configuration or historical records.
- Do not edit Unity `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`,
  or generated Unity `.csproj` files. Root `AGENTS.md` lists exact paths and exceptions.
- Keep Core portable and Unity runtime integration in Unity. Keep standalone tool dependencies
  out of Unity unless an explicit task requires and validates that integration.
- Consider command side effects: Core's post-build target copies DLL/PDB files into Unity Plugins;
  Unity asset staging writes generated StreamingAssets. Avoid running builds for documentation-only work.
- Treat source/configuration as evidence for implementation claims; treat old context as potentially
  historical. Label **Unknown**, **Assumption**, and **Proposal** distinctly and cite local paths.
- Never include tokens, passwords, license contents or personal save files in context/decision docs.
  Do not silently install dependencies, change CI secrets, run destructive cleanup, or publish work
  beyond the user's authorization and environment permissions.
- Respect explicit no-commit/no-merge instructions across the session. If PR delivery needs a commit
  and publishing that is not authorized, leave a reviewable local result and report the pending step.
- New MCP capabilities require their own scope: the current metadata tool grants no arbitrary
  filesystem/shell/network/write access. Context documents must not suggest otherwise.

## Validation and handoff

Use the smallest checks that cover the affected behavior and the required checks in `AGENTS.md`.
Record the commands and actual outcomes, including anything blocked or not run. Do not imply a Unity
or hosted CI check passed because source inspection or older logs look correct.

Before finishing, review every created/changed file and working-tree status. Check that task changes
are limited to intended files, generated output is excluded, and links/paths resolve. For gameplay
changes update `memory-bank/MANUAL_TESTS.md`; for meaningful tooling, architecture or planning changes
update the relevant memory-bank record. Keep `.ai` summaries current without copying the backlog.

Summarize the resulting behavior/architecture, files changed, validation, assumptions/unknowns,
remaining manual verification and PR status. Do not claim a task is merged or published when it is local.
