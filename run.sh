#!/bin/bash

# Build and run LD44-BoredomBeGone and restore/build all required dependencies.

set -euo pipefail

SolutionDir="$(realpath "$(dirname "${BASH_SOURCE[0]}")")/"
cd "$SolutionDir"

bash ./build.sh
SolutionDir="$SolutionDir" bash -c 'dotnet run "./LD44.sln" --project "./LD44/" --framework net8.0-windows --configuration Debug'
