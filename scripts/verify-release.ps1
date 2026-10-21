<#
.SYNOPSIS Verify the default Windows x64 folder release starts twice using an isolated database.
.EXAMPLE .\scripts\verify-release.ps1
#>
$ErrorActionPreference = 'Stop'
$release = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts\AetherDesktop-win-x64'))
$exe = Join-Path $release 'AetherDesktop.exe'
foreach ($file in 'AetherDesktop.exe', 'coreclr.dll', 'PresentationFramework.dll', 'e_sqlite3.dll') {
    if (-not (Test-Path -LiteralPath (Join-Path $release $file))) { throw "Release is missing $file. Run publish.ps1 first." }
}

$checkDir = Join-Path $env:TEMP ('aether-release-' + [Guid]::NewGuid().ToString('N'))
$dbPath = Join-Path $checkDir 'aether.db'
$previousPath = $env:AETHER_DB_PATH
try {
    $env:AETHER_DB_PATH = $dbPath
    foreach ($attempt in 1..2) {
        $appProcess = Start-Process -FilePath $exe -WorkingDirectory $release -WindowStyle Hidden -PassThru
        try {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            do {
                Start-Sleep -Milliseconds 100
                $appProcess.Refresh()
            } while (-not $appProcess.HasExited -and $appProcess.MainWindowHandle -eq 0 -and $timer.Elapsed.TotalSeconds -lt 20)
            if ($appProcess.HasExited) { throw "Release exited during startup (exit $($appProcess.ExitCode))." }
            if ($appProcess.MainWindowHandle -eq 0) { throw 'Release did not create its main window within 20 seconds.' }
            if ($appProcess.MainWindowTitle -ne 'AETHER Desktop') { throw "Unexpected window: $($appProcess.MainWindowTitle)" }
            if (-not (Test-Path -LiteralPath $dbPath) -or (Get-Item -LiteralPath $dbPath).Length -eq 0) {
                throw 'Release did not create its SQLite database.'
            }
            $runtime = $appProcess.Modules | Where-Object { $_.ModuleName -eq 'coreclr.dll' }
            if (-not $runtime -or -not $runtime.FileName.StartsWith($release + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Release did not load its bundled .NET runtime.'
            }
            Write-Host "Startup $attempt passed: main window, SQLite file, bundled runtime." -ForegroundColor Green
            if (-not $appProcess.CloseMainWindow() -or -not $appProcess.WaitForExit(10000)) {
                throw 'Release did not close normally.'
            }
            if ($appProcess.ExitCode -ne 0) { throw "Release exited with code $($appProcess.ExitCode)." }
        }
        finally {
            if (-not $appProcess.HasExited) { $appProcess.Kill(); $appProcess.WaitForExit() }
            $appProcess.Dispose()
        }
    }
}
finally {
    $env:AETHER_DB_PATH = $previousPath
}
Write-Host "Release check passed. Test database retained at $dbPath"
