#!/usr/bin/env bash
set -euo pipefail

SOLUTION="${SOLUTION:-DungeonEscape.sln}"
PACKAGES_DIRECTORY="${NUGET_PACKAGES_DIRECTORY:-}"

if [[ -n "$PACKAGES_DIRECTORY" ]]; then
  dotnet restore "$SOLUTION" --packages "$PACKAGES_DIRECTORY"
else
  dotnet restore "$SOLUTION"
fi
