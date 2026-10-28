#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$UnityCliPath = 'C:\Users\USER\AppData\Local\Unity\bin\unity.exe',
    [ValidateRange(1, 120)][int]$CommandTimeoutSeconds = 30
)

# This collector only queries an existing Editor. It never compiles or runs tests.
$ErrorActionPreference = 'Stop'
$projectPath = 'C:\Users\USER\Desktop\Unity\Crazy Frog'
$expectedVersion = '6000.3.23f1'
$resultsRoot = Join-Path $projectPath 'ValidationResults'
$started = [DateTimeOffset]::UtcNow
$runId = $started.ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
$runPath = Join-Path $resultsRoot $runId
$utf8 = New-Object System.Text.UTF8Encoding($false)
$lockHandle = $null
$exitCode = 2
$commands = New-Object 'System.Collections.Generic.List[object]'
$reasons = New-Object 'System.Collections.Generic.List[object]'

function Get-Field($Object, [string]$Name) {
    if ($null -ne $Object -and $null -ne $Object.PSObject.Properties[$Name]) {
        return $Object.$Name
    }
    return $null
}

function Add-Reason([string]$Code, [string]$Stage, [string]$Message) {
    $reasons.Add([pscustomobject]@{ code = $Code; stage = $Stage; message = $Message })
}

function Write-NewText([string]$Path, [string]$Text) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $bytes = $utf8.GetBytes($Text)
        $stream.Write($bytes, 0, $bytes.Length)
    } finally { $stream.Dispose() }
}

