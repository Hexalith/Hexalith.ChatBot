#!/usr/bin/env bash
set -euo pipefail

# The legacy recovery schedule ran on Sunday UTC. Central nightly invokes this
# hook daily; keep the destructive product sweep weekly without filtering tests.
weekday="$(date -u +%u)"
if [[ ! "$weekday" =~ ^[1-7]$ ]]; then
    printf 'Unable to determine the UTC recovery weekday.\n' >&2
    exit 1
fi
if [[ "$weekday" != "7" ]]; then
    printf 'Weekly ChatBot recovery is due on Sunday UTC.\n'
    exit 0
fi

deadline="${HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES-250}"
# Ten 20-minute scenarios plus 30 minutes for setup and cleanup are required.
if [[ ! "$deadline" =~ ^[1-9][0-9]{2}$ ]] || (( deadline < 230 || deadline > 250 )); then
    printf 'HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES must be an integer from 230 through 250 for nightly recovery.\n' >&2
    exit 1
fi
export HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES="$deadline"

: "${RUNNER_TEMP:?The prepared nightly runner must provide RUNNER_TEMP}"
runner_temp="$(cd -- "$RUNNER_TEMP" && pwd -P)"
producer_root="$(mktemp -d "$runner_temp/chatbot-recovery.XXXXXXXX")"
trap 'if (( $? == 0 )); then rm -rf -- "$producer_root"; fi' EXIT
producer_tmp="$producer_root/tmp"
mkdir -m 700 -- "$producer_tmp"
mkdir -p "$PWD/TestResults"
artifact_root="$(mktemp -d "$PWD/TestResults/nightly-recovery.XXXXXXXX")"
if [[ -n "${GITHUB_ENV:-}" ]]; then
    printf 'CHATBOT_RECOVERY_BUNDLE_DIR=%s\n' "$artifact_root" >> "$GITHUB_ENV"
fi
export HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT=live-recovery-validation-evidence
export HEXALITH_CHATBOT_RECOVERY_PRODUCER_ROOT="$producer_root/live-recovery"
export HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR="$producer_root/retention-failures"
required_commit="$(git rev-parse --verify HEAD)"
# Recovery's finally blocks run at the 250-minute in-process deadline, before
# SIGINT at 280 minutes and the 300-minute step limit. The registered job needs
# the separately released 330-minute allowance; do not enable against 45 minutes.
producer_status=0
TMPDIR="$producer_tmp" TMP="$producer_tmp" TEMP="$producer_tmp" \
    timeout --signal=TERM --kill-after=1m 300m \
    timeout --signal=INT --kill-after=15m 16800s \
    bash .github/scripts/run-xunit-v4.sh \
    tests/Hexalith.ChatBot.Recovery.Tests/bin/Release/net11.0/Hexalith.ChatBot.Recovery.Tests \
    "$producer_root/results.raw.ctrf.json" -result-trx "$producer_root/results.raw.trx" \
    >"$producer_root/producer.log" 2>&1 || producer_status=$?

# The producer can be killed before LiveCatalogFixture.DisposeAsync runs. Its Redis
# container is labeled with the .NET temporary path, which is unique to this producer.
cleanup_producer_redis() {
    local containers container
    local scope="label=io.hexalith.chatbot-live.scope=$producer_tmp"
    local role="label=io.hexalith.chatbot-live.role=fixture-redis"
    containers="$(timeout --kill-after=5s 15s docker container ls --all --quiet --no-trunc --filter "$scope" --filter "$role")" || return
    if [[ -z "$containers" ]]; then
        return 0
    fi
    local -a owned_ids=()
    while IFS= read -r container; do
        if [[ ! "$container" =~ ^[0-9a-f]{64}$ ]]; then
            printf 'Docker returned an invalid ChatBot live-catalog container ID.\n' >&2
            return 1
        fi
        owned_ids+=("$container")
    done <<< "$containers"
    timeout --kill-after=5s 15s docker container rm --force -- "${owned_ids[@]}" || return
    containers="$(timeout --kill-after=5s 15s docker container ls --all --quiet --no-trunc --filter "$scope" --filter "$role")" || return
    [[ -z "$containers" ]]
}

cleanup_status=0
cleanup_producer_redis >"$producer_root/cleanup.log" 2>&1 || cleanup_status=$?

