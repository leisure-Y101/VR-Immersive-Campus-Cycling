"""把本目录的 Markdown 转成 Word（支持标题/表格/列表/引用/代码块/粗体/行内代码）。
用法: python _md2docx.py <input.md> <output.docx>
"""
import re
import sys

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

FONT = "微软雅黑"


def set_east_asia(run):
    rpr = run._element.get_or_add_rPr()
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = rpr.makeelement(qn("w:rFonts"), {})
        rpr.append(rfonts)
    rfonts.set(qn("w:eastAsia"), FONT)
    rfonts.set(qn("w:ascii"), FONT)
    rfonts.set(qn("w:hAnsi"), FONT)


def add_runs(par, text, base_bold=False):
    """解析 **粗体** 与 `代码`，其余按普通文本处理。"""
    for chunk in re.split(r"(\*\*.+?\*\*|`.+?`)", text):
        if not chunk:
            continue
        if chunk.startswith("**") and chunk.endswith("**"):
            run = par.add_run(chunk[2:-2])
            run.bold = True
        elif chunk.startswith("`") and chunk.endswith("`"):
            run = par.add_run(chunk[1:-1])
            run.font.name = "Consolas"
            run.font.size = Pt(9)
            run.font.color.rgb = RGBColor(0xB0, 0x30, 0x60)
        else:
            run = par.add_run(chunk)
            if base_bold:
                run.bold = True
        set_east_asia(run)


def format_table(table):
    """表头跨页重复、行内不跨页、固定布局更整齐。"""
    tbl = table._tbl
    tblPr = tbl.tblPr
    for tag in ("w:tblLayout",):
        for el in tblPr.findall(qn(tag)):
            tblPr.remove(el)
    layout = tblPr.makeelement(qn("w:tblLayout"), {qn("w:type"): "autofit"})
    tblPr.append(layout)
    for ri, row in enumerate(table.rows):
        trPr = row._tr.get_or_add_trPr()
        cant = trPr.makeelement(qn("w:cantSplit"), {})
        trPr.append(cant)
        if ri == 0:
            hdr = trPr.makeelement(qn("w:tblHeader"), {})
            trPr.append(hdr)


def main(src, dst):
    with open(src, encoding="utf-8") as f:
        lines = f.read().splitlines()

    doc = Document()
    for section in doc.sections:
        section.top_margin = Cm(1.8)
        section.bottom_margin = Cm(1.8)
        section.left_margin = Cm(1.6)
        section.right_margin = Cm(1.6)
    normal = doc.styles["Normal"]
    normal.font.name = FONT
    normal.font.size = Pt(10.5)
    normal.element.rPr.rFonts.set(qn("w:eastAsia"), FONT)

    for i in (1, 2, 3):
        st = doc.styles[f"Heading {i}"]
        st.font.name = FONT
        st.font.color.rgb = RGBColor(0x1F, 0x38, 0x64)
        st.element.rPr.rFonts.set(qn("w:eastAsia"), FONT)

    i = 0
    n = len(lines)
    while i < n:
        line = lines[i].rstrip()
        stripped = line.strip()

        if not stripped:
            i += 1
            continue

        # 分隔线
        if re.fullmatch(r"-{3,}", stripped):
            i += 1
            continue

        # 代码块
        if stripped.startswith("```"):
            i += 1
            buf = []
            while i < n and not lines[i].strip().startswith("```"):
                buf.append(lines[i])
                i += 1
            i += 1
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Pt(12)
            run = p.add_run("\n".join(buf))
            run.font.name = "Consolas"
            run.font.size = Pt(8.5)
            continue

        # 表格
        if stripped.startswith("|"):
            rows = []
            while i < n and lines[i].strip().startswith("|"):
                raw = lines[i].strip().strip("|")
                cells = [c.strip() for c in raw.split("|")]
                if not re.fullmatch(r"[:\-\s|]+", lines[i].strip()):
                    rows.append(cells)
                i += 1
            if rows:
                cols = max(len(r) for r in rows)
                table = doc.add_table(rows=0, cols=cols)
                table.style = "Table Grid"
                table.alignment = WD_TABLE_ALIGNMENT.CENTER
                for ri, cells in enumerate(rows):
                    row = table.add_row()
                    for ci in range(cols):
                        cell = row.cells[ci]
                        text = cells[ci] if ci < len(cells) else ""
                        par = cell.paragraphs[0]
                        par.paragraph_format.space_before = Pt(1)
                        par.paragraph_format.space_after = Pt(1)
                        add_runs(par, text, base_bold=(ri == 0))
                        for run in par.runs:
                            run.font.size = Pt(8)
                format_table(table)
                doc.add_paragraph()
            continue

        # 标题
        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            level = len(m.group(1))
            text = m.group(2).strip()
            if level == 1:
                p = doc.add_heading(level=0)
            else:
                p = doc.add_heading(level=min(level - 1, 3))
            add_runs(p, text)
            p.paragraph_format.keep_with_next = True
            for run in p.runs:
                run.font.name = FONT
            i += 1
            continue

        # 引用
        if stripped.startswith(">"):
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Pt(14)
            add_runs(p, stripped.lstrip("> ").strip())
            for run in p.runs:
                run.italic = True
                run.font.color.rgb = RGBColor(0x60, 0x60, 0x60)
            i += 1
            continue

        # 列表
        m = re.match(r"^([-*])\s+(.*)$", stripped)
        if m:
            p = doc.add_paragraph(style="List Bullet")
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(3)
            add_runs(p, m.group(2))
            i += 1
            continue
        m = re.match(r"^(\d+)[.)]\s+(.*)$", stripped)
        if m:
            # 手写序号，避免 Word 自动编号跨列表续号
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Pt(18)
            p.paragraph_format.first_line_indent = Pt(-18)
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(3)
            add_runs(p, f"{m.group(1)}. {m.group(2)}")
            i += 1
            continue

        # 普通段落
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4)
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        add_runs(p, stripped)
        i += 1

    doc.save(dst)
    print(f"saved: {dst}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
