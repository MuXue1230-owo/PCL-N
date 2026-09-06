"""Native packaging on the matching CI runner. No source or shell strings are evaluated."""
import argparse
import os
import plistlib
import shutil
import subprocess
import tarfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run(*args, **kwargs):
    subprocess.run([str(arg) for arg in args], check=True, **kwargs)


def archive(source, output, name):
    if output.suffix == ".zip":
        shutil.make_archive(str(output.with_suffix("")), "zip", source)
    else:
        with tarfile.open(output, "w:gz") as tar:
            tar.add(source, arcname=name)


def windows(payload, output, work, base, version, prefix, arch):
    iscc = shutil.which("ISCC") or r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    icon = ROOT / "PCL.Desktop/Assets/icon.ico"
    run(iscc, f"/DPayload={payload}", f"/DProductVersion={version}", f"/DNumericVersion={prefix}",
        f"/DInstallArch={'arm64' if arch == 'arm64' else 'x64compatible'}", f"/DOutputDir={output}",
        f"/DOutputName={base}.setup", f"/DIconPath={icon}", ROOT / "eng/release/windows.iss")
    run("wix", "build", ROOT / "eng/release/windows.wxs", "-arch", arch,
        "-d", f"Payload={payload}", "-d", f"NumericVersion={prefix}", "-d", f"IconPath={icon}",
        "-sice:ICE38", "-sice:ICE64", "-sice:ICE91",
        "-o", work / "launcher.msi")
    shutil.copy2(work / "launcher.msi", output / f"{base}.msi")
    archive(payload, output / f"{base}.portable.zip", "PCL Nexa")


def macos(payload, output, work, base, version, prefix, arch):
    app = work / "PCL Nexa.app"
    contents = app / "Contents"
    shutil.copytree(payload, contents / "MacOS")
    resources = contents / "Resources"
    resources.mkdir()
    iconset = work / "Launcher.iconset"
    iconset.mkdir()
    for size in (16, 32, 128, 256, 512):
        for scale in (1, 2):
            suffix = "@2x" if scale == 2 else ""
            run("sips", "-z", size * scale, size * scale, ROOT / "PCL.Desktop/Assets/icon.png",
                "--out", iconset / f"icon_{size}x{size}{suffix}.png", stdout=subprocess.DEVNULL)
    run("iconutil", "-c", "icns", iconset, "-o", resources / "Launcher.icns")
    with (contents / "Info.plist").open("wb") as stream:
        plistlib.dump(dict(CFBundleName="PCL Nexa", CFBundleDisplayName="PCL Nexa", CFBundleIdentifier="org.pcln.nexa",
                          CFBundleExecutable="PCL.Desktop", CFBundlePackageType="APPL", CFBundleIconFile="Launcher.icns",
                          CFBundleShortVersionString=prefix, CFBundleVersion=prefix, PCLProductVersion=version,
                          NSHighResolutionCapable=True, LSMinimumSystemVersion="12.0"), stream)
    # Ad-hoc signing seals the complete bundle, including all NativeAOT/Skia libraries.
    run("codesign", "--force", "--deep", "--sign", "-", app)
    run("codesign", "--verify", "--deep", "--strict", app)
    run(contents / "MacOS/PCL.Desktop", "--validate-shell")
    dmg_source = work / "dmg"
    dmg_source.mkdir()
    shutil.copytree(app, dmg_source / app.name)
    (dmg_source / "Applications").symlink_to("/Applications", target_is_directory=True)
    run("hdiutil", "create", "-volname", "PCL Nexa", "-srcfolder", dmg_source, "-format", "UDZO", output / f"{base}.dmg")
    run("hdiutil", "verify", output / f"{base}.dmg")
    archive(app, output / f"{base}.portable.tar.gz", app.name)


