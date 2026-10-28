"""Execute the Linux workflow boundary without network or domain services.

Build StoryEvidenceGate.Tests in Debug first, or set STORY_EVIDENCE_TEST_RUNNER
(the CI invocation points it at the already-built Release runner). Missing runners
fail the suite; these checks never silently skip platform or runner evidence.
"""

import json
import os
from pathlib import Path
import shlex
import shutil
import sys
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / ".github" / "scripts"


class StoryEvidenceGuardsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="story-evidence-guards-")
        self.addCleanup(self.temporary.cleanup)
        self.work = Path(self.temporary.name)
        self.bin = self.work / "bin"
        self.bin.mkdir()
        self.env = os.environ.copy()
        self.env.update({"PATH": f"{self.bin}{os.pathsep}{self.env['PATH']}",
                         "GITHUB_REPOSITORY": "Hexalith/Hexalith.ChatBot",
                         "DOTNET_CLI_UI_LANGUAGE": "en-US"})
        self.output = self.work / "github-output"
        self.output.touch()
        self.env.update({"GITHUB_OUTPUT": str(self.output), "EVENT_NAME": "push",
                         "PR_BASE": "", "PR_HEAD": "", "PUSH_BASE": "", "PUSH_HEAD": ""})

    def execute(self, script, cwd=None):
        return subprocess.run(["bash", str(SCRIPTS / script)], cwd=cwd or self.work,
                              env=self.fixture_git_environment() if cwd is None else self.env,
                              text=True, capture_output=True, timeout=120)

    def fixture_git_environment(self):
        environment = {name: value for name, value in self.env.items() if not name.startswith("GIT_")}
        environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_SYSTEM=os.devnull,
                           GIT_CONFIG_GLOBAL=os.devnull)
        return environment

    def git(self, *args):
        return subprocess.run(["git", *args], cwd=self.work, env=self.fixture_git_environment(),
                              text=True, capture_output=True, timeout=30, check=True).stdout.strip()

    def history(self):
        empty_template = self.work / "empty-git-template"
        empty_template.mkdir()
        self.git("init", "--initial-branch=main", f"--template={empty_template}")
        self.git("config", "user.name", "Guard Tests")
        self.git("config", "user.email", "guard@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "tag.gpgsign", "false")
        self.git("config", "core.hooksPath", str(empty_template))
        revisions = []
        for index in range(3):
            (self.work / "source").write_text(str(index), encoding="utf-8")
            self.git("add", "source")
            self.git("commit", "-m", f"test: revision {index}")
            revisions.append(self.git("rev-parse", "HEAD"))
        return revisions

    def test_fixture_git_ignores_ambient_configuration_and_templates(self):
        hostile_template = self.work / "hostile-template"
        hooks = hostile_template / "hooks"
        hooks.mkdir(parents=True)
        pre_commit = hooks / "pre-commit"
        pre_commit.write_text("#!/bin/sh\nexit 91\n", encoding="utf-8")
        pre_commit.chmod(0o755)
        global_config = self.work / "ambient.gitconfig"
        global_config.write_text(
            f"[commit]\n gpgsign = true\n[core]\n hooksPath = {hooks}\n"
            "[fixture]\n ambient = inherited\n", encoding="utf-8")
        self.env.update(GIT_CONFIG_GLOBAL=str(global_config), GIT_TEMPLATE_DIR=str(hostile_template),
                        GIT_CONFIG_COUNT="1", GIT_CONFIG_KEY_0="commit.gpgsign", GIT_CONFIG_VALUE_0="true")

        self.assertEqual(len(self.history()), 3)
        self.assertEqual(self.git("config", "--get", "--default", "absent", "fixture.ambient"), "absent")
        self.assertEqual(self.git("config", "--local", "--get", "commit.gpgsign"), "false")
        self.assertEqual(self.git("config", "--local", "--get", "tag.gpgsign"), "false")
        self.assertEqual(self.git("config", "--local", "--get", "core.hooksPath"),
                         str(self.work / "empty-git-template"))
        self.assertFalse((self.work / ".git/hooks/pre-commit").exists())

    def test_exact_event_matrix(self):
        base, parent, head = self.history()
        cases = [("pull_request", base, head, "", base),
                 ("push", "", "", base, base),
                 ("push", "", "", "0" * 40, parent),
                 ("push", "", "", "", parent),
                 ("schedule", "", "", base, head),
                 ("workflow_dispatch", "", "", base, head)]
        for event, pr_base, pr_head, push_base, expected_base in cases:
            with self.subTest(event=event, push_base=push_base):
                self.output.write_text("", encoding="utf-8")
                self.env.update(EVENT_NAME=event, PR_BASE=pr_base, PR_HEAD=pr_head,
                                PUSH_BASE=push_base, PUSH_HEAD=parent if event == "pull_request" else head)
                result = self.execute("resolve-story-evidence-bounds.sh")
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(self.output.read_text(), f"base={expected_base}\nhead={head}\n")

    def test_event_refusals_emit_no_success_outputs(self):
        base, parent, head = self.history()
        cases = [("push", "f" * 40, head, "", ""),
                 ("push", base, parent, "", ""),
                 ("unknown", base, head, "", ""),
                 ("pull_request", "", head, base[:12], head),
                 ("pull_request", "", head, "", head)]
        for event, push_base, push_head, pr_base, pr_head in cases:
            with self.subTest(event=event, push_base=push_base, pr_base=pr_base):
                self.output.write_text("", encoding="utf-8")
                self.env.update(EVENT_NAME=event, PUSH_BASE=push_base, PUSH_HEAD=push_head,
                                PR_BASE=pr_base, PR_HEAD=pr_head)
                result = self.execute("resolve-story-evidence-bounds.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(self.output.read_bytes(), b"")

    def artifacts(self, locators):
        locator_path = self.work / "locators"
        locator_path.write_text(locators, encoding="utf-8")
        self.env.update(RESULTS_ROOT=str(self.work / "results"),
                        RETAINED_LOCATORS_PATH=str(locator_path), GH_CALLS=str(self.work / "gh-calls"))
        gh = self.bin / "gh"
        gh.write_text("#!/usr/bin/env python3\nimport json, os, sys\n"
                      "with open(os.environ['GH_CALLS'], 'a') as stream:\n"
                      "    stream.write(json.dumps(sys.argv[1:]) + '\\n')\n", encoding="utf-8")
        gh.chmod(0o755)

    def test_retained_download_is_exact_isolated_and_deduplicated(self):
        artifacts = [("123", "approved-evidence"), ("123", "other-evidence"),
                     ("456", "approved-evidence")]
        locators = [f"github-actions://Hexalith/Hexalith.ChatBot/runs/{run}/artifacts/{name}"
                    for run, name in artifacts]
        self.artifacts("\n" + "\n".join(locators + [locators[0], locators[2]]))
        result = self.execute("collect-story-evidence-artifacts.sh")
        self.assertEqual(result.returncode, 0, result.stderr)
        calls = [json.loads(line) for line in (self.work / "gh-calls").read_text().splitlines()]
        expected = []
        for run, name in artifacts:
            destination = self.work / "results/retained" / run / name
            expected.append(["run", "download", run, "--repo", "Hexalith/Hexalith.ChatBot",
                             "--name", name, "--dir", str(destination)])
            self.assertTrue(destination.is_dir())
        self.assertEqual(calls, expected)

    def test_retained_refusals_preflight_before_any_download(self):
        safe = "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/safe"
        cases = ["github-actions://Other/Repository/runs/123/artifacts/safe",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/0/artifacts/safe",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/../escape",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/$(touch injected)",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/`touch injected`",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/safe;touch injected",
                 "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/" + "a" * 129]
        for unsafe in cases:
            with self.subTest(locator=unsafe):
                self.artifacts(f"{safe}\n{unsafe}")
                result = self.execute("collect-story-evidence-artifacts.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((self.work / "gh-calls").exists())
                self.assertFalse((self.work / "results").exists())
                self.assertFalse((self.work / "injected").exists())

    def test_retained_destination_collisions_never_download(self):
        locator = "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/safe"
        valid = "github-actions://Hexalith/Hexalith.ChatBot/runs/456/artifacts/earlier-valid"
        self.artifacts(f"{valid}\n{locator}")
        destination = self.work / "results/retained/123/safe"
        destination.parent.mkdir(parents=True)
        for kind in ("directory", "dangling-symlink"):
            with self.subTest(kind=kind):
                if kind == "directory":
                    destination.mkdir()
                else:
                    destination.symlink_to(self.work / "absent")
                result = self.execute("collect-story-evidence-artifacts.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("collision", result.stderr)
                self.assertFalse((self.work / "gh-calls").exists())
                self.assertFalse((self.work / "results/retained/456/earlier-valid").exists())
                if kind == "directory":
                    destination.rmdir()
                else:
                    destination.unlink()

    def test_retained_destination_ancestry_cannot_escape_isolation(self):
        locator = "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/safe"
        valid = "github-actions://Hexalith/Hexalith.ChatBot/runs/456/artifacts/earlier-valid"
        self.artifacts(f"{valid}\n{locator}")
        for index, component in enumerate(("root", "retained", "run", "file")):
            with self.subTest(component=component):
                results = self.work / f"results-{index}"
                self.env["RESULTS_ROOT"] = str(results)
                ancestor = {"root": results, "retained": results / "retained",
                            "run": results / "retained/123", "file": results / "retained"}[component]
                ancestor.parent.mkdir(parents=True, exist_ok=True)
                outside = self.work / f"outside-{index}"
                outside.mkdir()
                if component == "file":
                    ancestor.touch()
                else:
                    ancestor.symlink_to(outside, target_is_directory=True)
                result = self.execute("collect-story-evidence-artifacts.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("ancestry", result.stderr)
                self.assertFalse((self.work / "gh-calls").exists())
                self.assertFalse((results / "retained/456/earlier-valid").exists())
                self.assertEqual(list(outside.iterdir()), [])

    def test_retained_download_failure_stops_collection(self):
        prefix = "github-actions://Hexalith/Hexalith.ChatBot/runs/123/artifacts/"
        self.artifacts(f"{prefix}first\n{prefix}second\n")
        gh = self.bin / "gh"
        gh.write_text(gh.read_text() + "raise SystemExit(17)\n", encoding="utf-8")
        result = self.execute("collect-story-evidence-artifacts.sh")
        self.assertEqual(result.returncode, 17)
        self.assertEqual(len((self.work / "gh-calls").read_text().splitlines()), 1)
        self.assertFalse((self.work / "results/retained/123/second").exists())

    def reserve(self, elapsed):
        origin = 1700000000
        start = self.work / "start.epoch"
        start.write_text(f"{origin}\n", encoding="utf-8")
        date = self.bin / "date"
        date.write_text('#!/usr/bin/env bash\nprintf "%s\\n" "$TEST_NOW"\n', encoding="utf-8")
        date.chmod(0o755)
        self.env.update(JOB_START_PATH=str(start), TEST_NOW=str(origin + elapsed),
                        CLOSEOUT_DEADLINE_PATH=str(self.work / "closeout.epoch"),
                        RESERVE_REPORT_PATH=str(self.work / "reports/completion-reserve.json"))
        return origin

    def test_reserve_admits_2399_seconds(self):
        origin = self.reserve(2399)
        result = self.execute("check-story-evidence-reserve.sh")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.work / "closeout.epoch").read_text(), f"{origin + 330 * 60}\n")
        self.assertFalse((self.work / "reports/completion-reserve.json").exists())

    def test_reserve_refuses_2400_with_bounded_stable_report(self):
        for elapsed in (2400, 2401, 24 * 60 * 60):
            with self.subTest(elapsed=elapsed):
                self.reserve(elapsed)
                report = self.work / "reports/completion-reserve.json"
                report.unlink(missing_ok=True)
                result = self.execute("check-story-evidence-reserve.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertLess(report.stat().st_size, 256)
                self.assertEqual(json.loads(report.read_text()), {
                    "schemaVersion": "1.0", "passed": False, "code": "completion_reserve_exhausted",
                    "elapsedSeconds": elapsed, "thresholdSeconds": 2400})
                self.assertFalse((self.work / "closeout.epoch").exists())

    def test_reserve_refuses_missing_malformed_and_future_origin(self):
        self.reserve(0)
        start = Path(self.env["JOB_START_PATH"])
        for value in (None, "", "not-an-epoch", "$(touch injected)", "99999999999", "1700000001"):
            with self.subTest(origin=value):
                if value is None:
                    start.unlink()
                else:
                    start.write_text(value, encoding="utf-8")
                result = self.execute("check-story-evidence-reserve.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((self.work / "closeout.epoch").exists())
                self.assertFalse((self.work / "reports/completion-reserve.json").exists())
                self.assertFalse((self.work / "injected").exists())
        start.write_text("1700000000", encoding="utf-8")
        self.env["TEST_NOW"] = "invalid-clock"
        result = self.execute("check-story-evidence-reserve.sh")
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.work / "closeout.epoch").exists())

    def test_ordinary_lane_rejects_real_zero_test_runner_without_checksums(self):
        runner = Path(self.env.get("STORY_EVIDENCE_TEST_RUNNER", str(
            ROOT / "tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.Tests")))
        self.assertTrue(runner.is_file() and os.access(runner, os.X_OK),
                        "Build StoryEvidenceGate.Tests in Debug or set STORY_EVIDENCE_TEST_RUNNER to a built runner")
        raw = self.work / "raw.ctrf.json"
        no_match = "Hexalith.ChatBot.StoryEvidenceGate.Tests.NoSuchClass.NoSuchTest"
        result = subprocess.run([str(runner), "-automated", "sync", "-noAutoReporters", "-noColor",
                                 "-reporter", "quiet", "-result-ctrf", str(raw), "-method", no_match],
                                env=self.env, cwd=ROOT, text=True, capture_output=True, timeout=120)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(raw.read_text())["results"]["summary"]["tests"], 0)
        test_root = self.work / "tests/Zero.Tests"
        test_root.mkdir(parents=True)
        (test_root / "Zero.Tests.csproj").touch()
        override = self.bin / "zero-runner"
        override.write_text(f'#!/usr/bin/env bash\nexec {shlex.quote(str(runner))} "$@" -method {shlex.quote(no_match)}\n',
                            encoding="utf-8")
        override.chmod(0o755)
        results = self.work / "ordinary-results"
        self.env.update(MERGE_TEST_ROOT=str(test_root.parent), MERGE_TEST_EXPECTED_LANES="1",
                        MERGE_TEST_RUNNER_OVERRIDE=str(override), MERGE_TEST_RESULTS_ROOT=str(results))
        result = self.execute("run-merge-test-lanes.sh", ROOT)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("xUnit lane executed zero tests", result.stderr)
        self.assertEqual(json.loads((results / "Zero.Tests.ctrf.json").read_text())["results"]["summary"]["tests"], 0)
        self.assertFalse(list(results.glob("*.sha256")))



class SourceMappingWrapperTests(unittest.TestCase):
    """Exercise the producer-to-child boundary without packages or infrastructure."""

    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="source-mapping-wrapper-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        scripts = self.root / ".github/scripts"
        scripts.mkdir(parents=True)
        self.wrapper = scripts / "with-source-mapping.py"
        shutil.copyfile(SCRIPTS / "with-source-mapping.py", self.wrapper)
        config = self.root / ".config"
        config.mkdir()
        shutil.copyfile(ROOT / ".config/dotnet-tools.json", config / "dotnet-tools.json")
        self.bin = self.root / "bin"
        self.bin.mkdir()
        producer = self.bin / "dotnet"
        producer.write_text("""#!/usr/bin/env python3
import json, os, pathlib, sys
case = os.environ.get("MAPPING_FIXTURE", "valid")
if sys.argv[1:3] == ["tool", "restore"]:
    sys.exit(23 if case == "restore-failure" else 0)
if "--version" in sys.argv:
    print("0.0.0" if case == "wrong-version" else
          json.loads(pathlib.Path(".config/dotnet-tools.json").read_text())["tools"]["hexalith.builds.module.cli"]["version"])
    sys.exit(0)
if case == "prepare-failure":
    sys.exit(24)
workspace = pathlib.Path(sys.argv[sys.argv.index("--workspace") + 1])
workspace.mkdir(parents=True)
imports = {
    "CustomBeforeDirectoryBuildProps": "SourceMapping.targets",
    "CustomAfterMicrosoftCommonProps": "SourceMapping.props",
    "CustomBeforeDirectoryBuildTargets": "SourceMapping.before-targets.props",
    "CustomAfterDirectoryBuildTargets": "SourceMapping.late.props",
    "AfterMicrosoftNETSdkTargets": "SourceMapping.final.targets",
}
environment = {"Configuration": "Debug", "HEXALITH_SOURCE_MAPPING_HASH": "a" * 64}
for key, filename in imports.items():
    path = workspace / "source-mapping" / filename
    path.parent.mkdir(exist_ok=True)
    path.touch()
    environment[key] = str(path)
if case == "missing-import":
    pathlib.Path(environment["AfterMicrosoftNETSdkTargets"]).unlink()
if case == "unexpected-import":
    environment["AfterMicrosoftNETSdkTargets"] = str(workspace / "other.targets")
if case == "wrong-hash":
    environment["HEXALITH_SOURCE_MAPPING_HASH"] = "b" * 64
if case == "extra-environment":
    environment["UNEXPECTED_PROPERTY"] = "value"
print(json.dumps({"status": "failed" if case == "failed-status" else "prepared",
    "outcome": {"exitCode": "Success"}, "diagnostics": [],
    "buildPreparation": {"mode": "source", "workspace": str(workspace),
        "mappingHash": "a" * 64, "environment": environment}}))
""", encoding="utf-8")
        producer.chmod(0o755)
        self.marker = self.root / "executed.json"

    def execute(self, case):
        environment = os.environ | {"PATH": f"{self.bin}{os.pathsep}{os.environ['PATH']}",
                                   "MAPPING_FIXTURE": case}
        return subprocess.run(
            [sys.executable, str(self.wrapper), sys.executable, "-c",
             "import json,os,pathlib,sys; pathlib.Path(sys.argv[1]).write_text(json.dumps({"
             "'configuration':os.environ['Configuration'],'hash':os.environ['HEXALITH_SOURCE_MAPPING_HASH'],"
             "'cwd':os.getcwd()})); sys.exit(7)", str(self.marker)],
            cwd=self.root, env=environment, text=True, capture_output=True, timeout=30)

    def test_refusals_never_launch_the_child(self):
        for case in ("restore-failure", "wrong-version", "prepare-failure", "failed-status",
                     "wrong-hash", "unexpected-import", "missing-import", "extra-environment"):
            with self.subTest(case=case):
                result = self.execute(case)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertFalse(self.marker.exists(), result.stdout + result.stderr)

    def test_verified_mapping_reaches_child_and_preserves_its_exit_code(self):
        result = self.execute("valid")
        self.assertEqual(result.returncode, 7, result.stdout + result.stderr)
        self.assertEqual(json.loads(self.marker.read_text()),
                         {"configuration": "Debug", "hash": "a" * 64, "cwd": str(self.root)})


if __name__ == "__main__":
    unittest.main()
