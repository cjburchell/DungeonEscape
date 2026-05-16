param(
    [string]$Solution = "DungeonEscape.sln",
    [string]$Unity = $env:UNITY_EXE,
    [string]$ToolVersion = "2026.1.0.1",
    [string]$Exclude = $env:RESHARPER_EXCLUDE,
    [string]$Severity = $(if ([string]::IsNullOrWhiteSpace($env:RESHARPER_SEVERITY)) { "WARNING" } else { $env:RESHARPER_SEVERITY }),
    [int]$Threshold = 0
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$SolutionPath = (Resolve-Path (Join-Path $RepoRoot $Solution)).Path
$UnityProjectPath = Join-Path $RepoRoot "DungeonEscape.Unity"
$TempRoot = Join-Path $RepoRoot ".ci\resharper-unity"
$UnityReferencesDir = Join-Path $UnityProjectPath "Logs\ReSharperReferences"
$CorePluginPath = Join-Path $UnityProjectPath "Assets\DungeonEscape\Plugins\DungeonEscape.Core.dll"
$CoreReport = Join-Path $RepoRoot "RsInspection.xml"
$UnityReport = Join-Path $RepoRoot "RsInspection.Unity.xml"
$CoreCache = Join-Path $RepoRoot "temp\core"
$UnityCache = Join-Path $RepoRoot "temp\unity"

function Test-ReSharperThreshold {
    param([int]$Threshold)

    $issueCount = 0
    $errorCount = 0
    Get-ChildItem -Path $RepoRoot -Filter "RsInspection*.xml" | ForEach-Object {
        [xml]$report = Get-Content -LiteralPath $_.FullName
        $issueTypes = @{}
        foreach ($type in $report.SelectNodes("//IssueType")) {
            $issueTypes[$type.Id] = $type.Severity
        }

        foreach ($issue in $report.SelectNodes("//Issue")) {
            $issueCount++
            if ($issueTypes[$issue.TypeId] -eq "ERROR") {
                $errorCount++
            }
        }
    }

    Write-Host "ReSharper issue count: $issueCount"
    Write-Host "ReSharper error count: $errorCount"
    if ($errorCount -gt $Threshold) {
        Write-Error "ReSharper error count $errorCount exceeds threshold $Threshold"
        return 1
    }

    return 0
}

dotnet tool install --global JetBrains.ReSharper.GlobalTools --version $ToolVersion 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool update --global JetBrains.ReSharper.GlobalTools --version $ToolVersion
}

$jb = (Get-Command jb).Source
$excludeOption = if ([string]::IsNullOrWhiteSpace($Exclude)) { @() } else { @("--exclude=$Exclude") }
$severityOption = if ([string]::IsNullOrWhiteSpace($Severity)) { @() } else { @("-e=$Severity") }

Push-Location $env:TEMP
$coreInspectArgs = @(
    $excludeOption
    $severityOption
    "-o=$CoreReport"
    "-f=xml"
    "--caches-home=$CoreCache"
    $SolutionPath
)
& $jb inspectcode @coreInspectArgs
$coreExitCode = $LASTEXITCODE
Pop-Location
if ($coreExitCode -ne 0) {
    exit $coreExitCode
}

if (Test-Path -LiteralPath $CorePluginPath) {
    New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
    Copy-Item -LiteralPath $CorePluginPath -Destination (Join-Path $TempRoot "DungeonEscape.Core.dll") -Force
} else {
    Write-Warning "Unity ReSharper project skipped because '$CorePluginPath' does not exist. Run dotnet build first."
    exit (Test-ReSharperThreshold -Threshold $Threshold)
}

New-Item -ItemType Directory -Force -Path $UnityReferencesDir | Out-Null
if ([string]::IsNullOrWhiteSpace($Unity)) {
    $Unity = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
}

$UnityEditorDir = Split-Path -Parent $Unity
$ManagedDir = Join-Path $UnityEditorDir "Data\Managed"
if (Test-Path -LiteralPath $ManagedDir) {
    Get-ChildItem -Path $ManagedDir -Recurse -Filter *.dll | Copy-Item -Destination $UnityReferencesDir -Force
} else {
    Write-Warning "Unity managed reference directory '$ManagedDir' was not found."
}

$PackageCache = Join-Path $UnityProjectPath "Library\PackageCache"
if (Test-Path -LiteralPath $PackageCache) {
    Get-ChildItem -Path $PackageCache -Recurse -Filter *.dll | Copy-Item -Destination $UnityReferencesDir -Force
}

$ScriptAssemblies = Join-Path $UnityProjectPath "Library\ScriptAssemblies"
if (Test-Path -LiteralPath $ScriptAssemblies) {
    Get-ChildItem -Path $ScriptAssemblies -Filter *.dll |
        Where-Object { $_.Name -like "Unity*.dll" -or $_.Name -like "nunit*.dll" } |
        Copy-Item -Destination $UnityReferencesDir -Force
}

$UnityProject = Join-Path $TempRoot "DungeonEscape.Unity.ReSharper.csproj"
$compileScripts = "..\..\DungeonEscape.Unity\Assets\DungeonEscape\Scripts\**\*.cs"
$compileEditor = "..\..\DungeonEscape.Unity\Assets\DungeonEscape\Editor\**\*.cs"
$compileTests = "..\..\DungeonEscape.Unity\Assets\DungeonEscape\Tests\**\*.cs"

$lines = @(
    '<Project Sdk="Microsoft.NET.Sdk">',
    '  <PropertyGroup>',
    '    <TargetFramework>netstandard2.1</TargetFramework>',
    '    <LangVersion>9.0</LangVersion>',
    '    <Nullable>disable</Nullable>',
    '    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>',
    '    <NoWarn>$(NoWarn);0649</NoWarn>',
    '  </PropertyGroup>',
    '  <ItemGroup>',
    "    <Compile Include=""$compileScripts"" />",
    "    <Compile Include=""$compileEditor"" />",
    "    <Compile Include=""$compileTests"" />",
    '    <Reference Include="DungeonEscape.Core">',
    '      <HintPath>DungeonEscape.Core.dll</HintPath>',
    '    </Reference>'
)

Get-ChildItem -Path $UnityReferencesDir -Filter *.dll | Sort-Object Name | ForEach-Object {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $relative = "..\..\DungeonEscape.Unity\Logs\ReSharperReferences\$($_.Name)"
    $lines += "    <Reference Include=""$name""><HintPath>$relative</HintPath></Reference>"
}

$lines += @(
    '  </ItemGroup>',
    '</Project>'
)

Set-Content -LiteralPath $UnityProject -Value $lines -Encoding ASCII

Push-Location $env:TEMP
$unityInspectArgs = @(
    $excludeOption
    $severityOption
    "-o=$UnityReport"
    "-f=xml"
    "--caches-home=$UnityCache"
    $UnityProject
)
& $jb inspectcode @unityInspectArgs
$unityExitCode = $LASTEXITCODE
Pop-Location
if ($unityExitCode -ne 0) {
    exit $unityExitCode
}

exit (Test-ReSharperThreshold -Threshold $Threshold)
