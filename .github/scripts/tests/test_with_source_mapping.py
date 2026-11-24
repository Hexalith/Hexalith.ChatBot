"""Verify the source-mapping imports received by a real child process."""

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


REPOSITORY = Path(__file__).resolve().parents[3]
IMPORTS = {
    "CustomBeforeDirectoryBuildProps": "SourceMapping.targets",
    "CustomAfterMicrosoftCommonProps": "SourceMapping.props",
    "CustomBeforeDirectoryBuildTargets": "SourceMapping.before-targets.props",
    "CustomAfterDirectoryBuildTargets": "SourceMapping.late.props",
    "AfterMicrosoftNETSdkTargets": "SourceMapping.final.targets",
}
DIGEST = "a" * 64


class SourceMappingWrapperTests(unittest.TestCase):
    def test_forwards_all_prepared_msbuild_imports_to_the_child(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            wrapper = root / ".github/scripts/with-source-mapping.py"
            wrapper.parent.mkdir(parents=True)
            shutil.copyfile(REPOSITORY / ".github/scripts/with-source-mapping.py", wrapper)
            manifest = root / ".config/dotnet-tools.json"
            manifest.parent.mkdir()
            shutil.copyfile(REPOSITORY / ".config/dotnet-tools.json", manifest)
            pin = json.loads(manifest.read_text())["tools"]["hexalith.builds.module.cli"]["version"]
            dotnet = root / "dotnet"
            dotnet.write_text(f"""#!{sys.executable}
import json
from pathlib import Path
import sys

if sys.argv[1:3] == ["tool", "restore"]:
    sys.exit(0)
if sys.argv[-1] == "--version":
    print({pin!r})
    sys.exit(0)
workspace = Path(sys.argv[sys.argv.index("--workspace") + 1])
imports_root = workspace / "source-mapping"
imports_root.mkdir(parents=True)
environment = {{"Configuration": "Debug", "HEXALITH_SOURCE_MAPPING_HASH": {DIGEST!r}}}
for name, filename in {IMPORTS!r}.items():
    imported = imports_root / filename
    imported.touch()
    environment[name] = str(imported)
print(json.dumps({{"status": "prepared", "outcome": {{"exitCode": "Success"}}, "diagnostics": [],
    "buildPreparation": {{"mode": "source", "workspace": str(workspace), "mappingHash": {DIGEST!r},
                          "environment": environment}}}}))
""", encoding="utf-8")
            dotnet.chmod(0o755)
            names = [*IMPORTS, "Configuration", "HEXALITH_SOURCE_MAPPING_HASH", "CHATBOT_TEST_INHERITED"]
            child = "import json, os, sys; print(json.dumps({key: os.environ[key] for key in sys.argv[1:]})); sys.exit(7)"
            result = subprocess.run(
                [sys.executable, str(wrapper), sys.executable, "-c", child, *names],
                cwd=root, env=dict(os.environ, PATH=f"{root}{os.pathsep}{os.environ['PATH']}", CHATBOT_TEST_INHERITED="present"),
                capture_output=True, text=True, timeout=30,
            )

            self.assertEqual(result.returncode, 7, result.stdout + result.stderr)
            workspace, = (root / ".artifacts/source-mapping").iterdir()
            self.assertEqual(json.loads(result.stdout), {
                **{name: str(workspace / "source-mapping" / filename) for name, filename in IMPORTS.items()},
                "Configuration": "Debug",
                "HEXALITH_SOURCE_MAPPING_HASH": DIGEST,
                "CHATBOT_TEST_INHERITED": "present",
            })
