<#
.SYNOPSIS  Publish a self-contained Windows x64 build into artifacts\AetherDesktop-win-x64.
.DESCRIPTION
  The result is a folder (not a single file) so that e_sqlite3.dll and the WPF native libraries stay
  visible and easy to check. Zip the folder to hand it out. No .NET install or database server is needed
  on the target computer. Add -SingleFile to try the single-file variant (see docs/DECISIONS.md D-14).
.EXAMPLE   .\scripts\publish.ps1
#>
param([switch]$SingleFile)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$out = 'artifacts\AetherDesktop-win-x64'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$args = @('publish', 'src\Aether.Desktop\Aether.Desktop.csproj', '-c', 'Release', '-r', 'win-x64',
          '--self-contained', 'true', '-o', $out)
if ($SingleFile) {
    $args += @('-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true')
}
dotnet @args
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
Write-Host "Published to $out" -ForegroundColor Green
Write-Host "Run $out\AetherDesktop.exe on a clean Windows 10/11 x64 machine to verify the release."
