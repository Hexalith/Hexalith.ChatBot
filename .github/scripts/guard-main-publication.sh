#!/usr/bin/env bash

set -euo pipefail

if (( $# != 2 )); then
    printf 'Usage: %s <release-branch> <validated-sha>\n' "$0" >&2
    exit 64
fi

readonly release_branch="$1"
readonly validated_revision="$2"
readonly github_output="${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"

decision_written=0

# GitHub does not document how a repeated key in the step output file is resolved, so the decision is written
# exactly once: every classified path writes it, and any unclassified exit is completed by the trap below with
# the fail-closed classification. The consumer therefore never sees two `should_publish` values.
write_decision() {
    local decision="$1"
    local should_publish="$2"
    decision_written=1
    printf 'publication_decision=%s\nshould_publish=%s\n' "$decision" "$should_publish" >> "$github_output"
}

# A failing step still leaves an explicit non-publication classification for diagnostics.
trap 'if (( decision_written == 0 )); then write_decision "blocked" "false"; fi' EXIT

case "$release_branch" in
    next|alpha|beta)
        write_decision 'prerelease' 'true'
        printf 'Prerelease branch %s preserves semantic-release publication eligibility.\n' "$release_branch"
        exit 0
        ;;
    main)
        ;;
    *)
        printf 'Unsupported release branch %s; publication is blocked.\n' "$release_branch" >&2
        exit 1
        ;;
esac

if [[ ! "$validated_revision" =~ ^[0-9A-Fa-f]{40}$ ]]; then
    printf 'Validated SHA must be a full 40-character commit SHA.\n' >&2
    exit 1
fi

if ! validated_sha="$(git rev-parse --verify "${validated_revision}^{commit}")"; then
    printf 'Validated SHA %s is not an available commit.\n' "$validated_revision" >&2
    exit 1
fi
readonly validated_sha

if ! checked_out_sha="$(git rev-parse --verify 'HEAD^{commit}')"; then
    printf 'Unable to resolve the checked-out HEAD commit; publication is blocked.\n' >&2
    exit 1
fi
readonly checked_out_sha

if [[ "$checked_out_sha" != "$validated_sha" ]]; then
    printf 'Checked-out HEAD %s does not match validated SHA %s.\n' \
        "$checked_out_sha" "$validated_sha" >&2
    exit 1
fi

if ! git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'; then
    printf 'Unable to fetch the current remote main; publication is blocked.\n' >&2
    exit 1
fi

if ! current_main="$(git rev-parse --verify 'refs/remotes/origin/main^{commit}')"; then
    printf 'Fetched remote main does not resolve to a commit; publication is blocked.\n' >&2
    exit 1
fi
readonly current_main

if [[ "$validated_sha" == "$current_main" ]]; then
    write_decision 'current' 'true'
    printf 'Validated SHA %s is the freshly fetched remote main head.\n' "$validated_sha"
    exit 0
fi

set +e
git merge-base --is-ancestor "$validated_sha" "$current_main"
readonly ancestry_status=$?
set -e

if (( ancestry_status == 0 )); then
    write_decision 'superseded' 'false'
    printf 'Validated SHA %s is superseded by remote main %s; semantic-release will be skipped.\n' \
        "$validated_sha" "$current_main"
    exit 0
fi

if (( ancestry_status == 1 )); then
    printf 'Validated SHA %s diverges from remote main %s; publication is blocked.\n' \
        "$validated_sha" "$current_main" >&2
    exit 1
fi

printf 'Unable to determine ancestry between validated SHA %s and remote main %s; publication is blocked.\n' \
    "$validated_sha" "$current_main" >&2
exit 1