function Same-Project($Path) {
    return ($null -ne $Path -and ([string]$Path).Replace('/', '\').TrimEnd('\') -ieq $projectPath)
}

function Is-Count($Value) {
    return (($Value -is [int] -or $Value -is [long]) -and $Value -ge 0)
}

function Set-CommandError($Record, [string]$Message) {
    $Record.status = 'error'
    $Record.error = $Message
    Add-Reason 'COMMAND_ERROR' $Record.name $Message
}

function Invoke-Query([string]$Name, [string[]]$Arguments, [bool]$Skip = $false) {
    $record = [pscustomobject][ordered]@{
        name = $Name; arguments = @($Arguments); status = 'not_run'
        exitCode = $null; jsonSuccess = $null; resultSuccess = $null
        startedAtUtc = $null; finishedAtUtc = $null
        stdoutFile = $null; stderrFile = $null; error = $null
        response = $null
    }
    $commands.Add($record)
    if ($Skip) { $record.error = 'Skipped because the required Editor connection was unavailable.'; return $record }
    $record.startedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $process = $null
    try {
        if (-not [IO.File]::Exists($UnityCliPath)) { throw "Unity CLI not found: $UnityCliPath" }
        # Fixed arguments contain neither embedded quotes nor trailing backslashes.
        # UseShellExecute=false means no PowerShell/cmd interpretation takes place.
        $info = New-Object System.Diagnostics.ProcessStartInfo
        $info.FileName = $UnityCliPath
        $info.Arguments = (($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' ')
        $info.WorkingDirectory = $projectPath
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $info.StandardOutputEncoding = $utf8
        $info.StandardErrorEncoding = $utf8
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $info
        if (-not $process.Start()) { throw 'CLI process did not start.' }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $timedOut = -not $process.WaitForExit($CommandTimeoutSeconds * 1000)
        if ($timedOut) {
            # Only terminate the query CLI process we created, never the Editor.
            $process.Kill()
            if (-not $process.WaitForExit(5000)) { throw 'Timed-out CLI could not be reaped.' }
        }
        if (-not $stdoutTask.Wait(5000) -or -not $stderrTask.Wait(5000)) { throw 'CLI output did not close.' }
        $record.exitCode = $process.ExitCode
        $record.stdoutFile = "$Name.stdout.json"
        $record.stderrFile = "$Name.stderr.txt"
        Write-NewText (Join-Path $runPath $record.stdoutFile) $stdoutTask.Result
        Write-NewText (Join-Path $runPath $record.stderrFile) $stderrTask.Result
        if ($timedOut) { throw "CLI query timed out after $CommandTimeoutSeconds seconds." }
        $record.response = $stdoutTask.Result | ConvertFrom-Json -ErrorAction Stop
        $success = Get-Field $record.response 'success'
        if ($success -isnot [bool]) { throw 'CLI response has no boolean success field.' }
        $record.jsonSuccess = $success
        if ($record.exitCode -ne 0 -or -not $success) {
            $cliErrors = Get-Field $record.response 'errors'
            throw "CLI exit=$($record.exitCode), success=$success; errors=$($cliErrors | ConvertTo-Json -Depth 10 -Compress)"
        }
        if ($Name -ne 'connection') {
            $data = Get-Field $record.response 'data'
            $target = Get-Field $data 'target'
            if (-not (Same-Project (Get-Field $target 'projectPath'))) { throw 'Response target is missing or differs from the fixed project.' }
            $result = Get-Field $data 'result'
            if ($null -eq $result) { throw 'Command response is missing data.result.' }
            $inner = Get-Field $result 'success'
            if ($null -ne $inner) {
                if ($inner -isnot [bool]) { throw 'Nested result.success is not boolean.' }
                $record.resultSuccess = $inner
                if (-not $inner) { throw 'Nested command result reported success=false.' }
            }
        }
        $record.status = 'success'
    } catch {
        Set-CommandError $record $_.Exception.Message
    } finally {
        if ($null -ne $process) { $process.Dispose() }
        $record.finishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }
    return $record
}

$report = [ordered]@{
    schemaVersion = 1; runId = $runId; startedAtUtc = $started.ToString('o')
    finishedAtUtc = $null; projectPath = $projectPath; expectedUnityVersion = $expectedVersion
    unityVersion = $null; unityCliPath = $UnityCliPath; reportPath = (Join-Path $runPath 'report.json')
    scope = 'read_only_collection'; overall = 'ERROR'; exitCode = 2
    lock = [ordered]@{ acquired = $false; path = (Join-Path $resultsRoot '.collector.lock') }
    editor = [ordered]@{ connection = 'not_queried'; pid = $null; playMode = $null }
    compile = [ordered]@{ queryStatus = 'not_run'; state = $null; compiling = $null; failed = $null; freshCompilation = $false; execution = 'not_run'; evidence = 'status_only' }
    console = [ordered]@{ queryStatus = 'not_run'; errors = $null; warnings = $null; session = $null; cursor = $null; sampledUtc = $null; ageMs = $null; entries = @(); dropped = $null; reset = $null }
    tests = [ordered]@{
        editMode = [ordered]@{ queryStatus = 'not_run'; discovered = $null; execution = 'not_run' }
        playMode = [ordered]@{ queryStatus = 'not_run'; discovered = $null; execution = 'not_run' }
    }
    reasons = @(); commands = @()
}

try {
    [void][IO.Directory]::CreateDirectory($resultsRoot)
    # New-Item without -Force refuses even a hypothetical run-id collision.
    [void](New-Item -ItemType Directory -Path $runPath -ErrorAction Stop)
    try {
        $lockHandle = [IO.File]::Open($report.lock.path, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $report.lock.acquired = $true
    } catch { throw "PROJECT_LOCK_UNAVAILABLE: $($_.Exception.Message)" }

    $common = @('--project-path', $projectPath, '--format', 'json', '--non-interactive', '--no-banner')
    $labels = @('--caller', 'plugin', '--skill', 'unity-cli')
    $connection = Invoke-Query 'connection' (@('status') + $common)
    $connected = $false
    if ($connection.status -eq 'success') {
        $instances = @(Get-Field (Get-Field $connection.response 'data') 'instances')
        $matching = @($instances | Where-Object { Same-Project (Get-Field $_ 'projectPath') })
        $ready = @($matching | Where-Object { (Get-Field $_ 'state') -eq 'ready' -and (Get-Field $_ 'role') -eq 'main' })
        if ($ready.Count -ne 1) {
            Set-CommandError $connection 'Expected exactly one ready main Editor for the fixed project.'
        } else {
            $connected = $true
            $report.editor.connection = 'ready'
            $report.editor.pid = Get-Field $ready[0] 'pid'
            $report.unityVersion = Get-Field $ready[0] 'editorVersion'
            if ($report.unityVersion -ne $expectedVersion) {
                $connected = $false
                Set-CommandError $connection "Expected Unity $expectedVersion; received $($report.unityVersion)."
            }
        }
    }
    if (-not $connected) { $report.editor.connection = 'error'; Add-Reason 'CONNECTION_UNAVAILABLE' 'connection' 'Required Editor connection or version could not be confirmed.' }

    $queries = @(
        @{ name = 'editor-status'; args = @('editor', 'status') },
        @{ name = 'console-status'; args = @('console', 'status') },
        @{ name = 'console-entries'; args = @('console', 'get', '--level', 'warn', '--tail', '200') },
        @{ name = 'compile-status'; args = @('script', 'recompile_status') },
        @{ name = 'editmode-discovery'; args = @('test', 'list', '--mode', 'editor') },
        @{ name = 'playmode-discovery'; args = @('test', 'list', '--mode', 'playmode') }
    )
    foreach ($query in $queries) {
        $record = Invoke-Query $query.name (@('command') + $query.args + $common + $labels) (-not $connected)
        if ($record.status -ne 'success') { continue }
        $result = $record.response.data.result
        switch ($query.name) {
            'editor-status' {
                $compiling = Get-Field $result 'compiling'
                if ($compiling -isnot [bool] -or -not (Same-Project (Get-Field $result 'projectPath'))) { Set-CommandError $record 'Invalid Editor status payload.'; break }
                $report.editor.playMode = Get-Field $result 'playMode'
                $report.compile.compiling = $compiling
            }
            'console-status' {
                $truth = Get-Field $result 'groundTruth'
                $errors = Get-Field $truth 'consoleErrors'
                $warnings = Get-Field $truth 'consoleWarnings'
                $failed = Get-Field $truth 'compilationFailed'
                $age = Get-Field $truth 'ageMs'
                if (-not (Is-Count $errors) -or -not (Is-Count $warnings) -or $failed -isnot [bool] -or $null -eq $age -or $age -lt 0 -or $age -gt 2000) {
                    Set-CommandError $record 'Console ground truth is missing, invalid, or older than 2000ms.'; break
                }
                $report.console.queryStatus = 'success'
                $report.console.errors = $errors; $report.console.warnings = $warnings
                $report.console.session = Get-Field $result 'session'; $report.console.cursor = Get-Field $result 'cursor'
                $report.console.sampledUtc = Get-Field $truth 'sampledUtc'; $report.console.ageMs = $age
                $report.compile.failed = $failed
                if ($failed) { Add-Reason 'COMPILATION_FAILED' 'console-status' 'Editor currently reports script compilation failure.' }
                if ($errors -gt 0) { Add-Reason 'CONSOLE_ERRORS' 'console-status' "Editor Console contains $errors error(s)." }
            }
            'console-entries' {
                if ($null -eq $result.PSObject.Properties['entries']) { Set-CommandError $record 'Console entries array is missing.'; break }
                $report.console.entries = @($result.entries)
                $report.console.dropped = Get-Field $result 'dropped'; $report.console.reset = Get-Field $result 'reset'
                if ($report.console.dropped -eq $true -or $report.console.reset -eq $true) { Add-Reason 'CONSOLE_EVIDENCE_PARTIAL' 'console-entries' 'Console capture reports dropped entries or a session reset.' }
            }
            'compile-status' {
                $state = Get-Field $result 'status'; $failed = Get-Field $result 'failed'
                if ($state -notin @('idle', 'triggered', 'compiling', 'completed', 'up_to_date') -or $failed -isnot [bool]) { Set-CommandError $record 'Invalid compile status payload.'; break }
                $report.compile.queryStatus = 'success'; $report.compile.state = $state
                if ($failed -or (Get-Field $result 'compilationFailed') -eq $true) { $report.compile.failed = $true; Add-Reason 'COMPILATION_FAILED' 'compile-status' 'Compile status reports failure.' }
            }
            default {
                $mode = if ($query.name -eq 'editmode-discovery') { 'editMode' } else { 'playMode' }
                $expectedMode = if ($mode -eq 'editMode') { 'EditMode' } else { 'PlayMode' }
                $count = Get-Field $result 'Count'
                if (-not (Is-Count $count) -or (Get-Field $result 'Mode') -ne $expectedMode -or $record.resultSuccess -ne $true) { Set-CommandError $record 'Invalid test discovery payload.'; break }
                $report.tests[$mode].queryStatus = 'success'; $report.tests[$mode].discovered = $count
                if ($count -eq 0) { Add-Reason 'NO_TESTS' $query.name "No $expectedMode tests were discovered." }
            }
        }
    }
    # Explicitly retain error vs. not_run for the public summary.
    foreach ($record in $commands) {
        if ($record.status -eq 'error') {
            switch ($record.name) {
                'console-status' { $report.console.queryStatus = 'error' }
                'compile-status' { $report.compile.queryStatus = 'error' }
                'editmode-discovery' { $report.tests.editMode.queryStatus = 'error' }
                'playmode-discovery' { $report.tests.playMode.queryStatus = 'error' }
            }
        }
    }
    Add-Reason 'VALIDATION_NOT_EXECUTED' 'scope' 'Collection only: no new compilation, EditMode tests, PlayMode tests, build, or gameplay validation executed.'
    if ($report.compile.compiling -eq $true -or $report.compile.state -in @('triggered', 'compiling')) { Add-Reason 'COMPILATION_IN_PROGRESS' 'compile-status' 'Compilation was in progress; status is not a completed verdict.' }
    if (@($commands | Where-Object { $_.status -eq 'error' }).Count -gt 0) {
        $report.overall = 'ERROR'; $exitCode = 2
    } elseif ($report.compile.failed -eq $true -or $report.console.errors -gt 0) {
        $report.overall = 'FAIL'; $exitCode = 1
    } else {
        # P1 cannot produce PASS: it deliberately does not execute validation.
        $report.overall = 'INCOMPLETE'; $exitCode = 3
    }
} catch {
    Add-Reason 'COLLECTOR_ERROR' 'collector' $_.Exception.Message
    $report.overall = 'ERROR'; $exitCode = 2
} finally {
    $report.finishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $report.exitCode = $exitCode
    $report.reasons = @($reasons.ToArray())
    $report.commands = @($commands.ToArray())
    try {
        if (-not [IO.Directory]::Exists($runPath)) { throw 'Run directory was not created; report cannot be saved.' }
        $temporary = Join-Path $runPath 'report.json.tmp'
        Write-NewText $temporary ($report | ConvertTo-Json -Depth 60)
        # File.Move does not replace an existing destination (also on Windows PS 5.1).
        [IO.File]::Move($temporary, $report.reportPath)
        # One JSON object on stdout: WSL can parse it without scraping human text.
        [pscustomobject]@{ runId = $runId; overall = $report.overall; exitCode = $exitCode; reportPath = $report.reportPath } | ConvertTo-Json -Compress
    } catch {
        [Console]::Error.WriteLine("CFCheck report write failed: $($_.Exception.Message)")
        $exitCode = 2
    } finally {
        if ($null -ne $lockHandle) { $lockHandle.Dispose() }
    }
}
exit $exitCode