# Export the producer's evidence even when the sweep fails. An early failure can leave
# either input directory absent, which must reach the gate as missing evidence.
export_status=0
for name in live-recovery retention-failures; do
    if [[ -d "$producer_root/$name" ]]; then
        cp -R -- "$producer_root/$name" "$artifact_root/" || export_status=$?
    fi
done
if [[ -s "$producer_root/results.raw.trx" ]]; then
    # Keep runner output in the private staging area. Publish only the generated test identity and counters.
    python3 - "$producer_root/results.raw.trx" "$artifact_root/results.trx" <<'PYTHON' || export_status=$?
import sys
from pathlib import Path
from xml.etree import ElementTree as et

source, destination = map(Path, sys.argv[1:])
namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
bound_test = (
    "Hexalith.ChatBot.Recovery.Tests.LiveContinuityAspireE2eTests."
    "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate"
)
et.register_namespace("", namespace)
def tag(name):
    return f"{{{namespace}}}{name}"

if source.stat().st_size > 64 * 1024 * 1024:
    raise SystemExit("Recovery test output exceeded the retention size bound.")
raw = et.parse(source).getroot()
results = raw.findall(f"./{tag('Results')}/{tag('UnitTestResult')}")
summary = raw.find(tag("ResultSummary"))
counters = None if summary is None else summary.find(tag("Counters"))
if (raw.tag != tag("TestRun") or len(results) != 1 or results[0].get("testName") != bound_test
        or summary is None or counters is None):
    raise SystemExit("Recovery test output did not identify the single expected test.")
outcome = results[0].get("outcome")
if outcome not in {"Passed", "Failed", "NotExecuted", "Aborted", "Timeout"}:
    raise SystemExit("Recovery test output used an unknown outcome.")
projected = et.Element(tag("TestRun"), {"name": "ChatBot live recovery"})
et.SubElement(et.SubElement(projected, tag("Results")), tag("UnitTestResult"),
              {"testName": bound_test, "outcome": outcome})
retained_summary = et.SubElement(projected, tag("ResultSummary"), {"outcome": outcome})
names = ("total", "executed", "passed", "failed", "notExecuted")
values = {key: counters.get(key, "0") for key in names}
if any(value not in {"0", "1"} for value in values.values()) or values["total"] != "1":
    raise SystemExit("Recovery test output did not contain one test's counters.")
et.SubElement(retained_summary, tag("Counters"), values)
et.ElementTree(projected).write(destination, encoding="utf-8", xml_declaration=True)
PYTHON
elif (( producer_status == 0 )); then
    export_status=1
fi
printf 'Retained ChatBot recovery evidence at %s\n' "$artifact_root"

# The gate reads the exported copy in a new process with the release policy pinned
# here instead of inheriting policy from the producing run.
export HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED=1
export HEXALITH_CHATBOT_RECOVERY_EVIDENCE_DIR="$artifact_root"
export HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASETS=recovery-baseline
export HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS=192
export HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASET_VERSION=v1
export HEXALITH_CHATBOT_RECOVERY_MINIMUM_DATASET_VOLUME=6
export HEXALITH_CHATBOT_RECOVERY_MAX_MEASURABLE_CEILING_SECONDS=180
export HEXALITH_CHATBOT_RECOVERY_REQUIRED_COMMIT="$required_commit"
gate_status=0
XUNIT_REQUIRE_ZERO_SKIPS=1 timeout --signal=TERM --kill-after=10s 3m \
    bash .github/scripts/run-xunit-v4.sh \
    tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net11.0/Hexalith.ChatBot.IntegrationTests \
    "$producer_root/gate.raw.ctrf.json" \
    -method Hexalith.ChatBot.IntegrationTests.Recovery.LiveRecoveryEvidenceGateReplayTests.RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess \
    >"$producer_root/gate.log" 2>&1 || gate_status=$?
printf '{"producerExit":%s,"cleanupExit":%s,"exportExit":%s,"gateExit":%s}\n' \
    "$producer_status" "$cleanup_status" "$export_status" "$gate_status" > "$artifact_root/decision.json"
if (( producer_status || cleanup_status || export_status || gate_status )); then
    printf 'ChatBot recovery validation failed. Producer=%s Cleanup=%s Export=%s Gate=%s\n' \
        "$producer_status" "$cleanup_status" "$export_status" "$gate_status" >&2
    exit 1
fi
