# Repository workflows

Reserved for future repository-specific, repeatable agent workflows. No executable skills or
automatic tool integrations are introduced by AI-001.

Existing automation remains in [`scripts/`](../../scripts) and commands remain in
[AGENTS.md](../../AGENTS.md). Link to those sources rather than duplicating implementations.

When a later task adds a workflow, document its purpose/trigger, required inputs and permissions,
steps, side effects, validation and stop conditions. Prefer the existing scripts and keep scope
explicit. **Unknown:** the intended future skill runner/discovery mechanism; a file here should
not be assumed to be installed, auto-discovered, or authorized to access external services.
