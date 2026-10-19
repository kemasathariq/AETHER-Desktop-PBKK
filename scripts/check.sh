#!/usr/bin/env bash
# Restore, build and test on macOS/Linux. The WPF project is compiled (EnableWindowsTargeting) but cannot run here.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore Aether.sln
dotnet build Aether.sln -c Release --no-restore
dotnet test Aether.sln -c Release --no-build
