#!/usr/bin/env bash

set -euo pipefail

readonly test_root="${MERGE_TEST_ROOT:-tests}"
readonly dotnet_command="${MERGE_TEST_DOTNET:-dotnet}"
readonly expected_lanes="${MERGE_TEST_EXPECTED_LANES:-13}"

if [[ ! -d "$test_root" ]]; then
    printf 'Merge test root %s does not exist.\n' "$test_root" >&2
    exit 1
fi

if [[ ! "$expected_lanes" =~ ^[1-9][0-9]*$ ]]; then
    printf 'MERGE_TEST_EXPECTED_LANES must be a positive integer.\n' >&2
    exit 1
fi

if ! command -v "$dotnet_command" >/dev/null 2>&1; then
    printf 'Merge test command %s is unavailable.\n' "$dotnet_command" >&2
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

# The ordinary lanes run on the VSTest bridge (Microsoft.NET.Test.Sdk + xunit.runner.visualstudio), not on
# Microsoft.Testing.Platform, so a Microsoft.Testing.Platform switch such as --minimum-expected-tests is parsed
# as a run-settings argument, silently ignored, and a lane that discovers zero tests still exits 0. The
# RunConfiguration.TreatNoTestsAsError run-settings override is the form this toolchain honors; it is the same
# setting live-recovery.runsettings applies to the required recovery lanes.
for project in "${projects[@]}"; do
    "$dotnet_command" test "$project" -m:1 --no-build --configuration Release \
        -- RunConfiguration.TreatNoTestsAsError=true
done

printf 'Executed %s ordinary merge test lanes successfully.\n' "${#projects[@]}"
