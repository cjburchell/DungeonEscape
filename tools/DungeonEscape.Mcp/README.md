# DungeonEscape MCP server

Standalone bounded repository inspection, Unity diagnostics, local Git change inspection, approved
test execution, and human-approved exact file replacement server.
Targets .NET 10 and uses the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk)
(`ModelContextProtocol` 2.2.0). It has no Unity or gameplay project references.

## Structure

- `Program.cs`: minimal host, stderr logging, stdio transport, explicit tool registration.
- `Services/RepositoryStartupOptions.cs`: strict parsing of trusted launcher configuration.
- `Tools/ProjectInfoTools.cs`: MCP presentation and read-only annotations.
- `Services/ProjectInfoService.cs`: repository discovery and bounded metadata reads, independent of MCP.
- `Tools/SearchCodeTools.cs`: MCP contract and strictly read-only annotations for literal search.
- `Services/SearchCodeService.cs`: bounded UTF-8 search, independent of MCP.
- `Tools/ReadScriptTools.cs`: MCP contract and strictly read-only annotations for bounded file ranges.
- `Services/ReadScriptService.cs`: bounded UTF-8 line-range reads, independent of MCP.
- `Tools/ListScenesTools.cs`: MCP contract and strictly read-only annotations for scene discovery.
- `Services/ListScenesService.cs`: bounded scene/build-settings discovery, independent of MCP.
- `Tools/GitDiffTools.cs`: narrow MCP contract and strictly read-only annotations for local changes.
- `Services/GitDiffService.cs`: fixed-command, bounded local Git status/diff inspection.
- `Tools/RunTestsTools.cs`: narrow MCP enum/timeout contract with non-read-only execution metadata.
- `Services/RunTestsService.cs`: fixed suite catalog and bounded no-shell test process execution.
- `Tools/ControlledWriteTools.cs`: explicit prepare/apply MCP contracts and accurate write annotations.
- `Services/ControlledWriteService.cs`: bounded preview, optimistic concurrency, approval, and atomic replacement.
- `Services/RepositoryContentPolicy.cs`: shared approved roots, exclusions, extensions, and path resolution.
- `Services/RepositoryPathSecurity.cs`: shared containment and reparse-point checks.
- `Security/McpCapabilityCatalog.cs`: authoritative typed nine-tool metadata and side-effect inventory.
- `../DungeonEscape.Mcp.Tests/`: xUnit discovery, metadata, boundary and real stdio tests.

These projects intentionally remain outside the gameplay solution. Build and test them directly;
no Unity editor, Unity assemblies or changes to game build scripts are needed.

The static capability catalog is the authoritative inventory for tool names, safety annotations, and
side-effect classifications. Tool attributes retain the SDK's clear compile-time registration model;
real stdio tests require the registered set and every annotation to match the catalog exactly. Unknown
catalog lookups fail closed, and duplicate/incomplete catalog definitions are rejected. Adding or
removing a tool therefore requires an explicit catalog and registration/test update.

## Build and test

From the repository root, with the .NET 10 SDK installed:

```powershell
dotnet build tools/DungeonEscape.Mcp/DungeonEscape.Mcp.csproj
dotnet test tools/DungeonEscape.Mcp.Tests/DungeonEscape.Mcp.Tests.csproj
```

The test run includes a real subprocess handshake (`initialize`, `notifications/initialized`),
`tools/list` verification of nine tools, real metadata/search/read/scene/log/diff/test/prepare calls,
denied apply without trusted approval,
rejected traversal/out-of-scope requests, and unknown-tool rejection.
It runs both executable-discovery and configured-root modes from an unrelated working directory,
verifies that a configured fixture overrides executable discovery, and checks invalid configuration
exits before transport starts with an error on stderr and no stdout.
Other tests use isolated temporary fixtures and do not modify the Unity project.
The Windows boundary test creates and removes a temporary junction; other platforms use a symlink.

## Run

Build first, then launch the DLL from its build output inside this checkout:

```powershell
dotnet tools/DungeonEscape.Mcp/bin/Debug/net10.0/DungeonEscape.Mcp.dll
```

The trusted launcher may explicitly select a checkout, including when the server is deployed elsewhere:

```powershell
dotnet tools/DungeonEscape.Mcp/bin/Debug/net10.0/DungeonEscape.Mcp.dll --repository-root "E:\dev\cburchell\apps\DungeonEscape"
```

