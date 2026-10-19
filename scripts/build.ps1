<#
.SYNOPSIS  Restore, build and test AETHER Desktop (Windows PowerShell or PowerShell 7).
.EXAMPLE   .\scripts\build.ps1
#>
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

dotnet restore Aether.sln
if ($LASTEXITCODE -ne 0) { throw 'restore failed' }
dotnet build Aether.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed' }
dotnet test Aether.sln -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'tests failed' }
Write-Host 'Restore, build and tests succeeded.' -ForegroundColor Green
