#!/usr/bin/env bash

set -euo pipefail

if (( $# < 1 )); then
    printf 'Usage: %s <project> [project...]\n' "$0" >&2
    exit 64
fi

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd -- "$repository_root"

for project in "$@"; do
    if [[ ! -f "$project" ]]; then
        printf 'Project %s does not exist.\n' "$project" >&2
        exit 1
    fi
    dotnet restore "$project" \
        -p:Configuration=Release \
        -p:UseHexalithProjectReferences=false \
        -m:1 \
        /nr:false
    dotnet build "$project" \
        --no-restore \
        --configuration Release \
        -p:UseHexalithProjectReferences=false \
        -m:1 \
        /nr:false
done
