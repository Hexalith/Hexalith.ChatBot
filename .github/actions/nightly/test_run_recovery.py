"""Exercise recovery hook cleanup after its producer exits."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


HOOK = Path(__file__).with_name("run-recovery.sh").resolve()


class RunRecoveryTests(unittest.TestCase):
    def test_cleanup_is_scoped_and_preserves_producer_failure(self):
        for mode in ("killed", "clean", "bad-id", "cleanup-error", "remaining"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                self.write_shim(root, "date", "#!/usr/bin/env bash\nprintf '7\\n'\n")
                self.write_shim(root, "git", "#!/usr/bin/env bash\nprintf '%040d\\n' 1\n")
                self.write_shim(root, "timeout", """#!/usr/bin/env bash
case "$3" in
    300m)
        test "$TMPDIR" = "$TMP" && test "$TMPDIR" = "$TEMP" || exit 91
        test "$4" = timeout && test "$5" = --signal=INT && test "$7" = 16800s || exit 95
        printf '%s' "$TMPDIR" > "$FIXTURE_ROOT/scope"
        [[ "$RECOVERY_TEST_MODE" == clean ]] || exit 137
        exit 7
        ;;
    3m) exit 0 ;;
esac
if [[ "$2" == 15s ]]; then
    shift 2
    exec "$@"
fi
exit 92
""")
                self.write_shim(root, "docker", """#!/usr/bin/env python3
import json, os, sys
from pathlib import Path

root = Path(os.environ["FIXTURE_ROOT"])
mode = os.environ["RECOVERY_TEST_MODE"]
args = sys.argv[1:]
scope = (root / "scope").read_text()
with (root / "docker-calls").open("a") as log:
    print(json.dumps(args), file=log)
assert args[0] == "container"
if args[1] == "ls":
    assert args[2:] == ["--all", "--quiet", "--no-trunc", "--filter",
                        "label=io.hexalith.chatbot-live.scope=" + scope,
                        "--filter", "label=io.hexalith.chatbot-live.role=fixture-redis"]
    if mode == "bad-id":
        print("--all")
    elif mode != "clean" and (mode == "remaining" or not (root / "removed").exists()):
        print("a" * 64)
elif args[1] == "rm":
    assert args[2:] == ["--force", "--", "a" * 64]
    if mode == "cleanup-error":
        sys.exit(4)
    (root / "removed").touch()
else:
    sys.exit(93)
""")
                env = dict(os.environ, RUNNER_TEMP=directory, FIXTURE_ROOT=directory,
                           GITHUB_ENV="", RECOVERY_TEST_MODE=mode,
                           PATH=f"{directory}{os.pathsep}{os.environ['PATH']}")
                result = subprocess.run(
                    ["bash", str(HOOK)], cwd=root, capture_output=True, text=True, timeout=15,
                    env=env,
                )
                self.assertEqual(result.returncode, 1, result.stderr)
                (decision,) = root.glob("TestResults/nightly-recovery.*/decision.json")
                status = json.loads(decision.read_text())
                self.assertEqual(status["producerExit"], 7 if mode == "clean" else 137)
                self.assertEqual(status["cleanupExit"], {
                    "killed": 0, "clean": 0, "bad-id": 1, "cleanup-error": 4, "remaining": 1,
                }[mode])
                scope = Path((root / "scope").read_text())
                self.assertEqual(scope.name, "tmp")
                self.assertEqual(scope.parent.parent, root)
                calls = [json.loads(line) for line in (root / "docker-calls").read_text().splitlines()]
                self.assertEqual([call[1] for call in calls], {
                    "killed": ["ls", "rm", "ls"], "clean": ["ls"], "bad-id": ["ls"],
                    "cleanup-error": ["ls", "rm"], "remaining": ["ls", "rm", "ls"],
                }[mode])

    @staticmethod
    def write_shim(root, name, script):
        path = root / name
        path.write_text(script, encoding="utf-8")
        path.chmod(0o755)


if __name__ == "__main__":
    unittest.main()
