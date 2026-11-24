#!/usr/bin/env bash
set -euo pipefail

results_dir="$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/chatbot-release.XXXXXXXX")"
trap 'rm -rf -- "$results_dir"' EXIT

# Keep the legacy 30-minute release allowance separate from recovery's timers.
# SIGINT gives the topology one minute to restore/close before the hard bound.
timeout --signal=INT --kill-after=1m 29m \
    dotnet test --project tests/Hexalith.ChatBot.Release.Tests/Hexalith.ChatBot.Release.Tests.csproj \
    --configuration Release --no-build --no-restore --minimum-expected-tests 2 \
    --results-directory "$results_dir" --report-xunit-trx --report-xunit-trx-filename release.trx

python3 - "$results_dir/release.trx" <<'PYTHON'
import sys
from xml.etree import ElementTree

namespace = {"trx": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
test_class = "Hexalith.ChatBot.Release.Tests.GovernedStateReleaseGateTests"
required = (
    "CommittedGovernedStateShouldPassM2StopShipSweepsInRealDaprTopology",
    "GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin",
)
results = ElementTree.parse(sys.argv[1]).findall("./trx:Results/trx:UnitTestResult", namespace)
for method in required:
    outcomes = [result.get("outcome") for result in results if result.get("testName") == f"{test_class}.{method}"]
    if outcomes != ["Passed"]:
        raise SystemExit(f"Required ChatBot release check did not pass exactly once: {method}")
PYTHON
