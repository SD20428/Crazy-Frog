#requires -Version 5.1
[CmdletBinding()]
param([string]$BackupRoot = 'C:\Users\USER\Desktop\Unity\CrazyFrog-RecoveryBackups')
$ErrorActionPreference = 'Stop'
$projectPath = 'C:\Users\USER\Desktop\Unity\Crazy Frog'
$utf8 = New-Object Text.UTF8Encoding($false)
$backupRootPath = [IO.Path]::GetFullPath($BackupRoot).TrimEnd('\')
if ($backupRootPath -ieq $projectPath -or $backupRootPath.StartsWith($projectPath + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup destination must be outside the project.' }
$roots = @('Assets', 'Packages', 'ProjectSettings', 'Documentation', 'Tools', 'Backups', 'ValidationResults')
$rootFiles = @('.gitignore', '.gitattributes', '.vsconfig')
$lockHandle = $null

function Source-Files {
    foreach ($root in $roots) {
        $path = Join-Path $projectPath $root
        if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Required backup root missing: $root" }
        if (@(Get-ChildItem -LiteralPath $path -Force -Recurse -Attributes ReparsePoint).Count -gt 0) { throw "Reparse point in $root; inspect external dependency before backup." }
        Get-ChildItem -LiteralPath $path -Recurse -File -Force | Where-Object { $_.FullName -ine (Join-Path $projectPath 'ValidationResults\.collector.lock') }
    }
    foreach ($name in $rootFiles) { Get-Item -LiteralPath (Join-Path $projectPath $name) }
}

function New-TextFile([string]$Path, [string]$Text) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $bytes = $utf8.GetBytes($Text); $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
}

try {
    $lockHandle = [IO.File]::Open((Join-Path $projectPath 'ValidationResults\.collector.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $files = @(Source-Files | Sort-Object FullName)
    $sourceBytes = [long](($files | Measure-Object Length -Sum).Sum)
    # Allow an uncompressed archive, full extraction, and generous overhead.
    $requiredFreeBytes = $sourceBytes * 3 + 100MB
    $drive = New-Object IO.DriveInfo([IO.Path]::GetPathRoot($backupRootPath))
    if ($drive.AvailableFreeSpace -lt $requiredFreeBytes) { throw 'Insufficient free space for archive and restore verification.' }
    $preflight = [ordered]@{ sourceBytes = $sourceBytes; sourceFiles = $files.Count; freeBytes = $drive.AvailableFreeSpace; requiredFreeBytes = $requiredFreeBytes }
    Write-Host ('Preflight: ' + ($preflight | ConvertTo-Json -Compress))
    $backupId = 'CrazyFrog-P2-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
    [void][IO.Directory]::CreateDirectory($backupRootPath)
    $backupPath = Join-Path $backupRootPath $backupId
    [void](New-Item -ItemType Directory -Path $backupPath)
    $manifest = @($files | ForEach-Object {
        [pscustomobject]@{
            path = $_.FullName.Substring($projectPath.Length + 1).Replace('\', '/')
            bytes = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
    $manifestDocument = [ordered]@{ schemaVersion = 1; backupId = $backupId; createdAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); projectPath = $projectPath; editorVersion = '6000.3.23f1'; preflight = $preflight; roots = $roots; rootFiles = $rootFiles; files = $manifest }
    New-TextFile (Join-Path $backupPath 'manifest.json') ($manifestDocument | ConvertTo-Json -Depth 8)
    # Windows PowerShell 5.1 does not load ZipArchiveMode transitively.
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath = Join-Path $backupPath 'CrazyFrog-source.zip'
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($item in $manifest) {
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $projectPath $item.path), ('Project/' + $item.path), [IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $zip.Dispose() }

    # Real extraction into a NEW external directory, never over the working project.
    $restorePath = Join-Path $backupPath 'restore-verification'
    [IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $restorePath)
    $restoredFiles = @(Get-ChildItem -LiteralPath (Join-Path $restorePath 'Project') -Recurse -File -Force)
    if ($restoredFiles.Count -ne $manifest.Count) { throw 'Restored file count differs from the manifest.' }
    foreach ($item in $manifest) {
        $restored = Join-Path (Join-Path $restorePath 'Project') $item.path
        if ((Get-Item -LiteralPath $restored).Length -ne $item.bytes -or (Get-FileHash -LiteralPath $restored -Algorithm SHA256).Hash -ne $item.sha256) { throw "Restored file mismatch: $($item.path)" }
        if ((Get-FileHash -LiteralPath (Join-Path $projectPath $item.path) -Algorithm SHA256).Hash -ne $item.sha256) { throw "Source changed during backup: $($item.path)" }
    }
    $afterPaths = @(Source-Files | ForEach-Object { $_.FullName.Substring($projectPath.Length + 1).Replace('\', '/') })
    if (@(Compare-Object @($manifest.path) $afterPaths).Count -ne 0) { throw 'Source file set changed during backup.' }
    $verification = [ordered]@{
        status = 'VERIFIED'; verifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); backupId = $backupId
        sourceFiles = $manifest.Count; sourceBytes = $sourceBytes; archivePath = $zipPath
        archiveBytes = (Get-Item -LiteralPath $zipPath).Length; archiveSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
        restoredProjectPath = (Join-Path $restorePath 'Project'); restoredFiles = $restoredFiles.Count
        everyRestoredFileMatchesSha256 = $true; sourceStableDuringBackup = $true
        editorLaunched = $false; unityImportOrGameplayVerified = $false
    }
    New-TextFile (Join-Path $backupPath 'verification.json') ($verification | ConvertTo-Json -Depth 8)
    New-TextFile (Join-Path $backupPath 'RESTORE.md') @'
# Crazy Frog source recovery

This backup contains a Project/ tree with Assets, Packages, ProjectSettings,
Documentation, Tools, original Backups, historical ValidationResults, and shared
root configuration. It intentionally omits generated Library/Temp/Logs/Builds,
IDE project files, local MCP credentials/config, and the collector lock file.

1. Close the affected Unity project and stop automation before any restoration.
2. Check archive SHA-256 against verification.json. Never overwrite an existing
   working project as a first recovery attempt.
3. Extract CrazyFrog-source.zip into a new empty directory outside the project.
4. Compare every Project/<path> file with manifest.json (length and SHA-256).
   restore-verification/Project is the extraction already checked at creation.
5. Add that Project directory to Unity Hub and use exactly Unity 6000.3.23f1.
   Unity will regenerate Library/Temp and resolve Packages; network and a valid
   local Unity setup may be required. No Editor was launched for this verification.
6. P1 scripts currently target the original desktop path. Do not run them against
   a restored copy expecting it to be the target. Per-device MCP setup is not in Git.
7. WSL wrapper source is Tools/Validation/cfcheck.sh. Back up any home wrapper
   before reinstalling it. The Linux home directory itself is not in this archive.

Keep manifest.json, verification.json, and this document alongside the archive.
The backup is on the same physical disk as the original; copy it to another disk
for protection against disk loss. Existing backups have not been removed.
'@
    $verification | ConvertTo-Json -Depth 8
} catch {
    if ($backupPath -and (Test-Path -LiteralPath $backupPath -PathType Container)) {
        New-TextFile (Join-Path $backupPath 'FAILED.json') ([ordered]@{status='FAILED';error=$_.Exception.Message;failedAtUtc=[DateTimeOffset]::UtcNow.ToString('o')} | ConvertTo-Json)
    }
    throw
} finally {
    if ($null -ne $lockHandle) { $lockHandle.Dispose() }
}
