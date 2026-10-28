#!/usr/bin/env bash

set -euo pipefail

if (( $# != 3 )); then
    printf 'Usage: %s <raw-trx> <results-root> <producer-outcome>\n' "$0" >&2
    exit 2
fi

readonly raw_trx="$1"
readonly results_root="$2"
readonly producer_outcome="$3"
readonly repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly gate_dll="${STORY_EVIDENCE_GATE_DLL:-${repository_root}/tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Release/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll}"

readonly summary_path="$results_root/workflow-attempt/recovery-attempt-summary.json"
readonly sanitized_path="$results_root/live-recovery-validation.trx"

# These two paths belong to this projection; an earlier attempt cannot stand in for this one.
rm -f -- "$summary_path" "$sanitized_path"

# Failure, missing input and malformed input still retain payload-free attempt metadata.
dotnet "$gate_dll" summarize-recovery-attempt \
    --repository-root "$repository_root" \
    --input "$raw_trx" --outcome "$producer_outcome" \
    --output "$summary_path"

# The summarizer is best effort for existing completion callers; this workflow requires retention.
if [[ ! -s "$summary_path" ]]; then
    printf 'Recovery attempt summary was not retained.\n' >&2
    exit 1
fi

# Raw TRX and CTRF remain outside artifact upload paths for every producer outcome.
if [[ "$producer_outcome" == success ]]; then
    dotnet "$gate_dll" sanitize-recovery-trx \
        --repository-root "$repository_root" \
        --input "$raw_trx" --output "$sanitized_path"
fi
