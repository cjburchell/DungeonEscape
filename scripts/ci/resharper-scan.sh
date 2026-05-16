#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${CI_PROJECT_DIR:-$(pwd)}"
SOLUTION="${RESHARPER_SOLUTION_NAME:-DungeonEscape.sln}"
SOLUTION_PATH="$PROJECT_ROOT/$SOLUTION"
RESHARPER_EXCLUDE="${RESHARPER_EXCLUDE:-}"
RESHARPER_SEVERITY="${RESHARPER_SEVERITY:-WARNING}"
RESHARPER_THRESHOLD="${RESHARPER_THRESHOLD:-0}"
RESHARPER_TOOL_VERSION="${RESHARPER_TOOL_VERSION:-2026.1.0.1}"
UNITY_PROJECT_PATH="${UNITY_PROJECT_PATH:-DungeonEscape.Unity}"
UNITY_REFERENCES_DIR="$PROJECT_ROOT/$UNITY_PROJECT_PATH/Logs/ReSharperReferences"
UNITY_TEMP_DIR="$PROJECT_ROOT/.ci/resharper-unity"
UNITY_PROJECT_FILE="$UNITY_TEMP_DIR/DungeonEscape.Unity.ReSharper.csproj"
PACKAGES_DIRECTORY="${NUGET_PACKAGES_DIRECTORY:-}"
DOTNET_EXE="$(command -v dotnet)"
DOTNET_SDK_PATH="$(dotnet --info | awk -F': ' '/Base Path/ {print $2; exit}' | xargs)"
DOTNET_SDK_VERSION="$(basename "$DOTNET_SDK_PATH")"
MSBUILD_DLL="$DOTNET_SDK_PATH/MSBuild.dll"

if [[ ! -f "$MSBUILD_DLL" ]]; then
  echo "MSBuild.dll was not found at '$MSBUILD_DLL'." >&2
  dotnet --info >&2
  exit 1
fi

echo "Using dotnet: $DOTNET_EXE"
echo "Using .NET SDK: $DOTNET_SDK_VERSION"
echo "Using MSBuild: $MSBUILD_DLL"

export PATH="$PATH:/root/.dotnet/tools:$HOME/.dotnet/tools"
dotnet tool install --global JetBrains.ReSharper.GlobalTools --version "$RESHARPER_TOOL_VERSION" ||
  dotnet tool update --global JetBrains.ReSharper.GlobalTools --version "$RESHARPER_TOOL_VERSION"

RESHARPER_EXCLUDE_OPTION=()
if [[ -n "$RESHARPER_EXCLUDE" ]]; then
  RESHARPER_EXCLUDE_OPTION=("--exclude=$RESHARPER_EXCLUDE")
fi

if [[ -n "$PACKAGES_DIRECTORY" ]]; then
  dotnet restore "$SOLUTION_PATH" --packages "$PACKAGES_DIRECTORY"
else
  dotnet restore "$SOLUTION_PATH"
fi

COMMON_INSPECT_OPTIONS=(
  "${RESHARPER_EXCLUDE_OPTION[@]}"
  "-e=$RESHARPER_SEVERITY"
  "-f=xml"
  "--dotnetcore=$DOTNET_EXE"
  "--dotnetcoresdk=$DOTNET_SDK_VERSION"
  "--toolset-path=$MSBUILD_DLL"
)

(cd /tmp && jb inspectcode "${COMMON_INSPECT_OPTIONS[@]}" -o="$PROJECT_ROOT/RsInspection.xml" --caches-home="$PROJECT_ROOT/temp/core" "$SOLUTION_PATH")

