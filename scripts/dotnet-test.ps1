param(
    [string]$Solution = "DungeonEscape.sln",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

if ($NoRestore) {
    dotnet test $Solution --no-restore
} else {
    dotnet test $Solution
}