For an MCP client, configure the executable as `dotnet` and pass the **absolute path** to that DLL,
optionally followed by separate `--repository-root` and absolute checkout path arguments. Quote paths
with spaces in a shell; in a client's argument array each path is a single value without shell quotes.
The path above is an example; use your actual checkout location.

`--repository-root` is process startup configuration only, never an MCP tool argument. It must be
a fully qualified local path to a directory containing the existing identity markers:
`DungeonEscape.sln` (file), `DungeonEscape.Core` and `DungeonEscape.Unity` (directories).
The server canonicalizes dot segments/trailing separators with .NET path APIs, then checks the root,
markers and their ancestors for symlinks/junctions/reparse points. Windows UNC/device paths are unsupported.
Invalid roots, relative paths, missing values, duplicate options or unknown options produce a clear
stderr startup error and exit code `2` before the MCP host starts. No fallback is attempted for an
invalid configured root. If a configured root later becomes unavailable, metadata becomes unavailable;
the server does not switch checkouts. Environment variables do not select a root.

When the option is omitted, the existing executable-location discovery is used.
No API key or HTTP endpoint is required. The process waits for MCP messages;
it is not an interactive command prompt. Standard input/output carry newline-delimited JSON-RPC;
host logs go to stderr. Avoid launching through `dotnet run` in a client because build output can
interfere with protocol traffic. Close stdin to shut down the server.

## Available tools

`get_project_info` takes no parameters and returns:

- `projectName`: `DungeonEscape` when the checkout is recognized.
- `unityVersion`: `m_EditorVersion` from `DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt`.
- `repositoryRoot`: absolute recognized checkout path.
- `currentBranch`: a simple symbolic branch from `.git/HEAD`.
- `defaultBranch`: a simple symbolic branch from `.git/refs/remotes/origin/HEAD`.

Unavailable values are the explicit string `not available`. No default branch is inferred from
the current branch or from conventional names such as `main`. Missing repository identity during
fallback discovery makes all fields unavailable; an invalid explicitly configured root instead fails
startup. Serialization may use the record's PascalCase property names in text content.

`search_code` accepts required `query`, optional repository-relative `path`, and optional `maxResults`.
It performs literal `OrdinalIgnoreCase` matching only; no regex, shell, external process or index is used.
`query` is limited to 128 non-control characters. `maxResults` defaults to 20 and must be from 1 to 50.
Results contain only repository-relative paths, one-based line numbers and snippets bounded to 240
characters. `truncated` is set when a result or resource limit prevents a complete search.

`read_script` accepts a required repository-relative file `path`, optional 1-based `startLine`
(default `1`), and optional `lineCount` (default `100`, maximum `200`). Its typed result reports the
requested and effective ranges, returned and total line counts, truncation, normalized relative path,
and content. Files are limited to 1 MiB and returned content to 64 KiB of UTF-8 per call. A requested
range past EOF returns a deterministic empty result. Text line endings are normalized to `\n`.

`list_scenes` takes no parameters. It deterministically discovers ordinary `.unity` files below
`DungeonEscape.Unity/Assets`, without reading scene contents, and returns repository-relative paths.
It reports nullable build-settings membership/enabled metadata from the narrowly parsed
`DungeonEscape.Unity/ProjectSettings/EditorBuildSettings.asset`. The result status is `available`,
`missing`, `malformed`, or `oversized`; membership and enabled values are explicitly `null` when the
settings are unavailable. A discovered scene absent from valid settings has `inBuildSettings: false`
and `enabled: null`. Duplicate settings entries are collapsed and enabled wins if any duplicate is
enabled. Settings entries for missing scenes do not fabricate scene results.

Scene discovery examines and returns at most 500 scene files and reads at most 256 KiB of
build settings. Per-directory entries are ordered by normalized repository-relative ordinal path;
traversal stops and `truncated` is set when a scene budget is exhausted. The Assets root and all visited
files/directories must pass the shared canonical containment and reparse-point checks. Linked content is
skipped, and scene contents are never opened.

`get_unity_console` accepts optional `maxEntries` (default `50`, range `1`–`100`) and optional
`minimumSeverity` (`info`, `warning`, `error`, or `exception`). It selects only the newest ordinary file
from this fixed project-scoped allowlist:

