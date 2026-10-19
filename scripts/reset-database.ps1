<#
.SYNOPSIS  Safely reset the local AETHER database.
.DESCRIPTION
  Moves aether.db (and its -wal/-shm files) to a timestamped backup next to it. The next start creates a
  fresh database and imports the case data again. Your edits stay in the backup file.
  Close the application first.
.EXAMPLE   .\scripts\reset-database.ps1
#>
$ErrorActionPreference = 'Stop'
$dir = if ($env:AETHER_DB_PATH) { Split-Path $env:AETHER_DB_PATH } else { Join-Path $env:LOCALAPPDATA 'AetherDesktop' }
$db  = if ($env:AETHER_DB_PATH) { $env:AETHER_DB_PATH } else { Join-Path $dir 'aether.db' }

if (Get-Process -Name 'AetherDesktop' -ErrorAction SilentlyContinue) {
    throw 'AetherDesktop is running. Close it first.'
}
if (-not (Test-Path $db)) { Write-Host "No database at $db. Nothing to reset."; return }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
foreach ($suffix in '', '-wal', '-shm') {
    $f = "$db$suffix"
    if (Test-Path $f) { Move-Item $f "$f.bak-$stamp" }
}
Write-Host "Backed up to $db.bak-$stamp. A new database is created on the next start." -ForegroundColor Green
