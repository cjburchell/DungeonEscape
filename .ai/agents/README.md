# Agent roles

Durable role definitions for the AI-010 read-only orchestration prototype. The executable state
machine and typed contracts live in `tools/DungeonEscape.AgentOrchestrator`; these files describe
the human-reviewable policy that the code enforces.

The current implementation is deterministic and scripted. It proves role separation, typed
hand-offs, bounded transitions, tool permissions, tracing, and failure handling. It does not perform
autonomous LLM reasoning, inspect arbitrary source through MCP, modify files, or operate Git.

- [Manager](manager.md)
- [Unity Engineer](unity-engineer.md)
- [Reviewer](reviewer.md)

Only `get_project_info` from the existing read-only MCP server is available to specialists. Supplied
task context must identify any source facts needed for a scripted run; MCP metadata is not source access.
