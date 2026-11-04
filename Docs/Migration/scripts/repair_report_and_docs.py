# -*- coding: utf-8 -*-
from pathlib import Path
import shutil

root = Path(r"D:\freebike")
docs = root / "Docs"

# Flatten nested Chinese folders created by the first pass.
for parent_name, nested_name in [
    ("AI", "AI文档"),
    ("WorkLogs", "工作日志"),
    ("Handoff", "交付"),
]:
    parent = docs / parent_name
    nested = parent / nested_name
    if nested.exists() and nested.is_dir():
        for child in list(nested.iterdir()):
            target = parent / child.name
            if target.exists():
                raise RuntimeError(f"target exists: {target}")
            shutil.move(str(child), str(target))
        nested.rmdir()
        print("flattened", nested, "->", parent)

# Rebuild latest report from the 58KB source, then run the addendum script.
source = docs / "Reports" / "archive" / "FreeBike_工作报告_2026-10-29_freebikeapk.docx"
target = docs / "Reports" / "FreeBike_工作报告_2026-10-29.docx"
if not source.exists():
    source = docs / "WorkLogs" / "FreeBike_工作报告_2026-10-29.docx"
if not source.exists():
    raise SystemExit("report source not found")
shutil.copy2(source, target)
print("copied base report ->", target, target.stat().st_size)

# Adapt and run the existing addendum script.
script = docs / "Reports" / "_scripts" / "edit_report_addendum.py"
text = script.read_text(encoding="utf-8")
text = text.replace(
    r'DOC = Path(r"D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29.docx")',
    r'DOC = Path(r"D:\freebike\Docs\Reports\FreeBike_工作报告_2026-10-29.docx")',
)
text = text.replace(
    r'BACKUP = Path(r"D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29_before_20261029_addendum.docx")',
    r'BACKUP = Path(r"D:\freebike\Docs\Reports\FreeBike_工作报告_2026-10-29_before_20261029_addendum.docx")',
)
script.write_text(text, encoding="utf-8")
print("adapted addendum script")
