param(
    [string]$Solution = "DungeonEscape.sln",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

if ($NoRestore) {
    dotnet build $Solution --no-restore
} else {
    dotnet build $Solution
}
