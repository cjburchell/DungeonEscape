#!/usr/bin/env bash
set -euo pipefail

SOLUTION="${SOLUTION:-DungeonEscape.sln}"

dotnet test "$SOLUTION" --no-restore