- `TestResults/editmode-log.txt`
- `TestResults/playmode-log.txt`
- `TestResults/unity-build-windows-log.txt`
- `DungeonEscape.Unity/Logs/local-unity-validate.log`
- `DungeonEscape.Unity/Logs/ci-unity-validate.log`
- `DungeonEscape.Unity/Logs/ci-unity-build-windows.log`

These are the deterministic outputs of the repository's Windows PowerShell and Linux CI Unity scripts.
Using only checkout-local files avoids the shared Windows/Linux Unity Editor and Player logs, which may
contain unrelated projects, user-profile details, or machine-wide activity. Callers cannot provide a
path, filename, glob, or search root; no directory is crawled. If none of these files exists, the result
has status `unavailable`. The source is reported as a logical type, never an absolute path.

Log inspection rejects a source over 10 MiB, tail-reads at most 256 KiB, returns at most 100 entries,
clips each entry to 2,000 characters, and returns at most 64 KiB of entry text. Reaching a tail, entry,
per-entry, or total-text limit sets `truncated`. NUL-containing and invalid UTF-8 sources report
`malformed`; unreadable sources report `unavailable`; oversized sources report `oversized`.

The intentionally small parser classifies common warning/error/exception text and treats other messages
as info. It attaches indented stack lines, `at` lines, UnityEngine lines, separators, and `(Filename:)`
lines to the preceding entry. A timestamp is returned only for an ISO-like prefix with an explicit `Z`
or UTC offset; otherwise it is `null`. Before output, obvious bearer tokens, authorization/API-key/
access-token/password/secret assignments are redacted. The configured repository prefix becomes
`<REPOSITORY>` and other Windows/Unix absolute paths become `<ABSOLUTE_PATH>`. Redaction is focused and
best-effort, so callers must still treat diagnostics as potentially sensitive. The tool does not read
the live Console, parse arbitrary logs, launch Unity, clear/write logs, or expose environment variables.

`get_git_diff` accepts optional `includeStaged`, `includeUnstaged`, and `includeUntrackedMetadata`
(all default `true`), `maxFiles` (default/maximum `200`), and `maxPatchCharacters`
(default/maximum `262144`). It returns repository-relative structured entries for added, modified,
deleted, renamed, copied, type-changed, and unmerged tracked paths. A path with both index and working
tree changes has both `staged` and `unstaged` set and labeled patch sections. Untracked paths are
metadata-only and ignored paths are omitted. Renames include the previous repository-relative path.
Binary changes set `binary` and never return binary patch content.

Git access uses only these fixed, local read-only command shapes, launched directly without a shell:

- `git -c core.fsmonitor=false -c core.untrackedCache=false -c submodule.recurse=false status --porcelain=v1 -z --untracked-files=all --ignored=no --ignore-submodules=all`
- the same fixed `-c` prefix followed by `diff --no-ext-diff --no-textconv --no-color --ignore-submodules=all -- <internally discovered path>`
- the same fixed `-c` prefix followed by `diff --cached --no-ext-diff --no-textconv --no-color --ignore-submodules=all -- <internally discovered path>`

Paths passed to the two diff forms come only from NUL-delimited status output and must canonicalize
inside the trusted repository. MCP callers cannot provide paths, pathspecs, commands, arguments,
branches, revisions, repositories, remotes, or environment values. Pager and prompts are disabled,
optional Git locks are disabled, and no remote/network or mutating Git operation is used.
Repository-configured filesystem monitors, untracked caches, and submodule recursion are disabled so
status/diff cannot delegate discovery to configured helper processes or nested repositories.

Each process has a 15-second timeout. Stdout is capped at 1 MiB and terminates that process when the
cap is reached; stderr is drained while retaining no more than 16 KiB and is never returned. Patch text
is capped at 32 KiB per file and 256 KiB total. File and total-patch limits may be lowered by the caller
within their fixed maxima. Any reached status/output/file/patch limit sets `truncated`; individual
clipped entries set `patchTruncated`. Process-start failure, timeout, malformed/invalid UTF-8 status,
or a missing Git working tree returns a safe `unavailable` result. Patch text reflects bounded local
repository content and may itself contain secrets; it is not copied to orchestrator traces.

`run_tests` accepts required enum `suite` (`core`, `mcp`, or `orchestrator`) and optional
`timeoutSeconds` (default `120`, range `10`–`300`). The fixed mappings are:

- `core`: `dotnet test DungeonEscape.Core.Test/DungeonEscape.Core.Test.csproj --no-restore --logger console;verbosity=minimal`
- `mcp`: `dotnet test tools/DungeonEscape.Mcp.Tests/DungeonEscape.Mcp.Tests.csproj --no-restore --logger console;verbosity=minimal`
- `orchestrator`: `dotnet test tools/DungeonEscape.AgentOrchestrator.Tests/DungeonEscape.AgentOrchestrator.Tests.csproj --no-restore --logger console;verbosity=minimal`

The executable, project, arguments, repository working directory, and small fixed .NET environment
settings are selected entirely in code. `ProcessStartInfo` runs `dotnet` directly with no shell and
closed stdin. Callers cannot provide commands, arguments, paths/projects, working directories,
environment variables, restore/install flags, filters, or scripts. `--no-restore` prevents package
restore; required assets must already exist. These suites do not intentionally use network services.

Stdout retains at most 128 KiB, stderr at most 64 KiB, and both pipes continue draining concurrently
to avoid deadlock. Timeout or cancellation kills the entire process tree. Results use the exit code as
authoritative and distinguish `passed`, `failed`, `timed-out`, `cancelled`, `unavailable`, and
`process-error`; console parsing does not guess failed test names, so `failedTests` is currently empty.
Running a suite may regenerate its normal `bin`/`obj` or test-host artifacts but does not intentionally
change source, gameplay, configuration, Git, or packages. Unity Edit/Play Mode are excluded because
their existing launchers depend on editor installation/licensing and environment-selected executables.
As with any `dotnet test` operation, the selected checkout's project/MSBuild files are trusted inputs
and may define build targets; this tool does not sandbox malicious repository content.

Because ordinary test artifacts/timestamps are side effects, MCP metadata deliberately declares
`ReadOnly=false`, `Destructive=false`, `Idempotent=false`, and `OpenWorld=false`. All current
AgentOrchestrator roles except QA deny this tool.

`prepare_write` accepts an existing approved repository-relative `path`, complete proposed replacement
text, and the expected SHA-256 of the current UTF-8 bytes. It performs no repository mutation. Existing
and proposed files are each limited to 1 MiB; NUL, invalid UTF-8, linked/reparse, generated, missing,
out-of-root, and unsupported targets fail closed. Writes use the shared approved roots but a smaller
extension allowlist: `.cs`, `.json`, `.md`, `.txt`, `.xml`, `.yml`, and `.yaml`.

The result includes canonical relative path, original/proposed SHA-256 and byte lengths, a deterministic
`sha256:` proposal identity, change status, and a line-based unified preview bounded to 128 KiB. A
truncated preview is marked and approval still binds to the complete stored proposal. Preparation stores
at most 100 short-lived in-memory proposals and is idempotent for the same path/version/content identity.

`apply_write` accepts only the returned proposal identity/path/hashes and an authenticated approval token.
The shared `tools/DungeonEscape.WriteApproval` component signs an exact canonical, line-delimited approval
payload with HMAC-SHA-256 and encodes it as `base64url(payload).base64url(signature)`. The payload contains
only version, random approval ID, proposal ID, canonical path, both hashes, proposed length, issued-at,
and expiry. The issuer recomputes the content-derived proposal identity before signing. Approval
issuance is deliberately not an MCP tool. Tokens expire after 10 minutes and signatures use constant-time
comparison. The service revalidates path policy, file existence, current hash, proposal content, token
signature/scope/time, and replay state immediately before execution. It writes a non-caller-controlled
temporary file in the same directory, flushes it, rechecks the source version, atomically moves it over the
target, verifies the resulting hash, and cleans temporary files on failure. Approval IDs are atomically
claimed before mutation, so concurrent replay cannot double-write; a failure after mutation begins
conservatively requires a fresh human approval. Metadata is
`ReadOnly=false`, `Destructive=true`, `Idempotent=false`, and `OpenWorld=false`.

The trusted AgentOrchestrator launcher generates a random 32-byte key per MCP child, injects it only into
that child's `DUNGEONESCAPE_WRITE_APPROVAL_KEY` environment, and retains the matching trusted issuer for
future AI-021 use. MCP removes the bootstrap variable immediately after parsing so Git/test subprocesses
cannot inherit it. The value is never placed in model context, MCP arguments, traces, or logs. Standalone
MCP startup without a key keeps all tools available, including `prepare_write`, but `apply_write` returns
`approval-unavailable`. A present malformed key fails startup without printing it.