UNITY_SCRIPT_COUNT=$(find "$UNITY_PROJECT_PATH/Assets/DungeonEscape/Scripts" "$UNITY_PROJECT_PATH/Assets/DungeonEscape/Editor" "$UNITY_PROJECT_PATH/Assets/DungeonEscape/Tests" -name '*.cs' 2>/dev/null | wc -l | tr -d ' ')
if [[ "$UNITY_SCRIPT_COUNT" -gt 0 ]]; then
  echo "Creating CI ReSharper project for $UNITY_SCRIPT_COUNT Unity scripts."
  mkdir -p "$UNITY_TEMP_DIR"
  cp "$UNITY_PROJECT_PATH/Assets/DungeonEscape/Plugins/DungeonEscape.Core.dll" "$UNITY_TEMP_DIR/DungeonEscape.Core.dll"
  mkdir -p "$UNITY_REFERENCES_DIR"

  UNITY_EXE="${UNITY_EXE:-}"
  if [[ -z "$UNITY_EXE" && -n "${UNITY_PATH:-}" ]]; then
    UNITY_EXE="$UNITY_PATH/Editor/Unity"
  fi
  if [[ -z "$UNITY_EXE" && -x /opt/unity/Editor/Unity ]]; then
    UNITY_EXE="/opt/unity/Editor/Unity"
  fi

  if [[ -n "$UNITY_EXE" ]]; then
    UNITY_EDITOR_DIR=$(dirname "$UNITY_EXE")
    UNITY_MANAGED_DIR="$UNITY_EDITOR_DIR/Data/Managed"
    if [[ -d "$UNITY_MANAGED_DIR" ]]; then
      find "$UNITY_MANAGED_DIR" -name '*.dll' -exec cp {} "$UNITY_REFERENCES_DIR" \;
    else
      echo "Unity managed reference directory '$UNITY_MANAGED_DIR' was not found."
    fi
  else
    echo "UNITY_EXE/UNITY_PATH was not set; using any existing Unity references in $UNITY_REFERENCES_DIR."
  fi

  PACKAGE_CACHE="$PROJECT_ROOT/$UNITY_PROJECT_PATH/Library/PackageCache"
  if [[ -d "$PACKAGE_CACHE" ]]; then
    find "$PACKAGE_CACHE" -name '*.dll' -exec cp {} "$UNITY_REFERENCES_DIR" \;
  fi
  SCRIPT_ASSEMBLIES="$PROJECT_ROOT/$UNITY_PROJECT_PATH/Library/ScriptAssemblies"
  if [[ -d "$SCRIPT_ASSEMBLIES" ]]; then
    find "$SCRIPT_ASSEMBLIES" -maxdepth 1 -name '*.dll' \( -name 'Unity.*.dll' -o -name 'nunit*.dll' \) -exec cp {} "$UNITY_REFERENCES_DIR" \;
  fi

  UNITY_CORE_REFERENCE="$UNITY_REFERENCES_DIR/UnityEngine.CoreModule.dll"
  UNITY_EDITOR_REFERENCE="$UNITY_REFERENCES_DIR/UnityEditor.CoreModule.dll"
  if [[ ! -f "$UNITY_CORE_REFERENCE" || ! -f "$UNITY_EDITOR_REFERENCE" ]]; then
    echo "Unity ReSharper references are incomplete." >&2
    echo "Expected '$UNITY_CORE_REFERENCE' and '$UNITY_EDITOR_REFERENCE'." >&2
    echo "Run unity-validate first and publish DungeonEscape.Unity/Logs/ReSharperReferences/*.dll as artifacts." >&2
    exit 1
  fi

  {
    printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">'
    printf '%s\n' '  <PropertyGroup>'
    printf '%s\n' '    <TargetFramework>netstandard2.1</TargetFramework>'
    printf '%s\n' '    <LangVersion>9.0</LangVersion>'
    printf '%s\n' '    <Nullable>disable</Nullable>'
    printf '%s\n' '    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
    printf '%s\n' '    <NoWarn>$(NoWarn);0649</NoWarn>'
    printf '%s\n' '  </PropertyGroup>'
    printf '%s\n' '  <ItemGroup>'
    printf '%s\n' "    <Compile Include=\"../../$UNITY_PROJECT_PATH/Assets/DungeonEscape/Scripts/**/*.cs\" />"
    printf '%s\n' "    <Compile Include=\"../../$UNITY_PROJECT_PATH/Assets/DungeonEscape/Editor/**/*.cs\" />"
    printf '%s\n' "    <Compile Include=\"../../$UNITY_PROJECT_PATH/Assets/DungeonEscape/Tests/**/*.cs\" />"
    printf '%s\n' '    <Reference Include="DungeonEscape.Core">'
    printf '%s\n' '      <HintPath>DungeonEscape.Core.dll</HintPath>'
    printf '%s\n' '    </Reference>'
    if [[ -d "$UNITY_REFERENCES_DIR" ]]; then
      find "$UNITY_REFERENCES_DIR" -maxdepth 1 -name '*.dll' | sort | while read -r dll; do
        name=$(basename "$dll" .dll)
        printf '    <Reference Include="%s"><HintPath>%s</HintPath></Reference>\n' "$name" "$dll"
      done
    fi
    printf '%s\n' '  </ItemGroup>'
    printf '%s\n' '</Project>'
  } > "$UNITY_PROJECT_FILE"

  (cd /tmp && jb inspectcode "${COMMON_INSPECT_OPTIONS[@]}" -o="$PROJECT_ROOT/RsInspection.Unity.xml" --caches-home="$PROJECT_ROOT/temp/unity" "$UNITY_PROJECT_FILE")
else
  echo "No Unity scripts found; skipping Unity ReSharper inspection."
fi

ISSUE_COUNT=0
ERROR_COUNT=0
for REPORT in RsInspection*.xml; do
  if [[ ! -f "$REPORT" ]]; then
    continue
  fi

  REPORT_ISSUES=$(grep -o '<Issue ' "$REPORT" | wc -l | tr -d ' ')
  ISSUE_COUNT=$((ISSUE_COUNT + REPORT_ISSUES))
  ERROR_TYPES=$(grep '<IssueType ' "$REPORT" | grep 'Severity="ERROR"' | sed -n 's/.*Id="\([^"]*\)".*/\1/p' || true)
  for TYPE_ID in $ERROR_TYPES; do
    TYPE_COUNT=$(grep -c "TypeId=\"$TYPE_ID\"" "$REPORT" || true)
    ERROR_COUNT=$((ERROR_COUNT + TYPE_COUNT))
  done
done

echo "ReSharper issue count: $ISSUE_COUNT"
echo "ReSharper error count: $ERROR_COUNT"
if [[ "$ERROR_COUNT" -gt "$RESHARPER_THRESHOLD" ]]; then
  echo "ReSharper error count $ERROR_COUNT exceeds threshold $RESHARPER_THRESHOLD"
  exit 1
fi
