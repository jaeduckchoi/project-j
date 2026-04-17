[CmdletBinding()]
param(
    [string[]]$Paths,
    [switch]$Staged
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location $repoRoot

$textExtensions = @(
    '.asset',
    '.asmdef',
    '.cginc',
    '.cmd',
    '.compute',
    '.cs',
    '.hlsl',
    '.json',
    '.md',
    '.meta',
    '.prefab',
    '.ps1',
    '.shader',
    '.sh',
    '.txt',
    '.unity',
    '.uss',
    '.uxml',
    '.yaml',
    '.yml'
)

$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$replacementChar = [string][char]0xFFFD
$suspiciousPatterns = @(
    [pscustomobject]@{
        Name = 'replacement-char'
        Regex = [regex]::new([regex]::Escape($replacementChar))
        Message = 'replacement character found'
    },
    [pscustomobject]@{
        Name = 'question-mojibake'
        Regex = [regex]::new('\?[^ -~]')
        Message = 'question-mark mojibake pattern found'
    },
    [pscustomobject]@{
        Name = 'compat-jamo-sequence'
        Regex = [regex]::new('[\u3131-\u3163]{2,}')
        Message = 'standalone Hangul jamo sequence found'
    },
    [pscustomobject]@{
        Name = 'cjk-mojibake'
        Regex = [regex]::new('[\u4E00-\u9FFF]')
        Message = 'unexpected CJK ideograph found'
    }
)

function Get-TargetPaths {
    if ($Staged) {
        $output = @(git diff --cached --name-only --diff-filter=ACMR -- 2>$null)
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to read staged file list.'
        }

        return @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }

    if ($null -ne $Paths -and @($Paths).Length -gt 0) {
        return @($Paths)
    }

    throw 'No target paths. Use -Paths or -Staged.'
}

function Resolve-RepoPath([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($path)) {
        return $path
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $path))
}

function Should-CheckFile([string]$fullPath) {
    if ([string]::IsNullOrWhiteSpace($fullPath) -or -not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return $false
    }

    $extension = [System.IO.Path]::GetExtension($fullPath)
    return $textExtensions -contains $extension
}

function Find-SuspiciousLines([string]$text) {
    $issues = @()
    $lines = $text -split "`r?`n"
    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $line = $lines[$lineIndex]
        foreach ($pattern in $suspiciousPatterns) {
            if ($pattern.Regex.IsMatch($line)) {
                $issues += [pscustomobject]@{
                    Line = $lineIndex + 1
                    Type = $pattern.Name
                    Message = $pattern.Message
                    Sample = $line.Trim()
                }
                break
            }
        }
    }

    return @($issues)
}

$relativeTargets = @(Get-TargetPaths)
$failures = @()

foreach ($target in $relativeTargets) {
    $fullPath = Resolve-RepoPath $target
    if (-not (Should-CheckFile $fullPath)) {
        continue
    }

    $bytes = [System.IO.File]::ReadAllBytes($fullPath)
    try {
        $text = $strictUtf8.GetString($bytes)
    } catch {
        $failures += [pscustomobject]@{
            Path = $target
            Issues = @(
                [pscustomobject]@{
                    Line = 0
                    Type = 'invalid-utf8'
                    Message = 'file is not valid UTF-8'
                    Sample = ''
                }
            )
        }
        continue
    }

    $issues = @(Find-SuspiciousLines $text)
    if ($issues.Length -gt 0) {
        $failures += [pscustomobject]@{
            Path = $target
            Issues = $issues
        }
    }
}

if ($failures.Length -gt 0) {
    Write-Host ''
    Write-Host 'Text encoding check failed:' -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $($failure.Path)" -ForegroundColor Yellow
        foreach ($issue in @($failure.Issues) | Select-Object -First 5) {
            if ($issue.Line -gt 0) {
                Write-Host ("   line {0}: {1}" -f $issue.Line, $issue.Message)
            } else {
                Write-Host ("   {0}" -f $issue.Message)
            }

            if (-not [string]::IsNullOrWhiteSpace($issue.Sample)) {
                Write-Host ("     {0}" -f $issue.Sample)
            }
        }
    }

    Write-Host ''
    Write-Host 'Re-save the file as UTF-8 and fix broken text before commit.' -ForegroundColor Red
    exit 1
}

Write-Host 'Text encoding check passed.'
