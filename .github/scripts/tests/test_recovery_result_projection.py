"""Execute the operational recovery artifact projection using the built evidence tool."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / ".github/scripts/project-recovery-results.sh"


class RecoveryResultProjectionTests(unittest.TestCase):
    def project(self, shape, outcome):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        raw = root / "runner-temp/raw.trx"
        raw.parent.mkdir()
        results = root / "TestResults"
        results.mkdir()
        (results / "domain.manifest.json").write_text('{"retained":"domain-metadata"}', encoding="utf-8")
        if shape != "absent":
            raw.write_text(self.trx("Failed" if shape == "failed" else "Passed")
                           if shape != "malformed" else "malformed private-payload", encoding="utf-8")
        (raw.parent / "raw.ctrf.json").write_text('private-payload', encoding="utf-8")
        environment = os.environ.copy()
        environment.setdefault("STORY_EVIDENCE_GATE_DLL", str(
            ROOT / "tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll"))
        completed = subprocess.run(["bash", str(SCRIPT), str(raw), str(results), outcome], cwd=ROOT,
                                   env=environment, text=True, capture_output=True, timeout=30)
        summary = results / "workflow-attempt/recovery-attempt-summary.json"
        self.assertTrue(summary.is_file(), completed.stdout + completed.stderr)
        for output in results.rglob("*"):
            if output.is_file():
                self.assertNotIn("private-payload", output.read_text(encoding="utf-8"))
        self.assertEqual((results / "domain.manifest.json").read_text(), '{"retained":"domain-metadata"}')
        self.assertTrue((raw.parent / "raw.ctrf.json").is_file())
        return completed, results, json.loads(summary.read_text())

    def test_success_sanitizes_trx_and_keeps_raw_files_outside_upload(self):
        completed, results, summary = self.project("passed", "success")
        self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
        sanitized = (results / "live-recovery-validation.trx").read_text()
        self.assertIn('outcome="Passed"', sanitized)
        self.assertNotIn("Output", sanitized)
        self.assertEqual(summary["trxState"], "parsed")
        self.assertEqual(len(list(results.rglob("*.trx"))), 1)

    def test_failure_absence_and_malformed_input_retain_only_safe_attempt_metadata(self):
        for shape, state in [("failed", "parsed"), ("absent", "absent"), ("malformed", "malformed")]:
            with self.subTest(shape=shape):
                completed, results, summary = self.project(shape, "failure")
                self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
                self.assertEqual(summary["trxState"], state)
                self.assertEqual(list(results.rglob("*.trx")), [])

    def test_success_with_invalid_trx_fails_after_retaining_attempt_metadata(self):
        completed, results, summary = self.project("malformed", "success")
        self.assertNotEqual(completed.returncode, 0)
        self.assertEqual(summary["trxState"], "malformed")
        self.assertFalse((results / "live-recovery-validation.trx").exists())

    def test_reused_results_root_cannot_publish_an_earlier_success(self):
        completed, results, _ = self.project("passed", "success")
        self.assertEqual(completed.returncode, 0)
        environment = os.environ.copy()
        environment.setdefault("STORY_EVIDENCE_GATE_DLL", str(
            ROOT / "tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll"))
        second = subprocess.run(["bash", str(SCRIPT), str(results.parent / "absent.trx"), str(results), "failure"],
                                cwd=ROOT, env=environment, text=True, capture_output=True, timeout=30)
        self.assertEqual(second.returncode, 0, second.stdout + second.stderr)
        self.assertFalse((results / "live-recovery-validation.trx").exists())
        summary = json.loads((results / "workflow-attempt/recovery-attempt-summary.json").read_text())
        self.assertEqual(summary["trxState"], "absent")

    def test_blocked_summary_output_fails_the_projection(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "workflow-attempt").write_text("blocked-output", encoding="utf-8")
            environment = os.environ.copy()
            environment.setdefault("STORY_EVIDENCE_GATE_DLL", str(
                ROOT / "tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll"))
            completed = subprocess.run(["bash", str(SCRIPT), str(root / "absent.trx"), str(root), "failure"],
                                       cwd=ROOT, env=environment, text=True, capture_output=True, timeout=30)
            self.assertNotEqual(completed.returncode, 0)
            self.assertFalse((root / "workflow-attempt/recovery-attempt-summary.json").exists())

    def test_arbitrary_safe_shaped_attribute_values_cannot_enter_the_summary(self):
        for attribute in ["className", "name", "outcome"]:
            with self.subTest(attribute=attribute):
                completed, results, _ = self.project("failed", "failure")
                self.assertEqual(completed.returncode, 0)
                raw = results.parent / "runner-temp/raw.trx"
                text = raw.read_text()
                if attribute == "className":
                    text = text.replace("Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests", "Customer.Acquisition.ConfidentialDeal123")
                elif attribute == "name":
                    text = text.replace("LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate", "Customer.Acquisition.ConfidentialDeal123")
                else:
                    text = text.replace('outcome="Completed"', 'outcome="Customer.Acquisition.ConfidentialDeal123"')
                raw.write_text(text, encoding="utf-8")
                environment = os.environ.copy()
                environment.setdefault("STORY_EVIDENCE_GATE_DLL", str(
                    ROOT / "tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll"))
                projected = subprocess.run(["bash", str(SCRIPT), str(raw), str(results), "failure"],
                                           cwd=ROOT, env=environment, text=True, capture_output=True, timeout=30)
                self.assertEqual(projected.returncode, 0, projected.stdout + projected.stderr)
                self.assertNotIn("ConfidentialDeal123", (results / "workflow-attempt/recovery-attempt-summary.json").read_text())

    @staticmethod
    def trx(outcome):
        passed = int(outcome == "Passed")
        return f'''<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times start="2026-10-28T12:00:00Z" finish="2026-10-28T12:00:01Z" />
  <Results><UnitTestResult testId="recovery-1" outcome="{outcome}">
    <Output><StdOut>private-payload</StdOut></Output>
  </UnitTestResult></Results>
  <TestDefinitions><UnitTest id="recovery-1"><TestMethod
    className="Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests"
    name="LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate" />
  </UnitTest></TestDefinitions>
  <ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="{passed}"
    failed="{1-passed}" error="0" timeout="0" aborted="0" inconclusive="0" notExecuted="0" />
  </ResultSummary>
</TestRun>'''


if __name__ == "__main__":
    unittest.main()
