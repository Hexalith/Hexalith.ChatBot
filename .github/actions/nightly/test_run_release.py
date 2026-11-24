"""Check that the nightly hook requires the two actual release methods."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest


HOOK = Path(__file__).with_name("run-release.sh").resolve()
GATE_CLASS = "Hexalith.ChatBot.Release.Tests.GovernedStateReleaseGateTests"
METHODS = (
    "CommittedGovernedStateShouldPassM2StopShipSweepsInRealDaprTopology",
    "GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin",
)


class RunReleaseTests(unittest.TestCase):
    def test_requires_both_methods_to_pass_exactly_once_in_this_run(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            dotnet = directory / "dotnet"
            dotnet.write_text(f"""#!/usr/bin/env python3
import os
from pathlib import Path
import sys
from xml.etree import ElementTree as ET

args = sys.argv[1:]
assert args[args.index("--minimum-expected-tests") + 1] == "2"
mode = os.environ["CHATBOT_FAKE_RELEASE_MODE"]
if mode == "runner-error":
    sys.exit(27)
if mode == "no-report":
    sys.exit(0)

root = ET.Element("TestRun", xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
definitions = ET.SubElement(root, "TestDefinitions")
results = ET.SubElement(root, "Results")
for index, method in enumerate({METHODS!r}):
    if index == 1 and mode == "missing":
        continue
    test_id = f"id-{{index}}"
    definition = ET.SubElement(definitions, "UnitTest", id=test_id)
    ET.SubElement(definition, "TestMethod", className={GATE_CLASS!r},
                  name="RenamedMethod" if index == 1 and mode == "spoofed-display-name" else method)
    properties = dict(testId=test_id, testName={GATE_CLASS!r} + "." + method,
                      outcome="NotExecuted" if index == 1 and mode == "skipped" else "Passed")
    ET.SubElement(results, "UnitTestResult", properties)
    if index == 1 and mode == "duplicate":
        ET.SubElement(results, "UnitTestResult", properties)

report = Path(args[args.index("--results-directory") + 1], args[args.index("--report-xunit-trx-filename") + 1])
ET.ElementTree(root).write(report)
""", encoding="utf-8")
            dotnet.chmod(0o755)

            for mode in ("passed", "missing", "skipped", "duplicate", "spoofed-display-name", "no-report", "runner-error"):
                with self.subTest(mode=mode):
                    result = subprocess.run(
                        ["bash", str(HOOK)], cwd=directory, capture_output=True, text=True, timeout=15,
                        env=dict(os.environ, RUNNER_TEMP=temporary, PATH=f"{temporary}{os.pathsep}{os.environ['PATH']}",
                                 CHATBOT_FAKE_RELEASE_MODE=mode),
                    )
                    if mode == "passed":
                        self.assertEqual(result.returncode, 0, result.stderr)
                    elif mode == "runner-error":
                        self.assertEqual(result.returncode, 27, result.stderr)
                    else:
                        self.assertNotEqual(result.returncode, 0)
                    self.assertEqual(list(directory.glob("chatbot-release.*")), [])
