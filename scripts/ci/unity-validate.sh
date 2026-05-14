#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${CI_PROJECT_DIR:-$(pwd)}"
UNITY_PROJECT_PATH="${UNITY_PROJECT_PATH:-DungeonEscape.Unity}"
UNITY_EXECUTABLE="${UNITY_EXECUTABLE:-unity-editor}"
UNITY_LOG_FILE="${UNITY_LOG_FILE:-$PROJECT_ROOT/$UNITY_PROJECT_PATH/Logs/ci-unity-validate.log}"
UNITY_REFERENCES_DIR="$PROJECT_ROOT/$UNITY_PROJECT_PATH/Logs/ReSharperReferences"

mkdir -p "$(dirname "$UNITY_LOG_FILE")"

"$UNITY_EXECUTABLE" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_ROOT/$UNITY_PROJECT_PATH" \
  -executeMethod Redpoint.DungeonEscape.UnityEditor.UnityCiValidation.ValidateProject \
  -logFile "$UNITY_LOG_FILE"

mkdir -p "$UNITY_REFERENCES_DIR"
find /opt/unity/Editor/Data/Managed -name '*.dll' -exec cp {} "$UNITY_REFERENCES_DIR/" \;
find "$PROJECT_ROOT/$UNITY_PROJECT_PATH/Library/PackageCache" -name '*.dll' -exec cp {} "$UNITY_REFERENCES_DIR/" \; || true
