param(
    [string]$Solution = "DungeonEscape.sln",
    [string]$PackagesDirectory = $env:NUGET_PACKAGES_DIRECTORY
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PackagesDirectory)) {
    dotnet restore $Solution
} else {
    dotnet restore $Solution --packages $PackagesDirectory
}
