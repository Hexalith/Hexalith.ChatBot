#!/usr/bin/env bash

set -euo pipefail

if (( $# < 1 )); then
    printf 'Usage: %s <project> [project...]\n' "$0" >&2
    exit 64
fi

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd -- "$repository_root"
export HexalithBuildsRoot="${HexalithBuildsRoot:-${repository_root}/references/Hexalith.Builds}"
export HexalithEventStoreRoot="${HexalithEventStoreRoot:-${repository_root}/references/Hexalith.EventStore}"
export HexalithEventStoreFromSource=false
# Platform's CI guard requires the published Builds tooling instead of its source project reference.
if [[ -z "${HexalithBuildsPublishedToolRoot:-}" ]]; then
    HexalithBuildsPublishedToolRoot="$(bash "${repository_root}/.github/scripts/resolve-published-builds-tooling.sh")"
fi
export HexalithBuildsPublishedToolRoot

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
