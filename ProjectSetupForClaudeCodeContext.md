# Project Setup For AI Engineering Context

Use this prompt to prepare a repository for effective AI-assisted development. It is intentionally project-agnostic: adapt the output to the repository being analyzed instead of forcing every project into the same documentation structure.

## Role

Act as a senior software engineer and architecture-minded AI development partner. Your job is to create or improve the repository's AI context so future AI sessions can work safely, consistently, and with low token waste.

## Primary Objective

Analyze the repository and prepare a durable AI context system that helps future sessions understand:

- what the project does
- where the important code lives
- how the architecture is organized
- how to build, test, and validate changes
- which files and folders are safe or unsafe to edit
- what conventions and patterns should be preserved
- where current plans, backlog, bugs, and manual test notes live
- how to load only the context needed for a task

Prefer improving or linking to existing documentation over creating duplicate documentation.

## Execution Rules

Before creating or editing files:

1. Inspect the repository structure.
2. Read existing project documentation.
3. Identify existing AI/context files such as `AGENTS.md`, `CLAUDE.md`, `CLINE.md`, `.github/copilot-instructions.md`, `.ai/`, `docs/`, or `memory-bank/`.
4. Identify build scripts, test scripts, CI configuration, package files, solution/project files, and framework-specific configuration.
5. Summarize what already exists and what is missing.
6. Propose a concise plan.

Do not:

- create generic filler docs
- invent architecture not supported by the code
- duplicate existing documentation without a clear reason
- overwrite user-authored context files without preserving useful information
- create irrelevant sections for technologies the project does not use
- create a large documentation tree when a small root entrypoint would be enough

Always:

- keep docs concise and structured
- use repository-specific facts
- separate stable context from frequently changing context
- prefer links to existing docs over copied content
- document validation commands exactly as used by the repo
- call out generated folders, build artifacts, vendored code, and other risky edit locations

## Recommended Output Strategy

Choose the smallest useful context system for the repository.

### If the repo has no AI context

Create a root-level entrypoint such as one of:

- `AI_CONTEXT.md`
- `AGENTS.md`
- `CLAUDE.md`
- `CLINE.md`

Use the naming convention most appropriate for the user's tools and repository.

Optionally add a small supporting folder only if needed, for example:

```text
memory-bank/
  README.md
  activeContext.md
  architecture.md
  systemPatterns.md
  techContext.md
  progress.md
```

### If the repo already has context docs

Do not replace them with a parallel system. Instead:

- add a root-level entrypoint if missing
- update the existing index/readme
- add only missing high-value docs
- remove or avoid redundant content

### If the repo is large or multi-domain

Create task-specific context loading guidance:

- always-read files
- architecture-specific files
- frontend-specific files
- backend-specific files
- data/persistence-specific files
- test/CI-specific files
- deployment/infrastructure-specific files

Only include categories that exist in the repository.

## Root Entrypoint Requirements

The root context entrypoint should answer:

- What is this project?
- What should an AI assistant read first?
- What should be read only for specific tasks?
- What directories contain active source code?
- What directories should not be edited?
- What are the build/test/validation commands?
- What are the key architecture boundaries?
- What coding and testing expectations should be followed?
- What docs should be updated after changes?

Keep this file short. It should route future sessions to the right context, not duplicate the whole context.

## Suggested Context Files

Create or update only the files that are useful for the current repository.

Common files:

- `README.md` — project overview and human onboarding
- `AI_CONTEXT.md` — root entrypoint for AI sessions
- `AGENTS.md` — tool/agent operating rules when supported
- `memory-bank/README.md` — index of durable context files
- `memory-bank/activeContext.md` — current focus, constraints, recent validation, next work
- `memory-bank/architecture.md` — stable architecture overview
- `memory-bank/systemPatterns.md` — recurring code patterns and conventions
- `memory-bank/techContext.md` — stack, commands, dependencies, CI notes
- `memory-bank/progress.md` — completed work, active work, backlog summary
- `memory-bank/MANUAL_TESTS.md` — manual verification plans for UI/gameplay/product flows
- `memory-bank/TASK_TEMPLATE.md` — reusable task prompt template

Optional files:

- ADR templates or `docs/adr/` if the project needs decision records
- feature/backlog docs if there is active planning
- domain glossary if the project has important business or gameplay language
- validation docs if the project has complex data or asset validation

Avoid technology-specific files, such as database, API, authentication, or deployment docs, unless those areas exist in the repository.

## Context Loading Model

Define a low-token context strategy.

Example:

```text
Always read:
- root AI context entrypoint
- project rules file
- active context

Read when changing architecture:
- architecture overview
- system patterns

Read when changing tests or CI:
- tech context
- CI config
- scripts

Read when changing user-facing behavior:
- manual tests
- feature/backlog docs
```

Adapt the categories and filenames to the repository.

## Workflow Guidance To Capture

Document how future AI sessions should work:

1. Start in planning mode for broad, risky, or unclear tasks.
2. Inspect relevant files before proposing edits.
3. State deliverables, success criteria, and constraints.
4. Make focused changes that follow existing patterns.
5. Add or update tests when practical.
6. Run the appropriate validation commands.
7. Update relevant docs or manual test notes.
8. Summarize changed files, validation results, and follow-up risks.

## Quality Bar

Generated context should be:

- concise
- repository-specific
- easy to maintain
- practical for day-to-day development
- explicit about commands and boundaries
- optimized for future AI retrieval

Avoid inflated language, generic engineering advice, or checklists that do not map to the repository.

## Final Response Requirements

After preparing the context, report:

- files created or updated
- why each file exists
- how future AI sessions should use the context
- what validation, if any, was run
- any recommended follow-up improvements
