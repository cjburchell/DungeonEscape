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
$ResultsDir = Join-Path $RepoRoot "TestResults"
$Results = Join-Path $ResultsDir "editmode-results.xml"
$Log = Join-Path $ResultsDir "editmode-log.txt"

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

Remove-Item -LiteralPath $Results -Force -ErrorAction SilentlyContinue

$Arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $Results,
    "-logFile", $Log
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -NoNewWindow -Wait -PassThru

$ExitCode = $Process.ExitCode
if ($ExitCode -eq 0 -and -not (Test-Path -LiteralPath $Results)) {
    Write-Error "Unity Edit Mode test results were not created. Check the log at '$Log'."
    exit 1
}

exit $ExitCode
