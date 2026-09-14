[CmdletBinding()]
param(
    [ValidateSet('Build','Verify','Capture','Baseline','Structure','Refactor')]
    [string]$Task = 'Build',
    [string]$EngineExecutable,
    [ValidateSet('Adaptive','Coverage','HUD','Popups')]
    [string]$CaptureMode = 'Adaptive',
    [string]$Sizes = '1280x720,1920x1080,1189x862'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$projectFile = Join-Path $projectRoot 'projectJ.uproject'
$projectInfo = Get-Content -LiteralPath $projectFile -Raw | ConvertFrom-Json
if (-not $EngineExecutable) {
    $engineRoot = $env:UNREAL_ENGINE_ROOT
    $installIndex = Join-Path $env:ProgramData 'Epic/UnrealEngineLauncher/LauncherInstalled.dat'
    if (-not $engineRoot -and (Test-Path -LiteralPath $installIndex)) {
        $installed = Get-Content -LiteralPath $installIndex -Raw | ConvertFrom-Json
        $match = $installed.InstallationList | Where-Object { $_.AppName -eq "UE_$($projectInfo.EngineAssociation)" } | Select-Object -First 1
        if ($match) { $engineRoot = $match.InstallLocation }
    }
    if (-not $engineRoot) { throw 'Set UNREAL_ENGINE_ROOT or pass -EngineExecutable with UnrealEditor.exe.' }
    $EngineExecutable = Join-Path $engineRoot 'Engine/Binaries/Win64/UnrealEditor.exe'
}
$EngineExecutable = (Resolve-Path -LiteralPath $EngineExecutable).Path
$commandlet = $Task -in @('Build','Structure','Refactor')
if ($commandlet) {
    $EngineExecutable = Join-Path (Split-Path $EngineExecutable) 'UnrealEditor-Cmd.exe'
}
$entry = Join-Path $PSScriptRoot 'entry.py'
$outputRoot = Join-Path $projectRoot 'Saved/RefactorQA'
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$log = Join-Path $outputRoot ("launcher_{0}.log" -f $Task.ToLowerInvariant())
$arguments = @(('"{0}"' -f $projectFile), "-JongguTask=$Task", '-unattended', '-nop4', '-nosplash', '-nosound', ('-abslog="{0}"' -f $log))
if ($commandlet) {
    $arguments += @('-run=pythonscript', ('-script="{0}"' -f $entry), '-nullrhi')
} else {
    $arguments += '-ExecutePythonScript="' + $entry + '"'
    if ($Task -eq 'Verify') { $arguments += '-JongguPrototypeQA' }
    if ($Task -eq 'Baseline') { $arguments += '-JongguBaselineQA' }
    if ($Task -eq 'Capture') {
        $marker = @{Adaptive='-JongguAdaptiveHUDQA';Coverage='-JongguAdaptiveHUDQA';HUD='-JongguHUDQA';Popups='-JongguPopupQA'}[$CaptureMode]
        $arguments += @($marker, '-RenderOffscreen', "-PopupSizes=$Sizes")
        if ($CaptureMode -eq 'Coverage') { $arguments += '-AdaptiveCoverageOnly' }
    }
}
# Floating PIE writes window preferences on exit; restore the exact previous bytes.
$preferences = Join-Path $projectRoot 'Saved/Config/WindowsEditor/EditorPerProjectUserSettings.ini'
$hadPreferences = Test-Path -LiteralPath $preferences
$preferenceBytes = if ($hadPreferences) { [IO.File]::ReadAllBytes($preferences) } else { $null }
$started = [DateTime]::UtcNow
try {
    $run = Start-Process -FilePath $EngineExecutable -ArgumentList ($arguments -join ' ') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -Wait
    if ($run.ExitCode -ne 0) { throw "Unreal exited with $($run.ExitCode). See $log" }
    $reportPath = switch ($Task) {
        'Build' { 'Saved/PrototypeQA/build_report.json' }
        'Verify' { 'Saved/PrototypeQA/pie_report.json' }
        'Baseline' { 'Saved/PrototypeQA/baseline_collision.json' }
        'Structure' { 'Saved/RefactorQA/structure_report.json' }
        'Refactor' { 'Saved/RefactorQA/asset_migration_report.json' }
        'Capture' {
            if ($CaptureMode -eq 'Coverage') { 'Saved/AdaptiveHUDQA/latest_coverage_report.json' }
            elseif ($CaptureMode -eq 'Adaptive') { 'Saved/AdaptiveHUDQA/latest_capture_report.json' }
            elseif ($CaptureMode -eq 'HUD') { 'Saved/MainHUDQA/latest_capture_report.json' }
            else { 'Saved/PopupDesignQA/latest_capture_report.json' }
        }
    }
    if ($reportPath) {
        $resultFile = Get-Item -LiteralPath (Join-Path $projectRoot $reportPath)
        if ($resultFile.LastWriteTimeUtc -lt $started) { throw "Stale report: $reportPath" }
        $result = Get-Content -LiteralPath $resultFile.FullName -Raw | ConvertFrom-Json
        if ($Task -ne 'Baseline' -and -not $result.success) { throw "Validation failed: $($resultFile.FullName)" }
        Write-Output $resultFile.FullName
    } else { Write-Output $log }
} finally {
    if (-not $commandlet) {
        if ($hadPreferences) { [IO.File]::WriteAllBytes($preferences, $preferenceBytes) }
        elseif (Test-Path -LiteralPath $preferences) {
            $resolvedPreferences = (Resolve-Path -LiteralPath $preferences).Path
            if (-not $resolvedPreferences.StartsWith((Join-Path $projectRoot 'Saved/Config'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected preferences path' }
            Remove-Item -LiteralPath $resolvedPreferences
        }
    }
}
