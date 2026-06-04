#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Release}"
TOOLS_PACKAGE_DIR="${TOOLS_PACKAGE_DIR:-artifacts/tools}"
MONSTER_EDITOR_PROJECT="${MONSTER_EDITOR_PROJECT:-DungeonEscape.Tools.MonsterEditor/DungeonEscape.Tools.MonsterEditor.csproj}"
MONSTER_EDITOR_RUNTIME="${MONSTER_EDITOR_RUNTIME:-win-x64}"

MONSTER_EDITOR_OUTPUT="$TOOLS_PACKAGE_DIR/DungeonEscape.Tools.MonsterEditor/$MONSTER_EDITOR_RUNTIME"

rm -rf "$TOOLS_PACKAGE_DIR"
mkdir -p "$MONSTER_EDITOR_OUTPUT"

dotnet restore "$MONSTER_EDITOR_PROJECT" --runtime "$MONSTER_EDITOR_RUNTIME"

dotnet publish "$MONSTER_EDITOR_PROJECT" \
  --configuration "$CONFIGURATION" \
  --runtime "$MONSTER_EDITOR_RUNTIME" \
  --self-contained true \
  --no-restore \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=false \
  -p:DebugType=embedded \
  --output "$MONSTER_EDITOR_OUTPUT"

if command -v zip >/dev/null 2>&1; then
  (
    cd "$TOOLS_PACKAGE_DIR/DungeonEscape.Tools.MonsterEditor"
    zip -r "../DungeonEscape.Tools.MonsterEditor-$MONSTER_EDITOR_RUNTIME.zip" "$MONSTER_EDITOR_RUNTIME"
  )
fi