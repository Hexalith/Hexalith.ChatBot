#!/usr/bin/env bash

set -euo pipefail

readonly test_root="${MERGE_TEST_ROOT:-tests}"
readonly expected_lanes="${MERGE_TEST_EXPECTED_LANES:-13}"
readonly results_root="${MERGE_TEST_RESULTS_ROOT:-TestResults/merge-test-lanes}"
readonly runner_override="${MERGE_TEST_RUNNER_OVERRIDE:-}"
readonly script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly runner_boundary="${script_directory}/run-xunit-v4.sh"

if [[ ! -d "$test_root" ]]; then
    printf 'Merge test root %s does not exist.\n' "$test_root" >&2
    exit 1
fi

if [[ ! "$expected_lanes" =~ ^[1-9][0-9]*$ ]]; then
    printf 'MERGE_TEST_EXPECTED_LANES must be a positive integer.\n' >&2
    exit 1
fi

if [[ ! -x "$runner_boundary" ]]; then
    printf 'xUnit v4 runner boundary %s is unavailable.\n' "$runner_boundary" >&2
    exit 1
fi

# Assigned before being made read-only for the reason documented in verify-pull-request-merge.sh: a
# `readonly name="$(command)"` declaration swallows the substitution's exit status, so a failed mktemp
# would continue with an empty path and misreport itself as a project-discovery failure.
if ! discovery_file="$(mktemp)"; then
    printf 'Unable to allocate a merge test discovery file.\n' >&2
    exit 1
fi
readonly discovery_file
trap 'rm -f "$discovery_file"' EXIT
if ! find "$test_root" -type f -name '*.csproj' -print0 | sort -z > "$discovery_file"; then
    printf 'Unable to discover merge test projects under %s.\n' "$test_root" >&2
    exit 1
fi

mapfile -d '' -t discovered_projects < "$discovery_file"
declare -a projects=()
declare -A seen_lanes=()

for project in "${discovered_projects[@]}"; do
    case "$project" in
        *'/Hexalith.ChatBot.RecoverySandbox.csproj'|*'/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj')
            continue
            ;;
    esac

    lane="$(basename "${project%.csproj}")"
    if [[ -n "${seen_lanes[$lane]+present}" ]]; then
        printf 'Colliding merge test lane %s: %s and %s.\n' \
            "$lane" "${seen_lanes[$lane]}" "$project" >&2
        exit 1
    fi

    seen_lanes[$lane]="$project"
    projects+=("$project")
done

if (( ${#projects[@]} == 0 )); then
    printf 'No ordinary merge test lanes were discovered under %s.\n' "$test_root" >&2
    exit 1
fi

if (( ${#projects[@]} != expected_lanes )); then
    printf 'Expected %s ordinary merge test lanes, discovered %s; the merge lane set must match the build job exactly.\n' \
        "$expected_lanes" "${#projects[@]}" >&2
    exit 1
fi

mkdir -p "$results_root"
for project in "${projects[@]}"; do
    lane="$(basename "${project%.csproj}")"
    runner="${project%/*}/bin/Release/net10.0/${lane}"
    if [[ -n "$runner_override" ]]; then
        runner="$runner_override"
    fi

    XUNIT_TEST_LANE="$lane" bash "$runner_boundary" \
        "$runner" \
        "${results_root}/${lane}.ctrf.json" \
        > "${results_root}/${lane}.runner.jsonl"
done

printf 'Executed %s ordinary merge test lanes successfully.\n' "${#projects[@]}"
