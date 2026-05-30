# AI Prompts And Context Switching

Use this file when starting work with Cline, Codex, or another AI coding assistant. The goal is to make each tool use the same repository memory and to make handoffs between tools predictable.

## Default Starter Prompt

Use this at the beginning of a new AI session:

```text
Use AGENTS.md and AI_CONTEXT.md first, then load only the memory-bank files relevant to this task.

Start in planning mode if the task is broad, risky, or unclear. Before editing, identify the relevant files and propose a concise plan.

After implementation, run appropriate validation and update relevant memory-bank docs if project context, progress, bugs, architecture, or manual test coverage changed.

Task: <describe task here>
```

## Short Starter Prompt

Use this for small tasks:

```text
Use AGENTS.md and AI_CONTEXT.md first, then load only the memory-bank files relevant to this task.

Task: <describe task here>
```

## Planning-First Prompt

Use this for broad, risky, or unclear work:

```text
Use AGENTS.md, AI_CONTEXT.md, and the relevant memory-bank docs.

Start in planning mode. Inspect the relevant files, identify constraints, and propose a concise implementation plan before editing.

Task: <describe task here>
```

## Continuing From Another AI Tool

Use this when switching from Cline to Codex, Codex to Cline, or another AI assistant:

```text
Use AGENTS.md and AI_CONTEXT.md first.

I am continuing work from another AI tool. Read memory-bank/activeContext.md and any relevant task files before making changes.

Task: <describe next task here>
```

## End-Of-Task Memory Update Prompt

Use this before finishing a meaningful task:

```text
Before finishing, update the relevant memory-bank docs if this changed project direction, architecture, known bugs, progress, or manual test coverage.

Summarize:
- changed files
- validation run
- validation skipped or blocked
- follow-up risks or next steps
```

## Which Memory Files To Update

- `memory-bank/activeContext.md` — current focus, recent changes, recent validation, and next likely work.
- `memory-bank/progress.md` — completed work, active work, deferred work, or backlog summary changes.
- `memory-bank/MANUAL_TESTS.md` — gameplay-facing manual verification steps.
- `memory-bank/BUGS.md` — newly discovered bugs or changed bug status.
- `memory-bank/FUTURE_FEATURES.md` — new feature ideas or changed feature priorities.
- `memory-bank/ARCHITECTURE_BACKLOG.md` — architecture cleanup ideas or changed architecture priorities.
- `memory-bank/architecture.md` — stable architecture changes.
- `memory-bank/systemPatterns.md` — recurring implementation pattern changes.
- `memory-bank/techContext.md` — tooling, command, dependency, or CI changes.

## Git Habit For Shared Memory

When an AI task changes durable context, commit the relevant memory-bank updates with the related code change.

Useful checks:

```powershell
git status
git diff
```

This keeps Cline, Codex, and future AI sessions aligned around the same repository memory.
