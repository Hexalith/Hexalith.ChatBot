#!/usr/bin/env bash

set -euo pipefail

if (( $# < 2 )); then
    printf 'Usage: %s <xunit-v4-runner> <ctrf-evidence-path> [runner arguments...]\n' "$0" >&2
    exit 2
fi

readonly runner="$1"
readonly evidence="$2"
shift 2
readonly repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"

if [[ ! -x "$runner" ]]; then
    printf 'xUnit v4 runner %s is not executable. Build the test project in Release first.\n' "$runner" >&2
    exit 1
fi

mkdir -p "$(dirname "$evidence")"
rm -f -- "$evidence" "${evidence}.sha256"
"$runner" \
    -automated sync \
    -noAutoReporters \
    -noColor \
    -reporter quiet \
    -result-ctrf "$evidence" \
    "$@"

python3 - "$evidence" "${XUNIT_REQUIRE_ZERO_SKIPS:-0}" "$repository_root" <<'PY'
import json
import pathlib
import subprocess
import sys

evidence_path = pathlib.Path(sys.argv[1])
require_zero_skips = sys.argv[2] == "1"
repository_root = pathlib.Path(sys.argv[3])
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
catalog = subprocess.run(
    ["dotnet", "msbuild", str(repository_root / "Directory.Packages.props"), "-getItem:PackageVersion"],
    cwd=repository_root,
    capture_output=True,
    text=True,
    timeout=120,
    check=True,
)
versions = json.loads(catalog.stdout)["Items"]["PackageVersion"]
xunit_versions = [item for item in versions if item["Identity"].lower() == "xunit.v3"]
authority = repository_root / "references" / "Hexalith.Builds" / "Props" / "Directory.Packages.props"
if len(xunit_versions) != 1 or pathlib.Path(xunit_versions[0]["DefiningProjectFullPath"]).resolve() != authority.resolve():
    raise SystemExit("xUnit runner version must have exactly one shared Builds catalog authority")
expected_version = xunit_versions[0]["Version"]
if not isinstance(expected_version, str) or not expected_version.startswith("4."):
    raise SystemExit(f"Expected a catalog-pinned xUnit 4 runner, observed {expected_version!r}")
if not isinstance(version, str) or not (version == expected_version or version.startswith(expected_version + "+")):
    raise SystemExit(f"Expected the catalog-pinned xUnit {expected_version} runner, observed {version!r}")

summary = results.get("summary")
if not isinstance(summary, dict):
    raise SystemExit("CTRF evidence has no results.summary object")
passed = summary.get("passed")
failed = summary.get("failed")
skipped = summary.get("skipped")
tests = summary.get("tests")
if not all(type(value) is int and value >= 0 for value in (tests, passed, failed, skipped)):
    raise SystemExit(f"CTRF summary contains invalid counts: {summary!r}")
if tests != passed + failed + skipped:
    raise SystemExit(f"CTRF summary test count does not match its outcomes: {summary!r}")
if passed + failed == 0:
    raise SystemExit(f"xUnit lane executed zero tests: {summary!r}")
if failed != 0:
    raise SystemExit(f"xUnit lane reported failed tests: {summary!r}")
if require_zero_skips and skipped != 0:
    raise SystemExit(f"Required xUnit lane reported skipped tests: {summary!r}")

print(json.dumps({"evidence": str(evidence_path), "runner": version, "summary": summary}, sort_keys=True))
PY

sha256sum "$evidence" > "${evidence}.sha256"
