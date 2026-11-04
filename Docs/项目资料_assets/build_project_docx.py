# -*- coding: utf-8 -*-
"""Build a polished Word handover document from 项目资料.md."""
from __future__ import annotations

import re
import sys
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_ALIGN_VERTICAL, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING, WD_TAB_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

ROOT = Path(r"D:\freebike")
MD_PATH = ROOT / "Docs" / "项目资料.md"
OUT_PATH = ROOT / "Docs" / "项目资料.docx"
ASSET_DIR = ROOT / "Docs" / "项目资料_assets"

FONT = "微软雅黑"
MONO = "Consolas"
NAVY = RGBColor(0x0B, 0x1F, 0x33)
BLUE = RGBColor(0x1D, 0x4E, 0xD8)
TEAL = RGBColor(0x0E, 0x74, 0x90)
MUTED = RGBColor(0x52, 0x67, 0x7A)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
CODE = RGBColor(0xB0, 0x30, 0x60)
PAGE_W = 17.0  # cm, A4 minus margins

P_ORDER = [
    "pStyle", "keepNext", "keepLines", "pageBreakBefore", "framePr", "widowControl",
    "numPr", "suppressLineNumbers", "pBdr", "shd", "tabs", "suppressAutoHyphens",
    "kinsoku", "wordWrap", "overflowPunct", "topLinePunct", "autoSpaceDE", "autoSpaceDN",
    "bidi", "adjustRightInd", "snapToGrid", "spacing", "ind", "contextualSpacing",
    "mirrorIndents", "suppressOverlap", "jc", "textDirection", "textAlignment",
    "textboxTightWrap", "outlineLvl", "divId", "cnfStyle", "rPr", "sectPr", "pPrChange",
]
TC_ORDER = [
    "cnfStyle", "tcW", "gridSpan", "hMerge", "vMerge", "tcBorders", "shd", "noWrap",
    "tcMar", "textDirection", "tcFitText", "vAlign", "hideMark", "headers", "cellIns",
    "cellDel", "cellMerge", "tcPrChange",
]
TBL_ORDER = [
    "tblStyle", "tblpPr", "tblOverlap", "bidiVisual", "tblStyleRowBandSize",
    "tblStyleColBandSize", "tblW", "jc", "tblCellSpacing", "tblInd", "tblBorders",
    "shd", "tblLayout", "tblCellMar", "tblLook", "tblCaption", "tblDescription", "tblPrChange",
]
TR_ORDER = [
    "cnfStyle", "divId", "gridBefore", "gridAfter", "wBefore", "wAfter", "cantSplit",
    "trHeight", "tblHeader", "tblCellSpacing", "jc", "hidden", "ins", "del", "trPrChange",
]


def _local(tag):
    return tag.split("}")[-1] if "}" in tag else tag


def insert_ordered(parent, child, order):
    """Insert child at the schema-correct position; replace any existing same-tag element."""
    for old in list(parent):
        if old.tag == child.tag:
            parent.remove(old)
    idx = order.index(_local(child.tag))
    for existing in list(parent):
        if existing.tag in [qn("w:" + x) for x in order]:
            if order.index(_local(existing.tag)) > idx:
                existing.addprevious(child)
                return
    parent.append(child)


def set_font(run, name=FONT, size=10.5, bold=None, color=None):
    run.font.name = name
    run.font.size = Pt(size)
    if bold is not None:
        run.bold = bold
    if color is not None:
        run.font.color.rgb = color
    rpr = run._element.get_or_add_rPr()
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = OxmlElement("w:rFonts")
        rpr.append(rfonts)
    rfonts.set(qn("w:eastAsia"), name)
    rfonts.set(qn("w:ascii"), name)
    rfonts.set(qn("w:hAnsi"), name)


def shade_paragraph(paragraph, fill):
    ppr = paragraph._p.get_or_add_pPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), fill)
    insert_ordered(ppr, shd, P_ORDER)


