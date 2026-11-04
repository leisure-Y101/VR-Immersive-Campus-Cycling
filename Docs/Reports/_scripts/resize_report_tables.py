# -*- coding: utf-8 -*-
from pathlib import Path
from docx import Document
from docx.shared import Cm

p = Path(r"D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29.docx")
doc = Document(p)

layout = {
    ("层级", "结论", "证据 / 影响"): [2.2, 5.0, 8.8],
    ("项目", "实测值", "说明"): [2.4, 5.2, 8.4],
    ("优先级", "类别", "动作", "验收标准"): [1.5, 2.3, 7.0, 5.2],
    ("项目", "结果", "证据"): [2.4, 6.2, 7.4],
}

for table in doc.tables:
    headers = tuple(c.text.strip() for c in table.rows[0].cells)
    widths = layout.get(headers)
    if not widths:
        continue
    table.autofit = False
    for col, width in zip(table.columns, widths):
        col.width = Cm(width)
    for row in table.rows:
        for cell, width in zip(row.cells, widths):
            cell.width = Cm(width)

doc.save(p)
print("resized")
