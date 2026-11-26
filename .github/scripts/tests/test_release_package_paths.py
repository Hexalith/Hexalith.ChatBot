"""Exercise the real packaging script from a caller outside the repository."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / ".github/scripts/pack-release-packages.sh"


class ReleasePackagePathTests(unittest.TestCase):
    def test_empty_destination_refuses_before_cleanup(self):
        with tempfile.TemporaryDirectory() as temporary:
            caller = Path(temporary)
            for name in ("keep.nupkg", "keep.snupkg", "keep.txt"):
                (caller / name).write_text("preserve caller files")
            before = {p.name: p.read_bytes() for p in caller.iterdir()}
            completed = subprocess.run(["bash", str(SCRIPT), "", "1.2.3"], cwd=caller,
                                       text=True, capture_output=True, timeout=30)
            self.assertEqual(completed.returncode, 64, completed.stdout + completed.stderr)
            self.assertIn("empty output directory", completed.stderr)
            self.assertEqual({p.name: p.read_bytes() for p in caller.iterdir()}, before)

    def test_caller_relative_absolute_and_dash_prefixed_outputs(self):
        for destination in ("artifacts", "-artifacts", "-", "directory with spaces", "absolute"):
            with self.subTest(destination=destination), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                repository = root / "repository"
                script = repository / ".github/scripts/pack-release-packages.sh"
                script.parent.mkdir(parents=True)
                script.write_bytes(SCRIPT.read_bytes())
                (repository / "release-metadata.json").write_bytes((ROOT / "release-metadata.json").read_bytes())
                caller = root / "caller"
                caller.mkdir()
                output = caller / destination
                output.mkdir()
                (output / "stale.nupkg").write_text("old")
                (output / "stale.snupkg").write_text("old")
                (output / "keep.txt").write_text("keep")
                tools = root / "tools"
                tools.mkdir()
                dotnet = tools / "dotnet"
                dotnet.write_text("""#!/usr/bin/env python3
import json, os, pathlib, sys
args = sys.argv[1:]
assert args[0] == 'pack'
assert args[args.index('--configuration') + 1] == 'Release'
assert '-p:UseHexalithProjectReferences=false' in args
output = pathlib.Path(args[args.index('--output') + 1])
assert output.is_absolute(), str(output)
assert pathlib.Path.cwd() == pathlib.Path(os.environ['TEST_REPOSITORY'])
inventory = json.loads(pathlib.Path('release-metadata.json').read_text())['inventory']['packages']
package = next(item for item in inventory if item['project'] == args[1])
for extension in ('nupkg', 'snupkg'):
    (output / (package['id'] + '.1.2.3.' + extension)).write_text('new package')
with open(os.environ['TEST_CALLS'], 'a') as calls:
    calls.write(json.dumps(args) + '\\n')
""", encoding="utf-8")
                dotnet.chmod(0o755)
                calls = root / "calls.jsonl"
                env = os.environ | {"PATH": str(tools) + os.pathsep + os.environ["PATH"],
                                    "TEST_REPOSITORY": str(repository), "TEST_CALLS": str(calls),
                                    "CDPATH": ".", "OLDPWD": str(root)}
                argument = str(output) if destination == "absolute" else destination
                completed = subprocess.run(["bash", str(script), argument, "1.2.3"], cwd=caller,
                                           env=env, text=True, capture_output=True, timeout=30)
                self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
                inventory = json.loads((repository / "release-metadata.json").read_text())["inventory"]["packages"]
                self.assertEqual({p.name for p in output.iterdir()}, {"keep.txt"} | {
                    f"{p['id']}.1.2.3.{extension}" for p in inventory for extension in ("nupkg", "snupkg")})
                self.assertEqual(len(calls.read_text().splitlines()), 3)
                self.assertFalse((repository / destination).exists())


if __name__ == "__main__":
    unittest.main()
