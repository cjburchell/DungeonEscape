# Agent roles

Durable role definitions for the read-only orchestration workflow. The executable state
machine and typed contracts live in `tools/DungeonEscape.AgentOrchestrator`; these files describe
the human-reviewable policy that the code enforces.

The Manager and state machine are deterministic. Specialists can use either the scripted executor or
the bounded OpenAI-backed executor added by AI-011. Role separation, typed hand-offs, transitions,
tool permissions, tracing, retry limits, and failure handling remain application-controlled. Model
execution does not permit arbitrary file access, repository modification, arbitrary test execution, or
Git mutation. QA's only execution capability is the existing bounded `run_tests` tool.

- [Manager](manager.md)
- [Unity Engineer](unity-engineer.md)
- [Reviewer](reviewer.md)
- [QA](qa.md)

The specialists may use `get_project_info`, bounded read-only `search_code` to locate source,
bounded read-only `read_script` to inspect approved source ranges, and bounded read-only `list_scenes`
to discover Unity scene paths and build-settings status. They may also use bounded, redacted, read-only
`get_unity_console` for the fixed project-scoped Unity script logs. The gateway passes results through
without interpreting, indexing, caching, or persisting them. Specialists may also use bounded read-only
`get_git_diff` for structured local changes; this adds no Git mutation or generic command access.
Every model-requested call passes through the same gateway. QA alone may use bounded `run_tests` after
Reviewer approval; Engineer and Reviewer never receive it.
