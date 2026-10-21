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

$artifacts = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts'))
$out = Join-Path $artifacts 'AetherDesktop-win-x64'
if (-not $out.StartsWith($artifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Publish output must be inside the artifacts folder.'
}
if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Recurse -Force }

$args = @('publish', 'src\Aether.Desktop\Aether.Desktop.csproj', '-c', 'Release', '-r', 'win-x64',
          '--self-contained', 'true', '-o', $out, '-m:1')
if ($SingleFile) {
    $args += @('-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true')
}
dotnet @args
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
Copy-Item -LiteralPath 'docs\PANDUAN.md' -Destination (Join-Path $out 'PANDUAN.txt')
Compress-Archive -Path (Join-Path $out '*') -DestinationPath "$out.zip" -Force
Write-Host "Published to $out" -ForegroundColor Green
Write-Host "ZIP: $out.zip"
if ($SingleFile) {
    Write-Host 'Single-file builds require a separate manual Windows check; verify-release.ps1 checks folder builds only.'
} else {
    Write-Host 'Run scripts\verify-release.ps1 to check startup and restart with an isolated database.'
}