def border_paragraph(paragraph, color="B8C9D6", sz="4", sides=("bottom",)):
    ppr = paragraph._p.get_or_add_pPr()
    pbdr = OxmlElement("w:pBdr")
    for side in ("top", "left", "bottom", "right"):
        if side not in sides:
            continue
        el = OxmlElement(f"w:{side}")
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), sz)
        el.set(qn("w:space"), "3")
        el.set(qn("w:color"), color)
        pbdr.append(el)
    insert_ordered(ppr, pbdr, P_ORDER)


def shade_cell(cell, fill):
    tcpr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), fill)
    insert_ordered(tcpr, shd, TC_ORDER)


def cell_margins(cell, top=0.06, bottom=0.06, left=0.12, right=0.12):
    tcpr = cell._tc.get_or_add_tcPr()
    mar = OxmlElement("w:tcMar")
    for side, val in (("top", top), ("left", left), ("bottom", bottom), ("right", right)):
        el = OxmlElement(f"w:{side}")
        el.set(qn("w:w"), str(int(val * 567)))
        el.set(qn("w:type"), "dxa")
        mar.append(el)
    insert_ordered(tcpr, mar, TC_ORDER)


def set_cell_borders(cell, color="D7E2EA", sz="4", left_only=False):
    tcpr = cell._tc.get_or_add_tcPr()
    borders = OxmlElement("w:tcBorders")
    for side in ("top", "left", "bottom", "right"):
        el = OxmlElement(f"w:{side}")
        if left_only and side != "left":
            el.set(qn("w:val"), "nil")
        else:
            el.set(qn("w:val"), "single")
            el.set(qn("w:sz"), "24" if left_only and side == "left" else sz)
            el.set(qn("w:space"), "0")
            el.set(qn("w:color"), "0E7490" if left_only and side == "left" else color)
        borders.append(el)
    insert_ordered(tcpr, borders, TC_ORDER)


def set_table_borders(table, color="D7E2EA", sz="4"):
    tblpr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement(f"w:{side}")
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), sz)
        el.set(qn("w:space"), "0")
        el.set(qn("w:color"), color)
        borders.append(el)
    insert_ordered(tblpr, borders, TBL_ORDER)


def repeat_header(row):
    trpr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:tblHeader")
    insert_ordered(trpr, el, TR_ORDER)


def cant_split(row):
    trpr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:cantSplit")
    insert_ordered(trpr, el, TR_ORDER)


def add_inline(paragraph, text, size=10.5, color=None, bold_base=False):
    parts = re.split(r"(\*\*.+?\*\*|`[^`]+`)", text)
    for part in parts:
        if not part:
            continue
        if part.startswith("**") and part.endswith("**") and len(part) > 4:
            run = paragraph.add_run(part[2:-2])
            set_font(run, FONT, size, True, color)
        elif part.startswith("`") and part.endswith("`") and len(part) > 2:
            run = paragraph.add_run(part[1:-1])
            set_font(run, MONO, size - 0.8, bold_base, CODE)
        else:
            run = paragraph.add_run(part)
            set_font(run, FONT, size, bold_base, color)


def add_page_field(paragraph, instr):
    """Insert a complex Word field (PAGE / NUMPAGES) as valid runs."""
    def run_with(child):
        run = OxmlElement("w:r")
        run.append(child)
        paragraph._p.append(run)

    begin = OxmlElement("w:fldChar")
    begin.set(qn("w:fldCharType"), "begin")
    run_with(begin)

    instr_el = OxmlElement("w:instrText")
    instr_el.set(qn("xml:space"), "preserve")
    instr_el.text = f" {instr} "
    run_with(instr_el)

    sep = OxmlElement("w:fldChar")
    sep.set(qn("w:fldCharType"), "separate")
    run_with(sep)

    text_run = OxmlElement("w:r")
    t = OxmlElement("w:t")
    t.text = "1"
    text_run.append(t)
    paragraph._p.append(text_run)

    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    run_with(end)


