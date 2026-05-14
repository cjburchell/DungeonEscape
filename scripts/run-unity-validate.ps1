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
$LogDir = Join-Path $ProjectPath "Logs"
$Log = Join-Path $LogDir "local-unity-validate.log"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

$Arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "Redpoint.DungeonEscape.UnityEditor.UnityCiValidation.ValidateProject",
    "-logFile", $Log
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -NoNewWindow -Wait -PassThru
exit $Process.ExitCode
