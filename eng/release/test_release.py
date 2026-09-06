import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from metadata import identity
from verify import expected_names, verify


class ReleaseTests(unittest.TestCase):
    def test_tag_channels_and_invalid_versions(self):
        sha = "abcdef" + "0" * 34
        for tag, channel in (("2.0.0", "stable"), ("v2.1.0.alpha.12", "alpha"), ("2.0.0.beta.1", "beta"), ("2.0.0.ci.abcdef", "ci")):
            with self.subTest(tag=tag):
                self.assertEqual(channel, identity("refs/tags/" + tag, sha)["channel"])
        for tag in ("2.0.0-beta.1", "2.0.0.alpha.0", "2.0.0.ci.000000", "2.0.0;echo bad", "02.0.0"):
            with self.assertRaises(ValueError):
                identity("refs/tags/" + tag, sha)
        self.assertEqual("false", identity("refs/heads/refactor/xsr", sha)["release"])

    def test_incomplete_or_empty_package_set_cannot_release(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            names = expected_names("2.0.0")
            self.assertEqual(18, len(names))
            for name in names:
                (root / name).write_bytes(b"package")
            verify(root, "2.0.0")
            self.assertEqual(18, len((root / "SHA256SUMS").read_text().splitlines()))
            one = root / next(iter(names))
            one.write_bytes(b"")
            with self.assertRaises(ValueError):
                verify(root, "2.0.0")
            one.unlink()
            with self.assertRaises(ValueError):
                verify(root, "2.0.0")

    def test_changelog_preserves_commit_text_without_executing_it(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            def git(*args):
                return subprocess.check_output(["git", *args], cwd=root, encoding="utf-8").strip()
            git("init", "-q")
            git("config", "user.name", "Release Test")
            git("config", "user.email", "test@example.invalid")
            git("commit", "--allow-empty", "-m", "old entry")
            git("tag", "2.0.0.alpha.1")
            git("commit", "--allow-empty", "-m", "fix: literal $(touch SHOULD_NOT_EXIST)", "-m", "Details with `backticks` and 中文")
            sha = git("rev-parse", "HEAD")
            environment = dict(os.environ)
            environment.pop("GITHUB_OUTPUT", None)
            subprocess.run([sys.executable, str(Path(__file__).with_name("metadata.py")), "--ref", "refs/tags/v2.0.0.beta.1", "--sha", sha, "--output", "out"], cwd=root, env=environment, check=True)
            notes = (root / "out/CHANGELOG.md").read_text(encoding="utf-8")
            self.assertIn("$(touch SHOULD_NOT_EXIST)", notes)
            self.assertIn("Details with `backticks` and 中文", notes)
            self.assertNotIn("old entry", notes)
            self.assertFalse((root / "SHOULD_NOT_EXIST").exists())


if __name__ == "__main__":
    unittest.main()