def add_cover(doc):
    # top spacer
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(18)

    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    table.columns[0].width = Cm(PAGE_W)
    cell = table.cell(0, 0)
    cell.width = Cm(PAGE_W)
    shade_cell(cell, "051421")
    set_cell_borders(cell, color="051421", sz="4")
    cell_margins(cell, top=0.8, bottom=0.8, left=0.8, right=0.8)
    cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER

    p = cell.paragraphs[0]
    p.paragraph_format.space_after = Pt(6)
    run = p.add_run("项目资料")
    set_font(run, FONT, 34, True, WHITE)

    p = cell.add_paragraph()
    p.paragraph_format.space_after = Pt(10)
    run = p.add_run("FreeBike 校园 360° 沉浸式骑行 Demo")
    set_font(run, FONT, 16, True, RGBColor(0x22, 0xD3, 0xEE))

    p = cell.add_paragraph()
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run("新队友交接手册")
    set_font(run, FONT, 13, False, RGBColor(0xEE, 0xF7, 0xFC))

    p = cell.add_paragraph()
    p.paragraph_format.space_after = Pt(0)
    run = p.add_run("2026-11-03 · v1.0（交接版）")
    set_font(run, FONT, 10.5, False, RGBColor(0x96, 0xBE, 0xC9))

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(18)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run("文档信息")
    set_font(run, FONT, 12, True, NAVY)
    border_paragraph(p, color="22D3EE", sz="10", sides=("bottom",))

    meta = [
        ("文档版本", "v1.0（交接版）"),
        ("适用工程", r"D:\freebike"),
        ("当前主线", "PC VR：panoTest + CampusRideApp"),
        ("目标头显", "PICO 4 Pro（A8110）"),
        ("资料截止", "2026-11-03"),
        ("维护者", "FreeBike 项目组"),
        ("文档定位", "项目说明、环境手册、演示 SOP、排障入口"),
    ]
    t = doc.add_table(rows=len(meta), cols=2)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    t.columns[0].width = Cm(3.6)
    t.columns[1].width = Cm(13.4)
    set_table_borders(t, color="D7E2EA", sz="3")
    for i, (k, v) in enumerate(meta):
        row = t.rows[i]
        cant_split(row)
        c0, c1 = row.cells
        c0.width = Cm(3.6)
        c1.width = Cm(13.4)
        shade_cell(c0, "F2F8FA")
        cell_margins(c0)
        cell_margins(c1)
        p0 = c0.paragraphs[0]
        p0.paragraph_format.space_after = Pt(0)
        r = p0.add_run(k)
        set_font(r, FONT, 9.5, True, MUTED)
        p1 = c1.paragraphs[0]
        p1.paragraph_format.space_after = Pt(0)
        add_inline(p1, v, 9.5, NAVY)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(4)
    add_inline(p, "本资料回答四个问题：**这个项目现在是什么、已经实现了什么、需要什么软硬件、接下来怎么继续。**", 10.5, NAVY)
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(4)
    add_inline(p, "它不是 AI 续作文档；AI 续作请读 Docs/AI/FreeBike_项目交接文档.md。两者冲突时，以当前代码和本资料的“现状”章节为准。", 9.5, MUTED)

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p.paragraph_format.space_before = Pt(24)
    run = p.add_run("FreeBike 项目组 · 2026-11-03")
    set_font(run, FONT, 9, False, MUTED)


