param(
    [string]$Solution = "DungeonEscape.sln",
    [switch]$NoRestore,
    [string]$ResultsDirectory = "TestResults/dotnet"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ResultsPath = Join-Path $RepoRoot $ResultsDirectory
New-Item -ItemType Directory -Force -Path $ResultsPath | Out-Null

$Arguments = @(
    "test",
    $Solution,
    "--logger", "trx;LogFileName=dotnet-tests.trx",
    "--results-directory", $ResultsPath
)

if ($NoRestore) {
    $Arguments += "--no-restore"
}

dotnet @Arguments
