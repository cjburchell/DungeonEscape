#!/usr/bin/env bash
set -euo pipefail

SOLUTION="${SOLUTION:-DungeonEscape.sln}"

dotnet build "$SOLUTION" --no-restore
