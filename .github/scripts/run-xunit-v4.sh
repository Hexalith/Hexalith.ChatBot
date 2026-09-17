#!/usr/bin/env bash

set -euo pipefail

if (( $# < 2 )); then
    printf 'Usage: %s <xunit-v4-runner> <ctrf-evidence-path> [runner arguments...]\n' "$0" >&2
    exit 2
fi

readonly runner="$1"
readonly evidence="$2"
shift 2

if [[ ! -x "$runner" ]]; then
    printf 'xUnit v4 runner %s is not executable. Build the test project in Release first.\n' "$runner" >&2
    exit 1
fi

mkdir -p "$(dirname "$evidence")"
"$runner" \
    -automated sync \
    -noAutoReporters \
    -noColor \
    -reporter quiet \
    -result-ctrf "$evidence" \
    "$@"

python3 - "$evidence" "${XUNIT_REQUIRE_ZERO_SKIPS:-0}" <<'PY'
import json
import pathlib
import sys

evidence_path = pathlib.Path(sys.argv[1])
require_zero_skips = sys.argv[2] == "1"
if not evidence_path.is_file():
    raise SystemExit(f"xUnit runner did not create CTRF evidence: {evidence_path}")

with evidence_path.open(encoding="utf-8") as stream:
    report = json.load(stream)

results = report.get("results")
if not isinstance(results, dict):
    raise SystemExit("CTRF evidence has no results object")
tool = results.get("tool")
if not isinstance(tool, dict) or tool.get("name") != "xUnit.net v3":
    raise SystemExit(f"CTRF evidence was not produced by xUnit.net v3: {tool!r}")
version = tool.get("version")
if not isinstance(version, str) or not version.startswith("4.0.0"):
    raise SystemExit(f"Expected the xUnit 4.0.0 runner, observed {version!r}")

summary = results.get("summary")
if not isinstance(summary, dict):
    raise SystemExit("CTRF evidence has no results.summary object")
passed = summary.get("passed")
failed = summary.get("failed")
skipped = summary.get("skipped")
if not all(isinstance(value, int) and value >= 0 for value in (passed, failed, skipped)):
    raise SystemExit(f"CTRF summary contains invalid counts: {summary!r}")
if passed + failed == 0:
    raise SystemExit(f"xUnit lane executed zero tests: {summary!r}")
if failed != 0:
    raise SystemExit(f"xUnit lane reported failed tests: {summary!r}")
if require_zero_skips and skipped != 0:
    raise SystemExit(f"Required xUnit lane reported skipped tests: {summary!r}")

print(json.dumps({"evidence": str(evidence_path), "runner": version, "summary": summary}, sort_keys=True))
PY

sha256sum "$evidence" > "${evidence}.sha256"
