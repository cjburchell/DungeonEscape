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

"$UNITY_EXECUTABLE" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_ROOT/$UNITY_PROJECT_PATH" \
  -runTests \
  -testPlatform PlayMode \
  -testResults "$RESULTS" \
  -logFile "$LOG"

if [[ ! -f "$RESULTS" ]]; then
  echo "Unity Play Mode test results were not created. Check the log at '$LOG'." >&2
  exit 1
fi
