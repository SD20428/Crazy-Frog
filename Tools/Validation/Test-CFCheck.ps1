#requires -Version 7.0
[CmdletBinding()]
param([switch]$IncludeLive)

# Infrastructure checks only. No compilation, Play Mode, or game tests.
$ErrorActionPreference = 'Stop'
$collector = Join-Path $PSScriptRoot 'Invoke-CFCheck.ps1'
$schema = Join-Path $PSScriptRoot 'report.schema.json'
$projectPath = 'C:\Users\USER\Desktop\Unity\Crazy Frog'
$checks = [Collections.Generic.List[object]]::new()

function Assert-Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Collector check failed: $Name" }
    $checks.Add([pscustomobject]@{ name = $Name; status = 'PASS' })
}

function Run-Collector([string[]]$ExtraArguments = @()) {
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    foreach ($arg in (@('-NoProfile', '-NonInteractive', '-File', $collector) + $ExtraArguments)) { $info.ArgumentList.Add($arg) }
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new(); $process.StartInfo = $info
    try {
        [void]$process.Start()
        $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(300000)) { $process.Kill(); throw 'Collector check timed out.' }
        $stdout = $outTask.GetAwaiter().GetResult(); $stderr = $errTask.GetAwaiter().GetResult()
        $locator = $stdout | ConvertFrom-Json
        Assert-Check ($process.ExitCode -eq $locator.exitCode) 'Process exit equals report locator exit'
        Assert-Check (Test-Json -LiteralPath $locator.reportPath -SchemaFile $schema) 'Report satisfies JSON Schema'
        $report = Get-Content -LiteralPath $locator.reportPath -Raw | ConvertFrom-Json
        Assert-Check ($report.runId -eq $locator.runId -and $report.overall -eq $locator.overall) 'Report identity equals locator'
        return $report
    } finally { $process.Dispose() }
}

$tokens = $null; $parseErrors = $null
[Management.Automation.Language.Parser]::ParseFile($collector, [ref]$tokens, [ref]$parseErrors) | Out-Null
Assert-Check ($parseErrors.Count -eq 0) 'PowerShell collector syntax'

$missing = Run-Collector @('-UnityCliPath', (Join-Path $PSScriptRoot 'nonexistent-unity.exe'))
Assert-Check ($missing.overall -eq 'ERROR' -and $missing.exitCode -eq 2) 'Missing CLI cannot pass'
Assert-Check ($missing.commands[0].status -eq 'error' -and @($missing.commands | Where-Object status -eq 'not_run').Count -eq 6) 'Connection failure skips dependent queries'

# whoami is a harmless real process that rejects Unity CLI arguments.
# This checks the nonzero child-process path without touching Unity.
$failedQuery = Run-Collector @('-UnityCliPath', 'C:\Windows\System32\whoami.exe')
Assert-Check ($failedQuery.overall -eq 'ERROR' -and $failedQuery.commands[0].exitCode -ne 0 -and $null -ne $failedQuery.commands[0].exitCode) 'Failed native query cannot pass'

$lock = [IO.File]::Open((Join-Path $projectPath 'ValidationResults/.collector.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $locked = Run-Collector @('-UnityCliPath', (Join-Path $PSScriptRoot 'nonexistent-unity.exe'))
    Assert-Check ($locked.overall -eq 'ERROR' -and -not $locked.lock.acquired -and $locked.commands.Count -eq 0) 'Competing invocation rejected before any CLI query'
    Assert-Check (@($locked.reasons | Where-Object message -Like '*PROJECT_LOCK_UNAVAILABLE*').Count -eq 1) 'Lock rejection reason retained'
} finally { $lock.Dispose() }

if ($IncludeLive) {
    $first = Run-Collector
    $firstHash = (Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash
    $second = Run-Collector
    Assert-Check ($first.runId -ne $second.runId -and (Split-Path $first.reportPath) -ne (Split-Path $second.reportPath)) 'Repeat runs have distinct directories'
    Assert-Check ($firstHash -eq (Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash) 'Earlier report preserved'
    foreach ($report in @($first, $second)) {
        Assert-Check (@($report.commands | Where-Object { $_.status -ne 'success' -or $_.exitCode -ne 0 -or $_.jsonSuccess -ne $true }).Count -eq 0) 'All seven live queries succeeded'
        Assert-Check ($report.overall -eq 'INCOMPLETE' -and $report.exitCode -eq 3) 'Collection does not claim full validation PASS'
        Assert-Check (-not $report.compile.freshCompilation -and $report.compile.execution -eq 'not_run' -and $report.tests.editMode.execution -eq 'not_run' -and $report.tests.playMode.execution -eq 'not_run') 'No compilation or test execution claimed'
    }
}

[pscustomobject]@{
    scope = 'collector_infrastructure_checks'
    status = 'PASS'
    liveIncluded = [bool]$IncludeLive
    checks = $checks.ToArray()
} | ConvertTo-Json -Depth 8
