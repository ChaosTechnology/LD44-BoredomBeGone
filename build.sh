#!/bin/bash

# Build LD44-BoredomBeGone and restore/build all required dependencies.

set -euo pipefail

SolutionDir="$(realpath "$(dirname "${BASH_SOURCE[0]}")")/"
cd "$SolutionDir"

dotnet build "./ChaosBuild.sln"
dotnet build "./LD44.sln"
