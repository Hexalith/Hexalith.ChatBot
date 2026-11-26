#!/usr/bin/env bash

set -euo pipefail

job_start_epoch=$(cat "$JOB_START_PATH")
now_epoch=$(date +%s)
if [[ ! "$job_start_epoch" =~ ^[0-9]{1,10}$ || ! "$now_epoch" =~ ^[0-9]{1,10}$ ]]; then
    printf 'Invalid completion job clock.\n' >&2
    exit 1
fi
elapsed_seconds=$((10#$now_epoch - 10#$job_start_epoch))
if (( elapsed_seconds < 0 )); then
    printf 'Completion job clock moved before its origin.\n' >&2
    exit 1
fi
if (( elapsed_seconds >= 2400 )); then
    # A fixed schema containing only bounded clock metadata; this is a workflow refusal, not a TE-2 reason.
    mkdir -p "$(dirname "$RESERVE_REPORT_PATH")"
    printf '{"schemaVersion":"1.0","passed":false,"code":"completion_reserve_exhausted","elapsedSeconds":%s,"thresholdSeconds":2400}\n' \
        "$elapsed_seconds" > "$RESERVE_REPORT_PATH"
    printf 'Recovery producer refused after %s seconds; the 30-minute cleanup/publication reserve would be unsafe.\n' \
        "$elapsed_seconds" >&2
    exit 1
fi
printf '%s\n' "$((10#$job_start_epoch + (330 * 60)))" > "$CLOSEOUT_DEADLINE_PATH"