def add_nav(doc):
    doc.add_page_break()
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(4)
    run = p.add_run("阅读导航")
    set_font(run, FONT, 22, True, NAVY)
    border_paragraph(p, color="22D3EE", sz="10", sides=("bottom",))

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(8)
    add_inline(p, "这份手册按“先看现状，再搭环境，最后上手”的顺序组织；赶时间可以直接读 §0、§9、§13。", 9.5, MUTED)

    nav = [
        ("§0", "30 秒速览", "先知道现在能演示什么、还缺什么。"),
        ("§1–§2", "项目定位与系统总览", "三条链路、当前三态流程、文档冲突。"),
        ("§3–§4", "硬件与软件", "设备、外设、版本、安装路径、不要装什么。"),
        ("§5–§6", "工程结构与已实现功能", "目录、关键文件、8K 主页、6K 视频、A1 联动、暂停。"),
        ("§7", "旧分支与预留能力", "旧 APK、Kinect、转向传感器、3D 路口方案。"),
        ("§8–§10", "环境 / 演示 / 构建", "从零装机、启动顺序、Windows/Android 打包。"),
        ("§11–§12", "测试与排障", "验收项、日志关键字、常见故障表。"),
        ("§13–§15", "风险 / 路线图 / 交接", "P0/P1、下一步、交接清单与安全检查。"),
        ("§16–§18", "速查 / 术语 / 变更", "路径、命令、端口、参数、术语表。"),
    ]
    t = doc.add_table(rows=len(nav) + 1, cols=3)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    widths = [2.0, 5.2, 9.8]
    set_table_borders(t, color="D7E2EA", sz="4")
    for i, w in enumerate(widths):
        t.columns[i].width = Cm(w)
    headers = ("章节", "你会得到什么", "说明")
    for j, h in enumerate(headers):
        c = t.rows[0].cells[j]
        c.width = Cm(widths[j])
        shade_cell(c, "0B3A53")
        cell_margins(c)
        p = c.paragraphs[0]
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(h)
        set_font(r, FONT, 9, True, WHITE)
    for i, (sec, title, desc) in enumerate(nav, 1):
        row = t.rows[i]
        cant_split(row)
        for j, val in enumerate((sec, title, desc)):
            c = row.cells[j]
            c.width = Cm(widths[j])
            cell_margins(c)
            if i % 2 == 0:
                shade_cell(c, "F5FAFC")
            p = c.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            r = p.add_run(val)
            set_font(r, FONT, 9, j == 0, TEAL if j == 0 else NAVY)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run("约定")
    set_font(run, FONT, 10, True, NAVY)
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(2)
    add_inline(p, "P0 = 不解决不能稳定演示；P1 = 影响交付/维护；P2 = 后续优化。代码引用使用 `等宽字体`，路径均为本机实际路径。", 9.2, MUTED)
    p = doc.add_paragraph()
    add_inline(p, "如果只看一页：打印 §9.5 的口令卡；如果要接手开发：按 §8 → §11 → §13 的顺序走。", 9.2, MUTED)


def add_header_footer(doc):
    section = doc.sections[0]
    section.different_first_page_header_footer = True

    header = section.header
    p = header.paragraphs[0]
    p.paragraph_format.tab_stops.add_tab_stop(Cm(PAGE_W), WD_TAB_ALIGNMENT.RIGHT)
    r = p.add_run("FreeBike 项目资料")
    set_font(r, FONT, 8, False, MUTED)
    r = p.add_run("\t2026-11-03 · v1.0")
    set_font(r, FONT, 8, False, MUTED)
    border_paragraph(p, color="D7E2EA", sz="4", sides=("bottom",))

    footer = section.footer
    p = footer.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("第 ")
    set_font(r, FONT, 8, False, MUTED)
    add_page_field(p, "PAGE")
    r = p.add_run(" 页 / 共 ")
    set_font(r, FONT, 8, False, MUTED)
    add_page_field(p, "NUMPAGES")
    r = p.add_run(" 页")
    set_font(r, FONT, 8, False, MUTED)

    # blank first-page header/footer
    section.first_page_header.paragraphs[0].text = ""
    section.first_page_footer.paragraphs[0].text = ""


def add_heading(doc, text, level):
    if level == 1:
        p = doc.add_heading(level=1)
    elif level == 2:
        p = doc.add_heading(level=2)
    else:
        p = doc.add_heading(level=3)
    add_inline(p, text, 17 if level == 1 else (12.5 if level == 2 else 10.8),
               NAVY if level == 1 else (TEAL if level == 2 else NAVY), bold_base=True)
    pf = p.paragraph_format
    pf.keep_with_next = True
    if level == 1:
        pf.space_before = Pt(20)
        pf.space_after = Pt(10)
        border_paragraph(p, color="22D3EE", sz="8", sides=("bottom",))
    elif level == 2:
        pf.space_before = Pt(10)
        pf.space_after = Pt(4)
    else:
        pf.space_before = Pt(8)
        pf.space_after = Pt(3)
    return p


