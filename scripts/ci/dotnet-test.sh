#!/usr/bin/env bash
set -euo pipefail

SOLUTION="${SOLUTION:-DungeonEscape.sln}"
PROJECT_ROOT="${CI_PROJECT_DIR:-$(pwd)}"
RESULTS_DIR="${DOTNET_TEST_RESULTS_DIR:-$PROJECT_ROOT/TestResults/dotnet}"

mkdir -p "$RESULTS_DIR"

dotnet test "$SOLUTION" \
  --no-restore \
  --logger "trx;LogFileName=dotnet-tests.trx" \
  --results-directory "$RESULTS_DIR"
