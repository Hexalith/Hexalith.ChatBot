"""Run a local command with the pinned module tool's verified Debug dependency mapping."""

import json
import os
from pathlib import Path
import re
import subprocess
import sys
import uuid


IMPORTS = {
    "CustomBeforeDirectoryBuildProps": "SourceMapping.targets",
    "CustomAfterMicrosoftCommonProps": "SourceMapping.props",
    "CustomBeforeDirectoryBuildTargets": "SourceMapping.before-targets.props",
    "CustomAfterDirectoryBuildTargets": "SourceMapping.late.props",
    "AfterMicrosoftNETSdkTargets": "SourceMapping.final.targets",
}


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: python3 .github/scripts/with-source-mapping.py <command> [arguments...]", file=sys.stderr)
        return 2

    root = Path(__file__).resolve().parents[2]
    manifest = root / ".config/dotnet-tools.json"
    restore = subprocess.run(["dotnet", "tool", "restore", "--tool-manifest", str(manifest)], cwd=root)
    if restore.returncode:
        return restore.returncode
    version = subprocess.run(["dotnet", "tool", "run", "hexalith-module", "--", "--version"],
                             cwd=root, capture_output=True, text=True, check=True).stdout.strip()
    pin = json.loads(manifest.read_text())["tools"]["hexalith.builds.module.cli"]["version"]
    if version.split("+", 1)[0] != pin:
        raise ValueError("The restored module tool differs from the exact local pin")

    # Retain imports for descendant builds, including detached Aspire resources.
    # This ignored directory contains machine-specific paths, never source configuration.
    parent = root / ".artifacts/source-mapping"
    parent.mkdir(parents=True, exist_ok=True)
    workspace = parent / uuid.uuid4().hex
    result = subprocess.run(
        ["dotnet", "tool", "run", "hexalith-module", "--", "prepare-build", "--manifest",
         "module/build.module-manifest.v2.json", "--mode", "source", "--workspace", str(workspace),
         "--output", "json"],
        cwd=root, capture_output=True, text=True,
    )
    if result.returncode:
        sys.stderr.write(result.stdout + result.stderr)
        return result.returncode

    prepared = json.loads(result.stdout)
    build = prepared["buildPreparation"]
    environment = build["environment"]
    digest = build["mappingHash"]
    if (prepared["status"] != "prepared" or prepared["outcome"]["exitCode"] != "Success"
            or build["mode"] != "source" or build["workspace"] != str(workspace)
            or set(environment) != set(IMPORTS) | {"Configuration", "HEXALITH_SOURCE_MAPPING_HASH"}
            or environment["Configuration"] != "Debug" or re.fullmatch(r"[0-9a-f]{64}", digest) is None
            or environment["HEXALITH_SOURCE_MAPPING_HASH"] != digest):
        raise ValueError("The module tool did not return the requested source/Debug mapping")
    for key, filename in IMPORTS.items():
        expected = workspace / "source-mapping" / filename
        if environment[key] != str(expected) or not expected.is_file():
            raise ValueError(f"The module tool returned an invalid {key} import")
    for diagnostic in prepared["diagnostics"]:
        print(f"{diagnostic['ruleId']}: {diagnostic['message']}", file=sys.stderr)
    print(f"Source mapping: {workspace} ({digest})", file=sys.stderr, flush=True)
    return subprocess.run(sys.argv[1:], cwd=root, env=os.environ | environment).returncode


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, KeyError, TypeError) as error:
        print(f"with-source-mapping: {error}", file=sys.stderr)
        sys.exit(1)