def add_table(doc, rows):
    if not rows:
        return
    ncols = max(len(r) for r in rows)
    rows = [r + [""] * (ncols - len(r)) for r in rows]
    font_size = 8.6 if ncols <= 4 else 7.9
    t = doc.add_table(rows=len(rows), cols=ncols)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = True
    set_table_borders(t, color="D7E2EA", sz="3")

    first_header = rows[0][0] if rows and rows[0] else ""
    if ncols == 2:
        widths = [4.2, 12.8]
    elif ncols == 3:
        if first_header in ("文件/字段", "文件", "检查项"):
            widths = [4.8, 6.2, 6.0]
        elif first_header == "能力":
            widths = [4.2, 2.6, 10.2]
        elif first_header == "参数":
            widths = [4.0, 4.0, 9.0]
        else:
            widths = [3.2, 6.9, 6.9]
    elif ncols == 4:
        widths = [2.8, 5.2, 5.2, 3.8]
    elif ncols == 5:
        if first_header == "软件":
            widths = [2.2, 3.0, 3.2, 1.5, 7.1]
        elif first_header == "路线":
            widths = [3.2, 4.6, 3.4, 2.4, 3.4]
        else:
            widths = [2.8, 4.4, 4.2, 2.8, 2.8]
    elif ncols == 6:
        widths = [1.4, 3.4, 4.4, 2.4, 3.0, 2.4]
    else:
        first = 2.4
        widths = [first] + [(PAGE_W - first) / (ncols - 1)] * (ncols - 1)

    for j, w in enumerate(widths):
        t.columns[j].width = Cm(w)

    for i, row in enumerate(rows):
        tr = t.rows[i]
        cant_split(tr)
        if i == 0:
            repeat_header(tr)
        for j, text in enumerate(row):
            c = tr.cells[j]
            c.width = Cm(widths[j])
            c.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            cell_margins(c)
            if i == 0:
                shade_cell(c, "0B3A53")
            elif i % 2 == 0:
                shade_cell(c, "F5FAFC")
            p = c.paragraphs[0]
            p.paragraph_format.space_before = Pt(1)
            p.paragraph_format.space_after = Pt(1)
            p.paragraph_format.line_spacing = 1.0
            add_inline(p, text, font_size, WHITE if i == 0 else NAVY, bold_base=(i == 0))
            if i == 0:
                p.paragraph_format.keep_with_next = True
    # small spacer after table
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(2)


def add_callout(doc, lines):
    text = "\n".join(lines)
    t = doc.add_table(rows=1, cols=1)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    t.columns[0].width = Cm(PAGE_W)
    c = t.cell(0, 0)
    c.width = Cm(PAGE_W)
    shade_cell(c, "EAF6F8")
    set_cell_borders(c, left_only=True)
    cell_margins(c, top=0.12, bottom=0.12, left=0.22, right=0.18)
    p = c.paragraphs[0]
    p.paragraph_format.space_after = Pt(2)
    add_inline(p, text, 9.4, NAVY)
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(2)


def add_code(doc, lines):
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Cm(0.3)
    p.paragraph_format.right_indent = Cm(0.2)
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(5)
    p.paragraph_format.line_spacing = 1.0
    shade_paragraph(p, "F4F6F8")
    border_paragraph(p, color="D7E2EA", sz="3", sides=("top", "left", "bottom", "right"))
    r = p.add_run("\n".join(lines))
    set_font(r, MONO, 8.4, False, RGBColor(0x1F, 0x29, 0x33))


def add_image(doc, path, alt):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.keep_with_next = True
    run = p.add_run()
    run.add_picture(str(path), width=Cm(16.3))
    cap = doc.add_paragraph()
    cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    cap.paragraph_format.space_after = Pt(6)
    r = cap.add_run(alt)
    set_font(r, FONT, 8.5, False, MUTED)


def add_list_item(doc, text, numbered=False, number=None):
    if text.startswith("[ ] "):
        text = "☐ " + text[4:]
    p = doc.add_paragraph(style="List Bullet" if not numbered else None)
    p.paragraph_format.left_indent = Cm(0.75)
    p.paragraph_format.first_line_indent = Cm(-0.38)
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(2.2)
    p.paragraph_format.line_spacing = 1.28
    if numbered and number is not None:
        add_inline(p, f"{number}. {text}", 10.2, NAVY)
    else:
        add_inline(p, text, 10.2, NAVY)


