#!/usr/bin/env bash

set -euo pipefail

if (( $# != 3 )); then
    printf 'Usage: %s <merge-sha> <base-sha> <head-sha>\n' "$0" >&2
    exit 64
fi

readonly advertised_merge="$1"
readonly advertised_base="$2"
readonly advertised_head="$3"

resolve_commit() {
    local label="$1"
    local revision="$2"

    if [[ ! "$revision" =~ ^[0-9A-Fa-f]{40}$ ]]; then
        printf '%s must be a full 40-character commit SHA.\n' "$label" >&2
        return 1
    fi

    local resolved
    if ! resolved="$(git rev-parse --verify "${revision}^{commit}")"; then
        printf '%s %s is not an available commit.\n' "$label" "$revision" >&2
        return 1
    fi

    printf '%s\n' "$resolved"
}

# A `readonly name="$(command)"` declaration swallows the substitution's exit status, so each resolution is
# assigned first and only then made read-only; otherwise an unresolvable SHA would continue with an empty value.
if ! merge_sha="$(resolve_commit 'Merge SHA' "$advertised_merge")"; then
    exit 1
fi
readonly merge_sha

if ! base_sha="$(resolve_commit 'Base SHA' "$advertised_base")"; then
    exit 1
fi
readonly base_sha

if ! head_sha="$(resolve_commit 'Head SHA' "$advertised_head")"; then
    exit 1
fi
readonly head_sha

if ! checked_out_sha="$(git rev-parse --verify 'HEAD^{commit}')"; then
    printf 'Unable to resolve the checked-out HEAD commit.\n' >&2
    exit 1
fi
readonly checked_out_sha

if [[ "$checked_out_sha" != "$merge_sha" ]]; then
    printf 'Checked-out HEAD %s does not match advertised merge SHA %s.\n' \
        "$checked_out_sha" "$merge_sha" >&2
    exit 1
fi

# Captured before splitting for the same reason the resolutions above are: a command substitution consumed
# directly by `read` discards its exit status, so an unreadable commit would be misreported as "found 0 parents".
if ! parent_list="$(git show --no-patch --format='%P' "$merge_sha")"; then
    printf 'Unable to read the parent commits of merge %s.\n' "$merge_sha" >&2
    exit 1
fi
read -r -a parents <<< "$parent_list"
if (( ${#parents[@]} != 2 )); then
    printf 'Synthetic merge %s must have exactly two parents; found %s.\n' \
        "$merge_sha" "${#parents[@]}" >&2
    exit 1
fi

if [[ "${parents[0]}" != "$base_sha" || "${parents[1]}" != "$head_sha" ]]; then
    printf 'Synthetic merge parent mismatch: expected base %s then head %s; found %s then %s.\n' \
        "$base_sha" "$head_sha" "${parents[0]}" "${parents[1]}" >&2
    exit 1
fi

printf 'Verified synthetic merge %s binds base %s and head %s in order.\n' \
    "$merge_sha" "$base_sha" "$head_sha"
