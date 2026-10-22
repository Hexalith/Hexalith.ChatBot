#!/usr/bin/env bash

set -euo pipefail

declare -A downloaded=()
declare -a run_ids=() artifacts=() destinations=()
# Preflight every locator and destination before granting any download authority.
while IFS= read -r locator || [[ -n "$locator" ]]; do
    [[ -z "$locator" ]] && continue
    if [[ ! "$locator" =~ ^github-actions://([A-Za-z0-9_.-]+)/([A-Za-z0-9_.-]+)/runs/([1-9][0-9]{0,19})/artifacts/([A-Za-z0-9][A-Za-z0-9_.-]{0,127})$ ]]; then
        printf 'Unsafe retained locator.\n' >&2
        exit 1
    fi
    repository="${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"
    run_id="${BASH_REMATCH[3]}"
    artifact="${BASH_REMATCH[4]}"
    if [[ "$repository" != "$GITHUB_REPOSITORY" ]]; then
        printf 'Retained locator repository is not this repository.\n' >&2
        exit 1
    fi
    key="${repository}/${run_id}/${artifact}"
    [[ -n "${downloaded[$key]+present}" ]] && continue
    destination="${RESULTS_ROOT}/retained/${run_id}/${artifact}"
    if [[ -e "$destination" || -L "$destination" ]]; then
        printf 'Retained destination collision.\n' >&2
        exit 1
    fi
    ancestor="$(dirname "$destination")"
    while [[ "$ancestor" != "." && "$ancestor" != "/" ]]; do
        if [[ -L "$ancestor" || ( -e "$ancestor" && ! -d "$ancestor" ) ]]; then
            printf 'Unsafe retained destination ancestry.\n' >&2
            exit 1
        fi
        ancestor="$(dirname "$ancestor")"
    done
    run_ids+=("$run_id")
    artifacts+=("$artifact")
    destinations+=("$destination")
    downloaded[$key]=1
done < "$RETAINED_LOCATORS_PATH"

for index in "${!run_ids[@]}"; do
    run_id="${run_ids[$index]}"
    artifact="${artifacts[$index]}"
    destination="${destinations[$index]}"
    mkdir -p "$(dirname "$destination")"
    mkdir "$destination"
    gh run download "$run_id" --repo "$GITHUB_REPOSITORY" --name "$artifact" --dir "$destination"
done
