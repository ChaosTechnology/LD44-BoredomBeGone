#!/bin/bash

# Build LD44-BoredomBeGone and restore/build all required dependencies.

set -euo pipefail

SolutionDir="$(realpath "$(dirname "${BASH_SOURCE[0]}")")/"
cd "$SolutionDir"

SolutionDir="$SolutionDir" bash -c 'dotnet build "./submodules/ChaosBuild.CodeFormatter.Tasks/"'
SolutionDir="$SolutionDir" bash -c 'dotnet build "./submodules/ChaosFrameworkBuild.ArchiveCreator/"'
SolutionDir="$SolutionDir" bash -c 'dotnet build "./LD44.sln"'