There is no `approve_write` tool. Raw MCP clients cannot independently issue a valid approval. No current
model-backed AgentOrchestrator role is authorized for either write tool; AI-021 owns the human decision
that will permit trusted application code to sign an exact returned proposal.

Approved roots are hard-coded:

- `DungeonEscape.Core/`
- `DungeonEscape.Unity/Assets/DungeonEscape/Scripts/`
- `DungeonEscape.Tools.GameEditor/`
- `tools/`
- `.ai/`
- `memory-bank/`

Generated/sensitive directory segments `.git`, `Library`, `Temp`, `Obj`, `obj`, `bin`, `Build`,
`Builds`, `Logs`, `UserSettings`, `TestResults` and `artifacts` are excluded. Search and read accept only
UTF-8 files with approved source/document extensions (`.cs`, `.csproj`, `.sln`, `.props`, `.targets`,
`.razor`, `.json`, `.md`, `.txt`, `.xml`, `.yml`, `.yaml`, `.ps1`, `.sh`, `.asmdef`, `.uxml`, `.uss`).
NUL-containing, invalid UTF-8, unsupported, linked and reparse-point files are skipped by search and
rejected by the single-file reader. Missing files and directory paths are also rejected by the reader.

Per call, individual files are limited to 1 MiB, total examined bytes to 25 MiB, discovered/retained
candidate files to 5,000, returned results to 50, and snippets to 240 characters. Candidate traversal
sorts each visited directory by normalized repository-relative path, stops immediately when the 5,000th
searchable candidate exhausts the global budget, sets `truncated`, and does not visit later entries or
directories. Only the bounded retained set is stored and scanned. Relative paths are canonicalized and
must remain within both the selected checkout and an approved root. Absolute paths, `..`, excluded
directories, out-of-scope paths and linked search roots are rejected.

## Security boundaries and limitations

- Repository containment, operating-system-sensitive path comparison, canonical repository-relative
  resolution, slash-normalized reporting, and ancestor reparse-point checks are shared security
  primitives. The approved source roots, generated-directory exclusions, and text-extension allowlist
  remain the narrower source/document content policy. Scene, Unity-log, Git, and test-execution rules
  stay capability-specific rather than being broadened into one policy.
- The trusted launcher can select a validated root once at startup. Without that option, discovery
  starts at the executable's directory, finds its `tools/DungeonEscape.Mcp` ancestor and verifies the
  same identity markers. Neither mode scans for other repositories. MCP callers and working directory
  cannot select/change the root; generic host/environment configuration is disabled.
- Fixed metadata reads remain bounded to 4,096 characters. `search_code`, `read_script`, `list_scenes`,
  `get_unity_console`, and `get_git_diff` add only the
  bounded, allowlisted operations described above; `read_script` cannot access files outside the shared
  approved roots/extensions and is not arbitrary `read_file` access. Callers cannot
  supply commands or URLs. The only write surface is the exact prepare/approved-apply flow described
  above. No shell, generic Git command surface, network access, resources,
  prompts or HTTP transport is registered.
- `run_tests` is the sole bounded process-execution exception. It maps a closed enum to three fixed
  local `dotnet test --no-restore` definitions and cannot launch caller-selected commands or arguments.
- Symlinks/junctions/reparse points in discovery and metadata paths are rejected. Detached or
  malformed HEAD, `.git` files (including worktrees/submodules), shared Git directories,
  packed remote HEAD metadata and complex branch names are unavailable. `get_git_diff` is the sole
  fixed-command Git subprocess surface; it neither reads nor updates remotes. Remote references are
  never fetched, and a local origin HEAD may be stale.
- The checkout and local process environment are trusted. Marker files establish layout, not
  cryptographic repository identity. This is not an OS sandbox against a malicious local user
  changing files or links concurrently between checks and reads.
- Moving/publishing the executable outside `tools/DungeonEscape.Mcp` requires `--repository-root`;
  otherwise discovery is unavailable.
  Linked checkout paths are intentionally unsupported. Repository root exposes a local absolute
  path to the client as required by the issue.
- NuGet restore needs network access during development; tool execution does not.

Manual verification remaining: configure this DLL in the intended MCP client and confirm its
tool UI displays nine tools with the documented annotations, including destructive `apply_write`.
The protocol itself is covered by the automated test.