def linux(payload, output, work, base, version, prefix, arch):
    appdir = work / "PCL-Nexa.AppDir"
    shutil.copytree(payload, appdir / "usr/lib/pcl-nexa")
    binary = appdir / "usr/lib/pcl-nexa/PCL.Desktop"
    binary.chmod(0o755)
    desktop = "[Desktop Entry]\nType=Application\nName=PCL Nexa\nExec=pcl-nexa %U\nIcon=pcl-nexa\nTerminal=false\nCategories=Game;\nStartupWMClass=PCL.Desktop\n"
    launcher = appdir / "usr/bin/pcl-nexa"
    launcher.parent.mkdir(parents=True)
    launcher.write_text('#!/bin/sh\nexec "$(dirname "$(readlink -f "$0")")/../lib/pcl-nexa/PCL.Desktop" "$@"\n', encoding="utf-8")
    launcher.chmod(0o755)
    for relative in ("pcl-nexa.desktop", "usr/share/applications/pcl-nexa.desktop"):
        path = appdir / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(desktop, encoding="utf-8")
    for relative in ("pcl-nexa.png", "usr/share/icons/hicolor/256x256/apps/pcl-nexa.png"):
        path = appdir / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / "PCL.Desktop/Assets/icon.png", path)
    (appdir / "AppRun").write_text('#!/bin/sh\nHERE="$(dirname "$(readlink -f "$0")")"\nexec "$HERE/usr/bin/pcl-nexa" "$@"\n', encoding="utf-8")
    (appdir / "AppRun").chmod(0o755)
    run("desktop-file-validate", appdir / "pcl-nexa.desktop")
    for package_type, package_arch in (("deb", "arm64" if arch == "arm64" else "amd64"), ("rpm", "aarch64" if arch == "arm64" else "x86_64")):
        native_version = version.replace(".alpha.", "~alpha.").replace(".beta.", "~beta.").replace(".ci.", "~ci.")
        dependencies = ("libc6", "libgcc-s1", "libstdc++6", "zlib1g", "libfontconfig1", "libx11-6", "libice6", "libsm6") if package_type == "deb" else ("glibc", "libgcc", "libstdc++", "zlib", "fontconfig", "libX11", "libICE", "libSM")
        options = [item for dependency in dependencies for item in ("--depends", dependency)]
        run("fpm", "-s", "dir", "-t", package_type, "-n", "pcl-nexa", "-v", native_version, "--iteration", "1",
            "-a", package_arch, "--maintainer", "PCL N", "--description", "PCL Nexa Minecraft Launcher",
            "--url", "https://github.com/PCL-N-Edition/PCL-N", "--license", "Apache-2.0", *options,
            "-C", appdir, "-p", output / f"{base}.{package_type}", "usr")
    environment = dict(os.environ, ARCH="aarch64" if arch == "arm64" else "x86_64", APPIMAGE_EXTRACT_AND_RUN="1")
    run(os.environ["APPIMAGETOOL"], appdir, output / f"{base}.AppImage", env=environment)
    archive(payload, output / f"{base}.portable.tar.gz", "PCL-Nexa")
    run("dpkg-deb", "--info", output / f"{base}.deb")
    run("rpm", "-qip", output / f"{base}.rpm")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--payload", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--rid", required=True)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    from metadata import identity
    data = identity("refs/tags/" + args.version, args.version.rsplit(".", 1)[-1].ljust(40, "0") if ".ci." in args.version else "0" * 40)
    platform, arch = args.rid.split("-")
    if platform not in ("win", "linux", "osx") or arch not in ("x64", "arm64"):
        raise ValueError("Unsupported RID")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    work = output.parent / f"package-{args.rid}"
    work.mkdir()  # A fresh staging directory prevents stale files entering packages.
    globals()[{"win": "windows", "osx": "macos", "linux": "linux"}[platform]](
        args.payload.resolve(), output, work, f"PCL-Nexa-{args.version}-{args.rid}", args.version, data["prefix"], arch)


if __name__ == "__main__":
    main()