def parse_markdown(md_path: Path):
    lines = md_path.read_text(encoding="utf-8").splitlines()
    start = next(i for i, l in enumerate(lines) if l.startswith("## "))
    return lines[start:]


def build():
    body = parse_markdown(MD_PATH)
    doc = Document()
    section = doc.sections[0]
    section.page_width = Cm(21.0)
    section.page_height = Cm(29.7)
    section.top_margin = Cm(1.9)
    section.bottom_margin = Cm(1.8)
    section.left_margin = Cm(2.0)
    section.right_margin = Cm(2.0)
    section.header_distance = Cm(0.9)
    section.footer_distance = Cm(0.9)

    normal = doc.styles["Normal"]
    normal.font.name = FONT
    normal.font.size = Pt(10.5)
    normal.paragraph_format.line_spacing = 1.38
    normal.paragraph_format.space_after = Pt(4)
    normal.paragraph_format.space_before = Pt(0)

    add_header_footer(doc)
    add_cover(doc)
    add_nav(doc)

    i = 0
    n = len(body)
    while i < n:
        raw = body[i].rstrip()
        line = raw.strip()

        if not line:
            i += 1
            continue

        if re.fullmatch(r"-{3,}", line):
            i += 1
            continue

        # image
        m = re.match(r"!\[([^\]]*)\]\(([^)]+)\)", line)
        if m:
            alt, rel = m.group(1), m.group(2)
            img = (MD_PATH.parent / rel).resolve()
            if img.exists():
                add_image(doc, img, alt)
            i += 1
            continue

        # code block
        if line.startswith("```"):
            i += 1
            buf = []
            while i < n and not body[i].strip().startswith("```"):
                buf.append(body[i])
                i += 1
            i += 1
            add_code(doc, buf)
            continue

        # heading
        m = re.match(r"^(#{1,4})\s+(.*)$", line)
        if m:
            level = len(m.group(1))
            text = m.group(2).strip()
            if level == 1:
                i += 1
                continue
            if level == 2:
                if text.startswith("5. "):
                    doc.add_page_break()
                add_heading(doc, text, 1)
            elif level == 3:
                add_heading(doc, text, 2)
            else:
                add_heading(doc, text, 3)
            i += 1
            continue

        # table
        if line.startswith("|"):
            rows = []
            while i < n and body[i].strip().startswith("|"):
                s = body[i].strip()
                if re.fullmatch(r"[:\-\s|]+", s):
                    i += 1
                    continue
                cells = [c.strip().replace("\\|", "|") for c in s.strip("|").split("|")]
                rows.append(cells)
                i += 1
            add_table(doc, rows)
            continue

        # blockquote
        if line.startswith(">"):
            buf = []
            while i < n and body[i].strip().startswith(">"):
                buf.append(body[i].strip().lstrip(">").strip())
                i += 1
            add_callout(doc, [x for x in buf if x])
            continue

        # list
        m = re.match(r"^[-*]\s+(.*)$", line)
        if m:
            add_list_item(doc, m.group(1))
            i += 1
            continue
        m = re.match(r"^(\d+)[.)]\s+(.*)$", line)
        if m:
            add_list_item(doc, m.group(2), numbered=True, number=m.group(1))
            i += 1
            continue

        # paragraph
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.line_spacing = 1.38
        add_inline(p, line, 10.5, NAVY)
        i += 1

    # Remove tblLook elements carrying attributes not accepted by Word's strict schema.
    for table in doc.tables:
        tblpr = table._tbl.tblPr
        for look in tblpr.findall(qn("w:tblLook")):
            tblpr.remove(look)

    cp = doc.core_properties
    cp.title = "项目资料"
    cp.subject = "FreeBike 校园 360° 沉浸式骑行 Demo — 新队友交接手册"
    cp.author = "FreeBike 项目组"
    cp.keywords = "FreeBike, PICO 4 Pro, Unity, 360 视频, A1, Kinect, 交接"
    cp.comments = "由 Docs/项目资料.md 生成；资料截止 2026-11-03。"
    cp.category = "项目交接"

    doc.save(OUT_PATH)
    print("saved:", OUT_PATH)


if __name__ == "__main__":
    build()
