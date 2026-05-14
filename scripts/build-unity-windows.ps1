param(
    [string]$Unity = $env:UNITY_EXE
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Unity)) {
    $Unity = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $Unity)) {
    Write-Error "Unity executable not found at '$Unity'. Set UNITY_EXE or pass -Unity with the Unity Editor path."
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectPath = Resolve-Path (Join-Path $RepoRoot "DungeonEscape.Unity")
$BuildPath = Join-Path $ProjectPath "Builds\Windows"
$ResultsDir = Join-Path $RepoRoot "TestResults"
$Log = Join-Path $ResultsDir "unity-build-windows-log.txt"

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
Remove-Item -LiteralPath $BuildPath -Recurse -Force -ErrorAction SilentlyContinue

$Arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "Redpoint.DungeonEscape.UnityEditor.UnityCiBuild.BuildWindows64",
    "-logFile", $Log
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -NoNewWindow -Wait -PassThru
exit $Process.ExitCode
