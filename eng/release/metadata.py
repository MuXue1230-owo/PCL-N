"""Derive release identity from Git refs; commit text is always treated as data."""
import argparse
import json
import os
import re
import subprocess
from pathlib import Path

TAG = re.compile(r"v?((0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*))(?:\.(alpha|beta)\.([1-9]\d*)|\.ci\.([0-9a-f]{6}))?$")


def identity(ref, sha):
    if not re.fullmatch(r"[0-9a-f]{40,64}", sha):
        raise ValueError("Expected a full Git commit hash")
    if not ref.startswith("refs/tags/"):
        return dict(version=f"2.0.0.ci.{sha[:6]}", prefix="2.0.0", channel="ci", sequence="1", commit=sha[:6], release="false")
    match = TAG.fullmatch(ref.removeprefix("refs/tags/"))
    if not match:
        raise ValueError("Tag must use the canonical dotted XSR version")
    prefix, _, _, _, channel, sequence, commit = match.groups()
    if commit and commit != sha[:6]:
        raise ValueError("CI tag suffix must match the tagged commit")
    return dict(version=ref.removeprefix("refs/tags/").removeprefix("v"), prefix=prefix,
                channel=channel or ("ci" if commit else "stable"), sequence=sequence or "1",
                commit=sha[:6], release="true")


def git(*args):
    return subprocess.check_output(["git", *args], encoding="utf-8").strip()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    parser.add_argument("--sha", default=os.environ.get("GITHUB_SHA", ""))
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sha = git("rev-parse", f"{args.sha}^{{commit}}")
    data = identity(args.ref, sha)
    previous = next((tag for tag in git("tag", "--merged", sha, "--sort=-creatordate").splitlines()
                     if TAG.fullmatch(tag) and git("rev-parse", f"{tag}^{{commit}}") != sha), None)
    revision = f"{previous}..{sha}" if previous else sha
    messages = git("log", "--format=%h%x09%B%x00", revision).split("\0")
    notes = f"# PCL Nexa {data['version']}\n\n"
    for message in messages:
        if not message.strip():
            continue
        commit, body = message.strip().split("\t", 1)
        lines = body.strip().splitlines()
        notes += f"- {lines[0]} (`{commit}`)\n"
        if len(lines) > 1:
            notes += "\n" + "\n".join("  " + line for line in lines[1:]) + "\n"
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "metadata.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
    (args.output / "CHANGELOG.md").write_text(notes, encoding="utf-8")
    if output := os.environ.get("GITHUB_OUTPUT"):
        with open(output, "a", encoding="utf-8") as stream:
            for key, value in data.items():
                stream.write(f"{key}={value}\n")


if __name__ == "__main__":
    main()
