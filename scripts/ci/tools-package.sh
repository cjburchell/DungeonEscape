#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Release}"
TOOLS_PACKAGE_DIR="${TOOLS_PACKAGE_DIR:-artifacts/tools}"
GAME_EDITOR_PROJECT="${GAME_EDITOR_PROJECT:-DungeonEscape.Tools.GameEditor/DungeonEscape.Tools.GameEditor.csproj}"
GAME_EDITOR_RUNTIME="${GAME_EDITOR_RUNTIME:-win-x64}"

GAME_EDITOR_OUTPUT="$TOOLS_PACKAGE_DIR/DungeonEscape.Tools.GameEditor/$GAME_EDITOR_RUNTIME"

rm -rf "$TOOLS_PACKAGE_DIR"
mkdir -p "$GAME_EDITOR_OUTPUT"

dotnet restore "$GAME_EDITOR_PROJECT" --runtime "$GAME_EDITOR_RUNTIME"

dotnet publish "$GAME_EDITOR_PROJECT" \
  --configuration "$CONFIGURATION" \
  --runtime "$GAME_EDITOR_RUNTIME" \
  --self-contained true \
  --no-restore \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=false \
  -p:DebugType=embedded \
  --output "$GAME_EDITOR_OUTPUT"

if command -v zip >/dev/null 2>&1; then
  (
    cd "$TOOLS_PACKAGE_DIR/DungeonEscape.Tools.GameEditor"
    zip -r "../DungeonEscape.Tools.GameEditor-$GAME_EDITOR_RUNTIME.zip" "$GAME_EDITOR_RUNTIME"
  )
fi