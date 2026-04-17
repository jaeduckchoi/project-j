[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location $repoRoot

git config core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to configure core.hooksPath.'
}

$configuredPath = git config --get core.hooksPath
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to read configured core.hooksPath.'
}

Write-Host ("Configured Git hooks path: {0}" -f $configuredPath)
Write-Host 'Pre-commit text encoding checks are now enabled.'
