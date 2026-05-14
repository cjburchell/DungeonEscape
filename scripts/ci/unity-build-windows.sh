#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${CI_PROJECT_DIR:-$(pwd)}"
UNITY_PROJECT_PATH="${UNITY_PROJECT_PATH:-DungeonEscape.Unity}"
UNITY_EXECUTABLE="${UNITY_EXECUTABLE:-unity-editor}"
UNITY_LOG_FILE="${UNITY_LOG_FILE:-$PROJECT_ROOT/$UNITY_PROJECT_PATH/Logs/ci-unity-build-windows.log}"
UNITY_WINDOWS_BUILD_PATH="${UNITY_WINDOWS_BUILD_PATH:-$UNITY_PROJECT_PATH/Builds/Windows}"

mkdir -p "$(dirname "$UNITY_LOG_FILE")"
rm -rf "$PROJECT_ROOT/$UNITY_WINDOWS_BUILD_PATH"

"$UNITY_EXECUTABLE" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_ROOT/$UNITY_PROJECT_PATH" \
  -executeMethod Redpoint.DungeonEscape.UnityEditor.UnityCiBuild.BuildWindows64 \
  -logFile "$UNITY_LOG_FILE"
