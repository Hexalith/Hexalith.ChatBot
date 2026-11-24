"""Exercise the runner-specific Playwright resolution and local exports."""

import contextlib
import io
import os
from pathlib import Path
import runpy
import shlex
import subprocess
import tempfile
import unittest
from unittest.mock import patch


RUNTIME = runpy.run_path(str(Path(__file__).with_name("require-runtime.py")))


class RequireRuntimeTests(unittest.TestCase):
    def test_selects_the_match_from_playwrights_packaged_node_executables(self):
        for system, architecture, locator in (
            ("Linux", "x86_64", "linux-x64/node"),
            ("Linux", "aarch64", "linux-arm64/node"),
            ("Darwin", "x86_64", "darwin-x64/node"),
            ("Darwin", "arm64", "darwin-arm64/node"),
            ("Windows", "AMD64", "win32_x64/node.exe"),
        ):
            with self.subTest(system=system, architecture=architecture), tempfile.TemporaryDirectory() as directory:
                driver = Path(directory, ".playwright")
                expected = driver / "node" / locator
                expected.parent.mkdir(parents=True)
                expected.touch()
                self.assertEqual(RUNTIME["playwright_node"](driver, system, architecture), expected)
        with self.assertRaisesRegex(ValueError, "does not bundle a Node executable"):
            RUNTIME["playwright_node"](Path(".playwright"), "Linux", "riscv64")

    def test_uses_the_platform_binary_from_the_prepared_output_under_emulation(self):
        with tempfile.TemporaryDirectory() as directory:
            driver = Path(directory, ".playwright")
            node = driver / "node/darwin-arm64/node"
            node.parent.mkdir(parents=True)
            node.touch()
            self.assertEqual(RUNTIME["playwright_node"](driver, "Darwin", "x86_64"), node)
            with self.assertRaises(FileNotFoundError):
                RUNTIME["playwright_node"](driver, "Linux", "x86_64")

    def test_writes_github_settings_or_shell_quoted_exports_after_verification(self):
        with tempfile.TemporaryDirectory() as directory:
            tool_root = Path(directory, "published tooling")
            tool_root.mkdir()
            (tool_root / "Hexalith.Builds.Tooling.dll").touch()
            environment_file = Path(directory, "github-env")
            chromium = "/fixture/test browser/with 'quote'"
            node = Path(directory, "tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net11.0/.playwright/node/linux-arm64/node")
            node.parent.mkdir(parents=True)
            node.touch()
            for github_env in ({"GITHUB_ENV": str(environment_file)}, {}):
                outputs = [None, None, chromium, None, str(tool_root)]
                results = [subprocess.CompletedProcess([], 0, stdout=output) for output in outputs]
                stdout = io.StringIO()
                with (patch.dict(os.environ, github_env, clear=True),
                      patch("platform.system", return_value="Linux"),
                      patch("platform.machine", return_value="aarch64"),
                      patch("subprocess.run", side_effect=results) as run,
                      contextlib.chdir(directory),
                      contextlib.redirect_stdout(stdout)):
                    RUNTIME["main"]()

                self.assertTrue(run.call_args_list[2].args[0][0].endswith("linux-arm64/node"))
                self.assertEqual(run.call_args_list[3].args[0], [chromium, "--version"])
                if github_env:
                    self.assertIn(f"CHROME_EXECUTABLE_PATH={chromium}\n", environment_file.read_text())
                    self.assertEqual(stdout.getvalue(), "")
                else:
                    exports = [shlex.split(line) for line in stdout.getvalue().splitlines()]
                    self.assertIn(["export", f"CHROME_EXECUTABLE_PATH={chromium}"], exports)
                    self.assertIn(["export", "HEXALITH_CHATBOT_TIER3_REQUIRED=1"], exports)
