# Agent roles

Durable role definitions for the AI-010 read-only orchestration prototype. The executable state
machine and typed contracts live in `tools/DungeonEscape.AgentOrchestrator`; these files describe
the human-reviewable policy that the code enforces.

The current implementation is deterministic and scripted. It proves role separation, typed
hand-offs, bounded transitions, tool permissions, tracing, and failure handling. It does not perform
autonomous LLM reasoning, read arbitrary files through MCP, modify files, or operate Git.

- [Manager](manager.md)
- [Unity Engineer](unity-engineer.md)
- [Reviewer](reviewer.md)

The specialists may use `get_project_info`, bounded read-only `search_code` to locate source,
bounded read-only `read_script` to inspect approved source ranges, and bounded read-only `list_scenes`
to discover Unity scene paths and build-settings status. The gateway passes results through
without interpreting, indexing, caching, or persisting them; this is not arbitrary filesystem access.
