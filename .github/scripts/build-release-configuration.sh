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
export UseHexalithProjectReferences=false
# Platform's CI guard requires the published Builds tooling instead of its source project reference.
if [[ -z "${HexalithBuildsPublishedToolRoot:-}" ]]; then
    HexalithBuildsPublishedToolRoot="$(bash "${repository_root}/.github/scripts/resolve-published-builds-tooling.sh")"
fi
export HexalithBuildsPublishedToolRoot

# The story-evidence gate run step invokes the native xUnit v4 host from
# tests/.../bin/Release/net11.0. Building only the gate tool or IntegrationTests
# leaves that host missing, so include the test project whenever it exists.
story_evidence_tests="tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj"
projects=("$@")
already_listed=0
for project in "${projects[@]}"; do
    if [[ "$project" == "$story_evidence_tests" ]]; then
        already_listed=1
        break
    fi
done
if (( already_listed == 0 )) && [[ -f "$story_evidence_tests" ]]; then
    projects+=("$story_evidence_tests")
fi

for project in "${projects[@]}"; do
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
