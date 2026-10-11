# AI context workspace

Repository context for AI-001 / [issue #7](https://github.com/cjburchell/DungeonEscape/issues/7).
This workspace is documentation, not an agent runtime or an automatic permission grant.

## Read order

1. Read [AGENTS.md](../AGENTS.md) for repository instructions and validation commands.
2. Read the tracked [memory-bank index](../memory-bank/README.md), then the tracked
   task-relevant files it identifies. This is the complete memory-bank startup path for a fresh checkout.
3. Use [architecture.md](architecture.md) for a source-backed map of the current code,
   [coding-rules.md](coding-rules.md) for existing conventions, and
   [agent-rules.md](agent-rules.md) for safe work and handoff.
4. Inspect the actual source and configuration involved in the task before making changes.

`memory-bank/activeContext.md` is an optional, ignored local session overlay. Read it when it is
present and useful, but never require it, assume it exists, or use it as the sole source of durable
project context. Durable shared context belongs in the tracked memory-bank files listed by the index.

## Context ownership

| Location | Responsibility |
| --- | --- |
| Root `AGENTS.md` | Repository operating rules, protected paths, Unity version, test commands |
| Root `AI_CONTEXT.md` | Existing routing entrypoint; retained |
| `.ai/architecture.md` | Concise current component/dependency map with source evidence and unknowns |
| `.ai/coding-rules.md` | Conventions observed in project files and representative source |
| `.ai/agent-rules.md` | Inspection, change scope, validation and handoff procedure |
| `.ai/agents/` | Durable role definitions for the deterministic multi-agent workflow |
| `.ai/decisions/` | Future explicit decision records; see its README |
| `.ai/skills/` | Future repository-specific workflows; see its README |
| `memory-bank/` | Tracked durable project context: architecture/history, progress, bugs, backlog and manual validation; optional ignored `activeContext.md` may hold local session context |

Keep tracked memory-bank files as the durable shared project context. Do not copy their
entire contents into `.ai` or maintain competing progress/backlog lists. Update the relevant
memory-bank records after meaningful changes; update `.ai` when its component map or conventions change.
Root instructions and explicit task constraints take precedence over these descriptive summaries.

## Evidence and uncertainty

The initial workspace was inspected against local source/configuration on 2026-10-10.
Links identify evidence; they do not establish that a build or manual test has passed today.
When docs disagree, inspect the source/configuration and report the discrepancy instead of
silently assuming either document is current. Label unresolved claims **Unknown** and proposed
choices **Proposal**; do not turn inferred history into accepted decisions.

Known documentation drift: root README still describes Unity tests as deferred, although test
assemblies and CI jobs exist. Some memory-bank entries refer to older versions or paths and are
historical. The architecture summary records the inspected current paths without rewriting those histories.

## Initial verification and open items

This issue changes documentation only. Review local links, required structure, source evidence,
and the diff; a gameplay build or Unity test run is not evidence needed for these text changes.

Owner verification requested:

- **Unknown:** which CI host/branch is authoritative for release gates; both GitHub and GitLab configuration exist.
- **Unknown:** current Unity license availability and manual validation status; historical successes are not a fresh run.
- **Unknown:** whether remaining UI Toolkit previews should eventually be retained or removed. No cleanup is part of this issue.
- **Proposal:** retain the existing root entrypoints and memory bank, with `.ai` as the compact supporting workspace above.

The existing local MCP implementation from AI-002 is described as local work, not assumed to be merged.
Pull-request delivery of AI-001 remains a separate handoff step while the instruction not to commit
or merge automatically is in effect.
