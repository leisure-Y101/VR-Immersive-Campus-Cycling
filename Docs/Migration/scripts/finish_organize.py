# -*- coding: utf-8 -*-
from pathlib import Path
import shutil

ART = Path(r"D:\freebikeapk")
PRJ = Path(r"D:\freebike")
DOCS = PRJ / "Docs"
RELEASES = PRJ / "Releases"
TOOLS = PRJ / "Tools"

for p in [
    DOCS / "AI", DOCS / "WorkLogs", DOCS / "Handoff", DOCS / "Reports",
    DOCS / "Reports" / "archive", DOCS / "Resources", DOCS / "Logs",
    DOCS / "Migration" / "scripts", DOCS / "RuntimeReports",
    RELEASES / "Android", RELEASES / "Windows", RELEASES / "Benchmark",
    PRJ / "Media" / "Video", PRJ / "Media" / "Stills",
    TOOLS / "A1", TOOLS / "deps", TOOLS / "ffmpeg", TOOLS / "compilecheck",
]:
    p.mkdir(parents=True, exist_ok=True)


def move_into(src: Path, dst_dir: Path):
    """Move src into an existing directory, merging if necessary."""
    if not src.exists():
        print("MISSING", src)
        return
    dst_dir.mkdir(parents=True, exist_ok=True)
    target = dst_dir / src.name
    if target.exists():
        if target.is_dir() and src.is_dir() and not any(target.iterdir()):
            target.rmdir()
        elif target.is_dir() and src.is_dir():
            for child in list(src.iterdir()):
                move_into(child, target)
            src.rmdir()
            return
        else:
            raise RuntimeError(f"target exists: {target}")
    shutil.move(str(src), str(target))
    print("MOVED", src, "->", target)


def move_exact(src: Path, dst: Path):
    if not src.exists():
        print("MISSING", src)
        return
    if dst.exists():
        if dst.is_dir() and src.is_dir() and not any(dst.iterdir()):
            dst.rmdir()
        else:
            raise RuntimeError(f"target exists: {dst}")
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(src), str(dst))
    print("MOVED", src, "->", dst)


# Documents / reports
move_into(ART / "AI文档", DOCS / "AI")
move_into(ART / "工作日志", DOCS / "WorkLogs")
move_into(ART / "交付", DOCS / "Handoff")
move_into(ART / "PicoLocalVideoMigration", DOCS / "Resources")

# Latest report from Desktop, plus old copy in artifacts root.
desktop_report = Path(r"D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29.docx")
if desktop_report.exists():
    shutil.copy2(desktop_report, DOCS / "Reports" / "FreeBike_工作报告_2026-10-29.docx")
    print("COPIED latest report")
else:
    print("MISSING desktop report", desktop_report)

old_report = ART / "FreeBike_工作报告_2026-10-29.docx"
if old_report.exists():
    move_exact(old_report, DOCS / "Reports" / "archive" / "FreeBike_工作报告_2026-10-29_freebikeapk.docx")

# Latest Android APKs
ver = ART / "版本更新"
for name in ("testbike1.9.apk", "testbike1.8.apk"):
    src = ver / name
    if src.exists():
        move_exact(src, RELEASES / "Android" / name)

# Launch scripts
for name in ("启动骑行台桥接.cmd", "启动Demo_等待SteamVR.cmd"):
    src = ART / name
    if src.exists():
        move_exact(src, TOOLS / "A1" / name)

# Migration script
src = ART / "organize_freebike.ps1"
if src.exists():
    move_exact(src, DOCS / "Migration" / "scripts" / "organize_freebike.ps1")

# Delete old root test videos and snapshots.
old_videos = [
    ART / "nighttest360vidio_before_daytime.mp4",
    ART / "nighttest360vidio_original_hevc.mp4",
    ART / "nighttest360_4k_h264.mp4",
]
for p in old_videos:
    if p.exists():
        p.unlink()
        print("REMOVED", p)

old_files = [
    "CampusRideApp.cs", "CampusRideApp.cs.meta",
    "PanoBikeSpeed.cs", "PanoBikeSpeed.cs.meta",
    "localization_block.cs", "PanoValidate.cs",
    "PanoVideoController.cs", "PanoVideoController.cs.meta",
    "RideHud.cs", "RideHud.cs.meta",
    "PicoVideoBenchmark.unity",
    "PanoMat.mat.bak", "PanoMat.mat.fixed",
    "panoTest.unity.app.fixed", "panoTest.unity.bak",
    "panoTest.unity.before_app.bak", "panoTest.unity.before_bikespeed.bak",
    "panoTest.unity.before_centering.bak", "panoTest.unity.before_cockpit_visible.bak",
    "panoTest.unity.before_hide_bad_bike.bak", "panoTest.unity.before_hud.bak",
    "panoTest.unity.before_remove_hud.bak", "panoTest.unity.bikespeed.fixed",
    "panoTest.unity.centered.fixed", "panoTest.unity.cockpit_visible.fixed",
    "panoTest.unity.fixed", "panoTest.unity.hud.fixed",
    "panoTest.unity.no_bad_bike.fixed", "panoTest.unity.no_hud.fixed",
    "add_app_scene.py", "convert_bicycle_obj.py", "corrupt_lines.txt",
    "download_fulgaz_shots.js", "download_more_ui_shots.js",
    "download_poly_bicycle.js", "fix_bikespeed_scene.py", "fix_hud_scene.py",
    "fix_langnames.py", "fix_pano_scene.py", "fix_view_cockpit_scene.py",
    "fix_view_speed_scene.py", "free3d_probe.js", "hide_bad_bike.py",
    "inspect_bike_mesh.py", "inspect_mp4.py", "itunes_search_apps.js",
    "itunes_ui_shots.js", "make_ui_reference_board.py",
    "poly_probe.js", "probe_ui_images.js", "remove_hud_scene.py",
    "tutorial_docx_text.txt", "_docx_dump.txt",
]
for name in old_files:
    p = ART / name
    if p.exists():
        p.unlink()
        print("REMOVED", p)

# Remove empty old dirs.
for name in ("版本更新", "tmp", ".acl-recovery", ".tmp-pip"):
    p = ART / name
    if p.exists():
        shutil.rmtree(p)
        print("REMOVED DIR", p)

# Move the remaining staging files into the repo root.
for name in ("README_github.md", ".gitignore", ".gitattributes", "Media_README.md", "Releases_README.md"):
    src = ART / name
    if not src.exists():
        continue
    target = PRJ / ("README.md" if name == "README_github.md" else
                    ("Media/README.md" if name == "Media_README.md" else
                     ("Releases/README.md" if name == "Releases_README.md" else name)))
    if target.exists() and target.is_file():
        target.unlink()
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(src), str(target))
    print("MOVED", src, "->", target)

# Update launcher path for the moved demo build.
launcher = TOOLS / "A1" / "启动Demo_等待SteamVR.cmd"
if launcher.exists():
    text = launcher.read_text(encoding="utf-8")
    text = text.replace(r"D:\freebikeapk\demo\freebike.exe", r"D:\freebike\Releases\Windows\demo_2026-10-27\freebike.exe")
    launcher.write_text(text, encoding="utf-8")
    print("UPDATED launcher")

# Final targeted cleanup of known junk in the old artifacts root.
for name in ("README_github.md", ".gitignore", ".gitattributes", "Media_README.md", "Releases_README.md"):
    p = ART / name
    if p.exists():
        p.unlink()

print("DONE")
