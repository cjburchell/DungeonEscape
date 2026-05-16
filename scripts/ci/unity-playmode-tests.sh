#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${CI_PROJECT_DIR:-$(pwd)}"
UNITY_PROJECT_PATH="${UNITY_PROJECT_PATH:-DungeonEscape.Unity}"
UNITY_EXECUTABLE="${UNITY_EXECUTABLE:-unity-editor}"
RESULTS_DIR="${UNITY_TEST_RESULTS_DIR:-$PROJECT_ROOT/TestResults}"
RESULTS="$RESULTS_DIR/playmode-results.xml"
LOG="$RESULTS_DIR/playmode-log.txt"

mkdir -p "$RESULTS_DIR"
rm -f "$RESULTS"

set +e
"$UNITY_EXECUTABLE" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_ROOT/$UNITY_PROJECT_PATH" \
  -runTests \
  -testPlatform playmode \
  -testResults "$RESULTS" \
  -logFile "$LOG"
UNITY_EXIT_CODE=$?
set -e

if [[ "$UNITY_EXIT_CODE" -ne 0 ]]; then
  echo "Unity Play Mode tests exited with code $UNITY_EXIT_CODE. Log tail:" >&2
  tail -n 200 "$LOG" >&2 || true
  exit "$UNITY_EXIT_CODE"
fi

if [[ ! -f "$RESULTS" ]]; then
  echo "Unity Play Mode test results were not created. Log tail:" >&2
  tail -n 200 "$LOG" >&2 || true
  exit 1
fi
