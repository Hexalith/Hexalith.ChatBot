#!/usr/bin/env bash

set -euo pipefail

if [[ "$EVENT_NAME" == "pull_request" ]]; then
    base_sha="$PR_BASE"
    head_sha="$PR_HEAD"
elif [[ "$EVENT_NAME" == "push" ]]; then
    head_sha="$PUSH_HEAD"
    if [[ -z "${PUSH_BASE:-}" || "$PUSH_BASE" =~ ^0+$ ]]; then
        base_sha="$(git rev-parse HEAD^)"
    elif git cat-file -e "${PUSH_BASE}^{commit}"; then
        base_sha="$PUSH_BASE"
    else
        printf 'Non-zero push base %s is unavailable; refusing a one-commit fallback.\n' "$PUSH_BASE" >&2
        exit 1
    fi
elif [[ "$EVENT_NAME" == "schedule" || "$EVENT_NAME" == "workflow_dispatch" ]]; then
    # These events propose no lifecycle transition, so retain an empty exact range.
    head_sha="$PUSH_HEAD"
    base_sha="$head_sha"
else
    printf 'Unsupported story-evidence event.\n' >&2
    exit 1
fi

checked_out_head="$(git rev-parse HEAD)"
if [[ "$checked_out_head" != "$head_sha" ]]; then
    printf 'Checked-out HEAD does not match event head.\n' >&2
    exit 1
fi
for revision in "$base_sha" "$head_sha"; do
    resolved="$(git rev-parse --verify --end-of-options "${revision}^{commit}")"
    if [[ "$resolved" != "$revision" ]]; then
        printf 'Story-evidence bounds require exact full commit revisions.\n' >&2
        exit 1
    fi
done
printf 'base=%s\nhead=%s\n' "$base_sha" "$head_sha" >> "$GITHUB_OUTPUT"
