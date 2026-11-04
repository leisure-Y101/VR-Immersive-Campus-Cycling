# -*- coding: utf-8 -*-
"""生成 FreeBike PICO 4 Pro 校园 360 骑行 Demo 工作报告（DOCX）。"""

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

FONT = "微软雅黑"
DARK = "0B3A53"
ACCENT = "0E7490"
LIGHT = "E8F4FA"
GREY = "F3F6F8"
RED = "B42318"
GREEN = "067647"
AMBER = "B54708"


def set_run(run, size=10.5, bold=False, color=None, font=FONT, italic=False):
    run.font.name = font
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    if color:
        run.font.color.rgb = RGBColor.from_string(color)
    rpr = run._element.get_or_add_rPr()
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = OxmlElement("w:rFonts")
        rpr.append(rfonts)
    rfonts.set(qn("w:eastAsia"), font)


def set_style_font(style, size, bold=True, color=DARK):
    style.font.name = FONT
    style.font.size = Pt(size)
    style.font.bold = bold
    style.font.color.rgb = RGBColor.from_string(color)
    rpr = style.element.rPr
    if rpr is None:
        rpr = OxmlElement("w:rPr")
        style.element.append(rpr)
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = OxmlElement("w:rFonts")
        rpr.append(rfonts)
    rfonts.set(qn("w:eastAsia"), FONT)


def shade(cell, fill):
    tcpr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), fill)
    tcpr.append(shd)


def cell_text(cell, text, size=9, bold=False, color=None, align=None):
    cell.text = ""
    p = cell.paragraphs[0]
    if align is not None:
        p.alignment = align
    p.paragraph_format.space_before = Pt(1)
    p.paragraph_format.space_after = Pt(1)
    r = p.add_run(str(text))
    set_run(r, size=size, bold=bold, color=color)


def add_table(doc, headers, rows, widths=None, size=9, header_fill=DARK):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    table.allow_autofit = False
    hdr = table.rows[0].cells
    for i, h in enumerate(headers):
        cell_text(hdr[i], h, size=size, bold=True, color="FFFFFF")
        shade(hdr[i], header_fill)
    for idx, row in enumerate(rows):
        cells = table.add_row().cells
        for i, val in enumerate(row):
            cell_text(cells[i], val, size=size)
            if idx % 2 == 1:
                shade(cells[i], GREY)
    if widths:
        for i, w in enumerate(widths):
            for cell in table.columns[i].cells:
                cell.width = Cm(w)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def add_callout(doc, title, body, fill=LIGHT):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = table.cell(0, 0)
    shade(cell, fill)
    p = cell.paragraphs[0]
    r = p.add_run(title)
    set_run(r, size=10.5, bold=True, color=DARK)
    p2 = cell.add_paragraph()
    r2 = p2.add_run(body)
    set_run(r2, size=10, color="1F2937")
    cell.width = Cm(16.6)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def add_bullets(doc, items, size=10.5):
    for item in items:
        p = doc.add_paragraph(style="List Bullet")
        p.paragraph_format.space_after = Pt(3)
        r = p.add_run(item)
        set_run(r, size=size)


def add_problem(doc, pid, title, phenomenon, root, fix, verify, status):
    doc.add_heading(f"{pid}  {title}", level=3)
    for label, text in (("现象", phenomenon), ("根因", root),
                        ("处理", fix), ("验证", verify), ("状态", status)):
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(3)
        r1 = p.add_run(label + "：")
        set_run(r1, size=10.5, bold=True, color=ACCENT)
        color = None
        if label == "状态":
            if "未闭环" in text or "待" in text:
                color = RED if "未闭环" in text else AMBER
            elif "已解决" in text or "已打通" in text:
                color = GREEN
        r2 = p.add_run(text)
        set_run(r2, size=10.5, color=color, bold=(label == "状态"))


def add_page_field(paragraph):
    run = paragraph.add_run()
    f1 = OxmlElement("w:fldChar")
    f1.set(qn("w:fldCharType"), "begin")
    it = OxmlElement("w:instrText")
    it.set(qn("xml:space"), "preserve")
    it.text = "PAGE"
    f2 = OxmlElement("w:fldChar")
    f2.set(qn("w:fldCharType"), "end")
    run._r.append(f1)
    run._r.append(it)
    run._r.append(f2)


def p(doc, text, size=10.5, bold=False, color=None, align=None, before=0, after=6, first_indent=True):
    par = doc.add_paragraph()
    par.paragraph_format.space_before = Pt(before)
    par.paragraph_format.space_after = Pt(after)
    par.paragraph_format.line_spacing = 1.35
    if align is not None:
        par.alignment = align
    if first_indent:
        par.paragraph_format.first_line_indent = Cm(0.74)
    r = par.add_run(text)
    set_run(r, size=size, bold=bold, color=color)
    return par


# ------------------------------------------------------------------ document

doc = Document()
doc.core_properties.title = "FreeBike PICO 4 Pro 校园 360 骑行 Demo 工作报告"
doc.core_properties.subject = "技术工作报告"
doc.core_properties.author = "FreeBike 项目组 / AI 助手"
doc.core_properties.comments = "金字塔原理 + IMRaD；2026-10-29"

section = doc.sections[0]
section.page_width = Cm(21)
section.page_height = Cm(29.7)
section.top_margin = Cm(2.2)
section.bottom_margin = Cm(2.0)
section.left_margin = Cm(2.2)
section.right_margin = Cm(2.2)

styles = doc.styles
set_style_font(styles["Normal"], 10.5, bold=False, color="1F2937")
styles["Normal"].paragraph_format.line_spacing = 1.35
styles["Normal"].paragraph_format.space_after = Pt(6)
for name, size in (("Heading 1", 16), ("Heading 2", 13), ("Heading 3", 11.5)):
    set_style_font(styles[name], size, bold=True, color=DARK)
    styles[name].paragraph_format.space_before = Pt(14 if name == "Heading 1" else 10)
    styles[name].paragraph_format.space_after = Pt(6)

header = section.header.paragraphs[0]
header.alignment = WD_ALIGN_PARAGRAPH.RIGHT
hr = header.add_run("FreeBike PICO 4 Pro 校园 360 骑行 Demo · 工作报告")
set_run(hr, size=8.5, color="6B7280")

footer = section.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
fr = footer.add_run("第 ")
set_run(fr, size=8.5, color="6B7280")
add_page_field(footer)
fr2 = footer.add_run(" 页  ·  2026-10-29")
set_run(fr2, size=8.5, color="6B7280")

# Cover
for _ in range(3):
    doc.add_paragraph()
t = doc.add_paragraph()
t.alignment = WD_ALIGN_PARAGRAPH.CENTER
tr = t.add_run("FreeBike PICO 4 Pro")
set_run(tr, size=30, bold=True, color=DARK)
t2 = doc.add_paragraph()
t2.alignment = WD_ALIGN_PARAGRAPH.CENTER
t2r = t2.add_run("校园 360° 骑行 Demo")
set_run(t2r, size=26, bold=True, color=ACCENT)
t3 = doc.add_paragraph()
t3.alignment = WD_ALIGN_PARAGRAPH.CENTER
t3r = t3.add_run("技术工作报告")
set_run(t3r, size=20, bold=True, color="374151")
t4 = doc.add_paragraph()
t4.alignment = WD_ALIGN_PARAGRAPH.CENTER
t4r = t4.add_run("2026-10-26 — 2026-10-29")
set_run(t4r, size=12, color="6B7280")
doc.add_paragraph()
add_table(
    doc,
    ["项目", "内容"],
    [
        ["报告版本", "v1.0（结项阶段版）"],
        ["编制", "FreeBike 项目组 / AI 助手"],
        ["适用工程", "C:\\Users\\admin\\freebike（Unity 6000.6.6f1）"],
        ["交付对象", "项目负责人、开发/测试同学"],
        ["报告方法", "金字塔原理（结论先行 / MECE / SCQA） + IMRaD"],
    ],
    widths=[3.2, 13.2],
    size=9.5,
)
doc.add_page_break()

# Executive summary
doc.add_heading("报告摘要（结论先行）", level=1)
add_callout(
    doc,
    "一句话结论",
    "FreeBike 的“主页 8K 全景 → 开始 → 360 视频 → 按住 B 暂停 → 继续/退出”产品链路已经成型；"
    "A1 骑行台的 BLE→UDP 数据链路已实测打通；当前尚未闭环的是三类问题：Unity 对 4K/6K 视频的解码兼容、"
    "头显里仍被 SteamVR 面板/影院层占据焦点、以及 OpenXR 手柄 Profile 未配置。",
)

doc.add_heading("三条核心结论", level=2)
add_bullets(doc, [
    "结论一（产品）：功能减法方向正确。极简三段式应用（主页 / 骑行 / 暂停）已落地，"
    "8K 首页、4K/6K 视频、暂停菜单、A1 联动逻辑均已写入工程，编译 0 error。",
    "结论二（技术）：视频卡顿的根因不是 Unity 逻辑，而是 6.4K H.264 / 361 Mbps 超出 H.264 硬解上限（4096×4096），"
    "只能软解（约 25 fps）；转成 6.4K HEVC 后 NVDEC 实测 131 fps，硬件余量充足。",
    "结论三（交付）：VR 通不起来有两个环境级根因——应用早于 SteamVR 启动，以及 DSH 工作区 "
    "Low integrity 导致 exe 以低完整性运行、访问不了 SteamVR OpenXR；改到 C 盘 Medium 目录构建后 "
    "OpenXR 已能正常 FOCUSED。",
])

doc.add_heading("关键成果", level=2)
add_table(
    doc,
    ["成果", "状态", "关键证据"],
    [
        ["极简应用（主页/骑行/暂停）", "已完成", "CampusRideApp.cs 重构；MSBuild 0 error"],
        ["6.4K HEVC 视频（6400×3200/59.94）", "已生成", "130 Mbps / 1.09 GB；NVDEC 131 fps"],
        ["4K H.264 备份视频（3840×1920/59.94）", "已生成", "70 Mbps / 589 MB"],
        ["A1 BLE→UDP 数据链路", "已实测", "26–35 W；停踩 0 W；UDP 50506"],
        ["OpenXR/SteamVR 初始化", "C 盘构建已通", "Runtime Name: SteamVR/OpenXR；FOCUSED"],
        ["头显内进入应用全景", "未闭环", "仍显示 SteamVR 面板/影院层"],
        ["4K/6K 视频在 Unity 内稳定播放", "未闭环", "日志：主视频播不了"],
    ],
    widths=[6.0, 2.8, 7.6],
)

doc.add_heading("关键风险", level=2)
add_bullets(doc, [
    "视频解码兼容性：H.264 High/B 帧 + 时间戳可能触发 Unity/MediaFoundation 准备失败；需 baseline/CFR 或 HEVC 4K 验证。",
    "OpenXR 焦点：SteamVR 面板、多实例、窗口不在前台都会把应用切到 VISIBLE，头显看到的是影院/桌面。",
    "控制器映射：当前仅启用 Khronos Simple Controller Profile，B/Grip 可能没有映射。",
    "工程卫生：旧构建、旧场景、硬编码 D 盘路径容易造成“改了没生效”的假象。",
])

doc.add_heading("一页结论表", level=2)
add_table(
    doc,
    ["维度", "结论", "依据"],
    [
        ["产品形态", "极简 Demo 成立", "主页 8K + 开始 + 6K/4K 视频 + B 暂停 + 继续/退出"],
        ["视频", "6K HEVC 可硬解；Unity 4K 播放待修", "NVDEC 131 fps；日志“主视频播不了”"],
        ["VR", "系统层已通，应用层焦点未闭环", "FOCUSED + 2644² swapchain；头显仍 SteamVR 界面"],
        ["数据", "A1 功率链路已通", "26–35 W / 0 W；UDP 50506"],
        ["交付", "需按新流程构建到 C 盘", "Low integrity 工作区导致 OpenXR 失败"],
    ],
    widths=[3.0, 6.6, 7.2],
)

doc.add_heading("报告结构说明", level=2)
p(doc,
  "本报告同时采用金字塔原理与 IMRaD。金字塔原理保证“结论先行、以上统下、归类分组、逻辑递进”："
  "摘要先给结论，正文按“视频与解码 / VR 运行时 / 场景与数据 / 工程与流程”四组 MECE 展开；"
  "引言使用 SCQA（情境—冲突—问题—答案）。IMRaD 保证框架完整：第 1 章引言、第 2 章方法、第 3 章结果、"
  "第 4 章讨论、第 5 章结论。每个问题均按“现象—根因—处理—验证—状态”独立成节，便于逐项复盘。",
  first_indent=True)

# 1 Introduction
doc.add_heading("1  引言（IMRaD：Introduction）", level=1)
doc.add_heading("1.1  情境（Situation）", level=2)
p(doc,
  "项目目标是在 PICO 4 Pro 上做一套校园 360° 沉浸式骑行 Demo：用户佩戴头显，在 PC 端 Unity 渲染的"
  "全景校园中骑行；真实自行车（ThinkRider A1 功率台）提供功率，经 BLE 桥接进入 Unity，驱动视频播放速度；"
  "交互只保留“开始 / 暂停 / 继续 / 退出”。硬件为 PICO 4 Pro + Windows PC（RTX 4060 Laptop 8 GB）+ A1 骑行台，"
  "PC 端通过 PICO 互联 + SteamVR + OpenXR 串流到头显。")

doc.add_heading("1.2  冲突（Complication）", level=2)
p(doc,
  "原工程功能很多（HUD、心率、报告、五语言、设置、射线菜单等），但演示只需要“看风景”。"
  "同时出现三个硬冲突：6.4K H.264 视频在 PC 上卡顿；应用在头显里不是全景而是 SteamVR 影院/桌面；"
  "A1 数据链路尚未在真实踩踏中验证。三者分别属于编解码、VR 运行时、数据链路三个层面，互相掩盖。")

doc.add_heading("1.3  问题（Question）", level=2)
p(doc,
  "如何在保留 8K 主页与 6K/4K 视频的前提下，做到：①视频不卡、不降分辨率；②应用真正进入头显的沉浸全景，"
  "而不是 SteamVR 面板；③A1 有功率才播放、停踩即暂停；④工程可重复构建、可交付？")

doc.add_heading("1.4  答案（Answer）与范围", level=2)
p(doc,
  "答案是把应用减到三段式，把视频转成硬件可解码的格式，把 A1 数据经 Python BLE 桥接为 UDP，"
  "并把“构建目录完整性、SteamVR 启动顺序、单实例焦点”作为交付前置条件。"
  "本报告覆盖 2026-10-26 至 2026-10-29 的设计、实现、联调与排障；不包含正式校园素材替换、"
  "长期稳定性测试与量产化工作。")

doc.add_heading("1.5  术语与缩写", level=2)
add_table(
    doc,
    ["术语", "说明"],
    [
        ["OpenXR", "跨厂商 VR/AR 运行时接口；本项目在 Windows 上使用 SteamVR 作为 OpenXR Runtime。"],
        ["SteamVR / PICO 互联", "PC 端 VR 运行时与 PICO 串流工具；SteamVR 负责合成与头显输出。"],
        ["NVDEC / NVENC", "NVIDIA 硬件解码 / 编码单元；HEVC 可支持到 8K，H.264 通常上限 4096×4096。"],
        ["CPS / FTMS", "蓝牙 Cycling Power Service（0x1818/0x2A63）与 Fitness Machine Service。"],
        ["Equirect", "等距柱状投影，2:1 全景图/视频，贴到球体内表面形成 360°。"],
        ["Low integrity", "Windows 完整性级别；Low 进程无法访问 Medium/High 的共享资源（如 SteamVR）。"],
        ["MECE / SCQA", "相互独立、完全穷尽 / 情境—冲突—问题—答案。"],
    ],
    widths=[3.6, 13.0],
)

# 2 Methods
doc.add_heading("2  方法（IMRaD：Methods）", level=1)
doc.add_heading("2.1  技术栈", level=2)
add_table(
    doc,
    ["层级", "技术 / 工具", "版本或规格", "用途"],
    [
        ["引擎", "Unity", "6000.6.6f1（Unity 6.6）", "场景、UI、视频、XR"],
        ["渲染", "URP", "随工程", "PC VR 渲染管线"],
        ["XR", "OpenXR + XR Management + XRI", "1.18.0 / 4.6.1 / 3.6.1", "Windows OpenXR 会话与输入"],
        ["PICO", "PICO SDK（PXR Loader）", "3.4.0", "Android 一体机路线备用"],
        ["运行时", "SteamVR", "2.18.4", "Windows OpenXR Runtime + 合成"],
        ["串流", "PICO 互联 / PICO Connect", "—", "头显与 PC 连接"],
        ["语言", "C#", "—", "应用与交互逻辑"],
        ["视频", "H.264 / HEVC + Media Foundation + VideoPlayer", "—", "360 视频解码与材质覆盖"],
        ["转码", "ffmpeg（NVENC / NVDEC）", "7.1 essentials", "6K/4K 转码与硬件基准"],
        ["数据", "Python + bleak + WinRT + UDP", "3.14.3", "A1 BLE → UDP 50506"],
        ["硬件", "PICO 4 Pro / RTX 4060 Laptop / A1", "8 GB 显存 / 22 线程", "目标设备与骑行台"],
        ["验证", "MSBuild / PowerShell / icacls / Player.log", "—", "静态编译、权限、日志取证"],
    ],
    widths=[2.2, 5.6, 4.2, 4.6],
    size=8.5,
)

doc.add_heading("2.2  系统架构与数据流", level=2)
p(doc,
  "系统分为三条流：视频流、XR 显示流、骑行数据流。三条流在 Unity 的 CampusRideApp 汇合："
  "A1 功率决定视频是否播放与倍速；视频纹理通过 MaterialOverride 贴到 800 m 半径球体内表面；"
  "相机由 XR 头显姿态驱动，经 OpenXR → SteamVR → PICO 互联输出到头显。")
add_callout(
    doc,
    "数据流（文字版）",
    "A1 骑行台 --BLE CPS 0x2A63--> a1_bridge.py --UDP 127.0.0.1:50506--> PanoBikeSpeed "
    "--> VideoPlayer（播放/暂停/倍速）\n"
    "Unity Camera --OpenXR--> SteamVR 2.18.4 --PICO 互联--> PICO 4 Pro\n"
    "8K homepage.jpg --球体材质--> 主页；6.4K/4K 视频 --MaterialOverride--> PanoSphere 内表面",
    fill=LIGHT,
)

doc.add_heading("2.3  实施过程（时间线）", level=2)
add_table(
    doc,
    ["时间", "阶段", "关键动作", "产出"],
    [
        ["10-26", "存量梳理", "盘点旧工程、旧构建、视频素材与 XR 配置", "确认 PC VR + OpenXR + SteamVR 路线"],
        ["10-27 上午", "功能减法", "重写 CampusRideApp 为三段式；删除 HUD/心率/报告/多语言等", "极简应用 + 主页 + 暂停菜单"],
        ["10-27 中午", "视频排障", "定位 6.4K H.264 软解；生成 6K HEVC 与 4K H.264", "6K HEVC 1.09 GB；4K 589 MB"],
        ["10-27 下午", "VR 排障", "发现启动顺序、Low integrity、多实例、500% 超采样问题", "C 盘 Medium 构建；OpenXR FOCUSED"],
        ["10-27 傍晚", "数据联调", "启动 a1_bridge.py，实测功率 26–35 W / 停踩 0 W", "A1 数据链路打通"],
        ["10-29", "复盘与交付", "整理问题清单、验证证据与后续优先级", "本报告"],
    ],
    widths=[2.2, 3.0, 7.8, 3.6],
    size=8.5,
)

doc.add_heading("2.4  验证方法", level=2)
add_table(
    doc,
    ["手段", "目的", "关键结果"],
    [
        ["MSBuild 编译 Assembly-CSharp.csproj", "在不启动 Unity 的情况下做全量 C# 类型检查", "多次 0 error（179 warning 来自 SDK/旧代码）"],
        ["Player.log 关键字", "判断 OpenXR 是否初始化、视频是否报错、进程完整性", "Runtime Name / FOCUSED / 主视频播不了 / low integrity"],
        ["ffmpeg -benchmark + NVDEC", "测 6.4K HEVC 硬件解码吞吐", "131 fps（约 2.2× 实时）"],
        ["icacls", "检查 exe/目录完整性标签", "D 盘 Low；C 盘无 Mandatory Label（Medium）"],
        ["UDP 50506 探针", "确认端口被应用占用、数据报可达", "BIND_FAILED 10048（应用在监听）"],
        ["A1 实踩", "验证 BLE 连接、CPS 订阅、功率上报", "26–35 W，停踩 0 W"],
    ],
    widths=[5.2, 6.2, 5.2],
    size=8.5,
)

# 3 Results
doc.add_heading("3  结果（IMRaD：Results）", level=1)
doc.add_heading("3.1  总体结果", level=2)
add_table(
    doc,
    ["需求", "状态", "证据 / 说明"],
    [
        ["主页只保留 8K 背景 + 开始", "已完成", "BuildHomeUi + GoHome；homepage 7680×3840"],
        ["开始后进入视频，不显示其他内容", "部分完成", "视频文件与路径就绪；4K H.264 播放报错"],
        ["按住 B 暂停，两个按钮：继续 / 退出", "已完成", "pauseHoldSeconds=0.25；Continue/Exit"],
        ["退出回主页", "已完成", "GoHome 恢复 8K 球与开始按钮"],
        ["6K60 不降分辨率", "文件就绪", "6.4K HEVC 6400×3200/59.94；Unity 内待验证"],
        ["A1 有功率才播、停踩暂停", "已实现待实测", "controlPlaybackState=true；≥5 W 播放"],
        ["进入头显沉浸全景", "未闭环", "OpenXR 已 FOCUSED，但仍显示 SteamVR 面板/影院"],
    ],
    widths=[6.4, 3.0, 7.2],
)

doc.add_heading("3.2  关键指标", level=2)
add_table(
    doc,
    ["指标", "数值", "备注"],
    [
        ["原始 6K 视频", "6400×3200 / 59.94 fps / H.264 / 361 Mbps / 3.16 GB / 70 s", "超出 H.264 硬解上限"],
        ["6K HEVC", "6400×3200 / 59.94 fps / HEVC / 130 Mbps / 1.09 GB / 70 s", "NVDEC 131 fps"],
        ["4K H.264", "3840×1920 / 59.94 fps / H.264 / 70 Mbps / 589 MB / 70 s", "Unity 内仍报播不了"],
        ["8K 主页", "7680×3840，mipmap，Max Size 8192", "压缩导入，显存约 30–40 MB 量级"],
        ["每眼渲染分辨率", "最新 2644×2644；曾为 6976×6976（500% 超采样）", "2644 更合理"],
        ["A1 功率", "骑行 26–35 W；停踩 0 W", "CPS 0x2A63，约 1 Hz"],
        ["编译", "0 error / 179 warning", "MSBuild 全量校验"],
    ],
    widths=[3.6, 8.6, 4.4],
    size=8.5,
)

doc.add_heading("3.3  问题与坑（按 MECE 分组，逐项回答）", level=2)
p(doc,
  "以下 15 个问题按四组展开：A 视频与解码、B VR 运行时与显示、C 场景/数据/交互、D 工程与交付。"
  "每组内部相互独立，合起来覆盖本次会话遇到的全部关键坑。每个问题统一给出“现象—根因—处理—验证—状态”。")

doc.add_heading("3.3.1  视频与解码", level=3)
add_problem(
    doc, "A1", "6.4K H.264 361 Mbps 播放卡顿",
    "编辑器 Play 与 PC 构建里视频都卡；构建日志出现 “Failed to create the D3D11 device … software video decoding”。",
    "H.264 硬件解码通常上限 4096×4096；6400×3200 超出上限，只能软件解码；361 Mbps 码率进一步放大 CPU 压力，实测软件解码约 25 fps。",
    "用 ffmpeg NVENC 转 6.4K HEVC（130 Mbps），保留 6400×3200/59.94；另生成 4K H.264（70 Mbps）做兼容备份。",
    "ffmpeg -benchmark + hevc_cuvid 实测 131 fps；用户反馈 4K 版本“不卡了”；文件规格用 ffmpeg -i 核对。",
    "已缓解；Unity 端 4K 播放仍有独立错误（见 A2）。",
)
add_problem(
    doc, "A2", "Unity 报“主视频播不了，切换到兼容视频”",
    "Player.log：`CampusRideApp: 主视频播不了，切换到兼容视频 ...6k60fps_4k.mp4`；前一行有 `Unexpected timestamp values detected … H.264 videos not encoded with the baseline profile`。",
    "高概率是 H.264 High profile + B 帧 + 非 baseline 时间戳导致 Unity/MediaFoundation 准备失败；也可能叠加 D3D11 解码设备创建失败（A3）与多实例资源竞争。",
    "计划 baseline/CFR 重编码：`-profile:v baseline -bf 0 -fps_mode cfr -r 60000/1001`；或改用 4K HEVC。替换同路径文件不需要重新编译。",
    "待重编码后用同一场景复测，检查日志是否仍出现“主视频播不了”。",
    "未闭环（P0）。",
)
add_problem(
    doc, "A3", "D3D12 下 D3D11 视频解码设备创建失败",
    "日志：`Failed to create the D3D11 device` / `Dedicated video D3D11 device creation failed (error: 0x887a0004)`；随后全部走软件解码。",
    "Unity 的视频硬解通道走 D3D11（DXVA）。当播放器主设备是 D3D12、进程又是 Low integrity 时，单独创建 D3D11 设备会失败，硬件解码不可用。",
    "建议把 Player Graphics API 切到 D3D11（此前被拒绝执行，未改动）；C 盘 Medium 构建后先观察是否仍报错；用任务管理器 GPU 的 Video Decode 引擎确认是否硬解。",
    "待 D3D11 或 Medium 构建下复测；目前 4K 播放错误尚未排除此因素。",
    "待定。",
)
add_problem(
    doc, "A4", "6K 保清晰度必须用 HEVC，不能用 H.264",
    "用户明确要求“必须 6K60，不降分辨率、不掉帧”；但 H.264 6.4K 无法硬解。",
    "H.264 硬解上限 4096；HEVC（H.265）NVDEC 可到 8K。Windows 已安装 Microsoft.HEVCVideoExtension 2.5.33。",
    "生成 6.4K HEVC 主用文件，保留 4K H.264 作为兼容备份；场景按需要切换路径。",
    "NVDEC 131 fps；HEVC 扩展注册表存在；Unity 内 HEVC 最终播放仍需实机确认。",
    "文件就绪，最终播放待验证。",
)

doc.add_heading("3.3.2  VR 运行时与显示", level=3)
add_problem(
    doc, "B1", "OpenXR RUNTIME_UNAVAILABLE：应用跑在 SteamVR 之前",
    "日志 `xrCreateInstance: XR_ERROR_RUNTIME_UNAVAILABLE`；头显里是平面/影院画面。时间线：13:04:48 应用启动，13:04:50 OpenXR 失败，13:05:02 SteamVR 才开始记录。",
    "Unity OpenXR 只在启动时初始化一次；SteamVR/OpenXR Runtime 未就绪就永久失败，之后不会重试，应用退化成桌面平面窗口。",
    "规定启动顺序：PICO 互联连头显 → 启动 SteamVR 并确认头显进入 SteamVR 主页 → 再启动应用；提供等待 vrserver 的启动脚本。",
    "13:46 日志出现 `Runtime Name: SteamVR/OpenXR`、`XR_SESSION_STATE_FOCUSED`、2644² swapchain，证明顺序修复有效。",
    "已解决（流程问题）。",
)
add_problem(
    doc, "B2", "DSH 工作区 Low integrity 导致 exe 无法访问 SteamVR",
    "日志第一行 `Process is running in low integrity mode...`；`icacls` 显示 `D:\\freebikeapk` 与 `demo\\freebike.exe` 为 `Mandatory Label\\Low Mandatory Level`；OpenXR 仍 RUNTIME_UNAVAILABLE。",
    "DSH 工作区目录带有 Low 完整性标签，构建产物继承 Low。Low 进程被 Windows 完整性隔离，无法访问 Medium/High 的 SteamVR 共享资源与 OpenXR Runtime。",
    "把构建改到 `C:\\Users\\admin\\Desktop\\bikedemo`（Medium）；验证 `icacls` 无 Low 标签；日志不再出现 low integrity。",
    "新构建日志无 low integrity，OpenXR 成功 FOCUSED；这是本次最关键的隐藏坑。",
    "已解决。",
)
add_problem(
    doc, "B3", "SteamVR 500% 超采样导致 6976×6976/眼",
    "早期日志 `Recommended: Width=6976, Height=6976`，画面极卡。",
    "`D:\\Steam\\config\\steamvr.vrsettings` 中 `supersampleManualOverride: true`、`supersampleScale: 5`（500%），把每眼像素放大约 10 倍。",
    "把渲染分辨率降到 Auto 或 100–150%；最新日志已降到 2644×2644。",
    "最新日志 Recommended 2644²；仍需在头显内确认帧率。",
    "已改善（需最终确认）。",
)
add_problem(
    doc, "B4", "多实例/焦点被抢，头显仍显示 SteamVR 影院层",
    "同一份 Player.log 中出现 3 个进程 PID（22724 / 43692 / 20300），并有 `FOCUSED → VISIBLE → SYNCHRONIZED`；用户描述“像壳，中间一个观察框”，确认是 SteamVR 界面。",
    "多个 freebike.exe 同时运行会抢 SteamVR 前台焦点与 UDP 50506；应用窗口不在前台时 SteamVR 显示仪表盘/影院层，头显里就不是应用画面。",
    "任务管理器结束全部 freebike.exe；只启动一个；在 PC 上点一下应用窗口保证前台；头显里按左手柄菜单键关闭 SteamVR 面板；必要时用手柄选中运行中的 freebike 窗口。",
    "最新单实例日志已 FOCUSED，但用户仍看到 SteamVR 界面，说明焦点/面板仍未真正切到应用。",
    "未闭环（P0）。",
)
add_problem(
    doc, "B5", "OpenXR 手柄 Profile 缺失，B/Grip 可能不映射",
    "日志 `Features requested to be enabled: Khronos Simple Controller Profile`，其余 Interaction Profile 均为关闭。",
    "Unity OpenXR 需要启用与 PICO Connect/SteamVR 暴露的手柄匹配的 Interaction Profile，否则 `InputDevices` 拿不到完整按键（尤其 B/Grip）。",
    "在 Project Settings → XR Plug-in Management → OpenXR（Windows）中启用 Oculus Touch / HTC Vive / Valve Index / KHR Simple 等 Profile；临时可用键盘 Enter 开始、按住 Esc 暂停。",
    "待配置后实机验证 Trigger/B/Grip。",
    "待配置。",
)

doc.add_heading("3.3.3  场景、数据与交互", level=3)
add_problem(
    doc, "C1", "重复 PanoSphere / 两个 PanoBikeSpeed 抢 UDP 50506",
    "场景里有 `PanoSphere` 与 `PanoSphere (1)`，各带 VideoPlayer + PanoBikeSpeed；日志 `PanoBikeSpeed: UDP 50506 打开失败`。",
    "重复对象导致两个 UDP 监听、两个球体在 800 m 处重叠，A1 数据可能进错对象。",
    "在 `CampusRideApp.Awake()` 里关闭 `PanoSphere (1)`，保证只保留主球与一个 PanoBikeSpeed。",
    "日志出现 `已关闭重复的 PanoSphere (1)`；UDP 50506 正常监听。",
    "已解决。",
)
add_problem(
    doc, "C2", "A1 骑行台 BLE 数据链路验证",
    "需要确认真实踩踏时能否收到功率。",
    "A1 使用 CPS（0x1818 / 0x2A63）；PC 端 Python bleak 连接后把功率以 ASCII float 发到 127.0.0.1:50506。",
    "运行 `a1_bridge.py`（依赖 `.pydeps-ble`）；Unity 端 PanoBikeSpeed 监听 50506；关闭键盘 fallback。",
    "实测：Connected F5:79:6C:84:32:07；订阅 0x2A63；26–35 W；停踩 0 W；UDP 端口被 freebike.exe 占用。",
    "已打通（仅功率；踏频未解析）。",
)
add_problem(
    doc, "C3", "播放与自行车联动：有功率才播、停踩暂停",
    "早期为了 6K 直接播放，视频一进场景就自动播放，和自行车无关。",
    "`controlPlaybackState` 被设为 false、`idlePlayback=1`、StartRide 直接 Play，导致不踩也播。",
    "改回：`controlPlaybackState=true`、`idlePlayback=1`、`StartRide` 先 Pause；PanoBikeSpeed 在 `external && smoothedPower >= 5W` 时 Play，否则 Pause；`allowKeyboardFallback=false`。",
    "编译 0 error；A1 实测有功率/停踩；Unity 内联动待实机确认。",
    "已实现，待实测。",
)
add_problem(
    doc, "C4", "8K 主页纹理会不会拖垮电脑",
    "用户担心 7680×3840 的 8K 照片会卡。",
    "8K 静态纹理压缩后约 30–40 MB（未压缩约 118–157 MB），只加载一次、一个球体一次采样；RTX 4060 8 GB 完全够用；真正的负载是 500% 超采样与视频解码。",
    "保留 8K；如要省显存可把 Max Size 降到 4096；不视为性能瓶颈。",
    "资源规格与导入设置核对；无持续卡顿证据。",
    "结论：不影响，无需降级。",
)

doc.add_heading("3.3.4  工程与交付流程", level=3)
add_problem(
    doc, "D1", "构建与场景/脚本不同步，旧包混淆",
    "出现 11:55 的 exe 对应 12:41 的场景、12:46 的 demo 对应 13:19 的场景；旧 `freebike2.0` 也在被启动；场景序列化字段会覆盖脚本默认值。",
    "改脚本/场景后没有立即重建；多个构建目录并存；Unity 场景字段优先于脚本默认值，改代码不改场景不生效。",
    "用文件时间戳对比 exe / Assembly-CSharp.dll / 场景 / 脚本；只勾 panoTest 构建到新目录；改场景 YAML 后再构建；旧包只作备份不启动。",
    "多次 MSBuild 0 error；日志中的视频路径与构建时间能对应。",
    "已解决，需固化为流程。",
)
add_problem(
    doc, "D2", "视频路径硬编码与旧素材体积",
    "视频路径写死 `D:\\freebikeapk\\...`；StreamingAssets 为空；旧 nighttest/Videos 共约 1.16 GB 会被打进包。",
    "PC 演示机可用，但拷贝到其他机器会失效；旧素材无引用却占体积。",
    "解析顺序改为 StreamingAssets/pro2 → StreamingAssets 根 → D 盘外部路径；旧视频移到 `_backup_20261027\\removed-streaming-assets`；打包后把视频复制到 `_Data\\StreamingAssets\\pro2\\`。",
    "日志显示主视频路径正确；备份目录大小约 1.14 GB。",
    "已处理；便携交付需按流程复制视频。",
)

doc.add_heading("3.4  问题总览", level=2)
add_table(
    doc,
    ["编号", "类别", "问题", "状态"],
    [
        ["A1", "视频", "6.4K H.264 361 Mbps 软解卡顿", "已缓解"],
        ["A2", "视频", "Unity 报主视频播不了（High/B 帧/时间戳）", "未闭环"],
        ["A3", "视频", "D3D12 下 D3D11 解码设备失败", "待定"],
        ["A4", "视频", "6K 必须 HEVC 才能硬解", "文件就绪"],
        ["B1", "VR", "应用早于 SteamVR 启动", "已解决"],
        ["B2", "VR", "DSH 工作区 Low integrity", "已解决"],
        ["B3", "VR", "SteamVR 500% 超采样", "已改善"],
        ["B4", "VR", "多实例/焦点被抢，显示 SteamVR 影院", "未闭环"],
        ["B5", "VR", "OpenXR 手柄 Profile 缺失", "待配置"],
        ["C1", "场景", "重复 PanoSphere 抢 UDP", "已解决"],
        ["C2", "数据", "A1 BLE→UDP 链路", "已打通"],
        ["C3", "交互", "有功率才播、停踩暂停", "待实测"],
        ["C4", "性能", "8K 主页纹理", "无影响"],
        ["D1", "流程", "构建与场景不同步", "已解决"],
        ["D2", "交付", "路径硬编码与旧素材体积", "已处理"],
    ],
    widths=[1.6, 2.2, 9.4, 3.4],
    size=8.5,
)

# 4 Discussion
doc.add_heading("4  讨论（IMRaD：Discussion）", level=1)
doc.add_heading("4.1  根因归类", level=2)
p(doc,
  "15 个问题看似分散，归因后只有五类。把它们分开，才能避免“改错层”：视频卡不是 Unity 逻辑，"
  "VR 不进不是场景问题，A1 不通不是 Unity 代码问题。")
add_table(
    doc,
    ["根因类别", "涉及问题", "本质"],
    [
        ["编解码边界", "A1 / A2 / A4", "H.264 硬解 4096 上限；高码率/High/B 帧/时间戳兼容性"],
        ["Windows 完整性", "B2 / A3", "Low 进程无法访问 SteamVR 与 D3D11 解码设备"],
        ["OpenXR 生命周期与焦点", "B1 / B4", "只初始化一次；前台窗口/单实例/仪表盘决定是否可见"],
        ["场景对象与序列化", "C1 / D1", "重复对象、场景字段覆盖脚本默认值"],
        ["工程与交付卫生", "D2", "构建/场景/路径/素材版本不一致"],
    ],
    widths=[3.6, 4.2, 8.8],
    size=8.5,
)

doc.add_heading("4.2  经验清单（可直接复用）", level=2)
add_bullets(doc, [
    "先查边界再调代码：H.264 先看分辨率是否 >4096、码率是否过高；6K 优先 HEVC。",
    "先看完整性再查 XR：`icacls` 有 Low 标签、日志有 low integrity，就先换 Medium 目录，不要浪费时间调 OpenXR。",
    "先 SteamVR 后应用：OpenXR 只初始化一次；用等待 vrserver 的启动脚本。",
    "只跑一个实例：多实例会抢焦点与 UDP 端口，头显里就是 SteamVR 影院层。",
    "看日志关键字：`Runtime Name`、`FOCUSED`、`RUNTIME_UNAVAILABLE`、`主视频播不了`、`Failed to create the D3D11 device`。",
    "改场景再构建：Unity 场景序列化字段优先于脚本默认值；exe/DLL/场景时间戳要对齐。",
    "视频路径优先 StreamingAssets，外部 D 盘只做开发回退；交付时显式复制视频。",
    "8K 静态图不是瓶颈；真正的性能大头是每眼渲染分辨率与视频解码。",
])

doc.add_heading("4.3  未闭环问题与优先级", level=2)
add_table(
    doc,
    ["优先级", "动作", "验收标准", "备注"],
    [
        ["P0", "4K H.264 重编码为 baseline/CFR（或 4K HEVC），复测播放", "日志不再出现“主视频播不了”，画面正常", "同路径替换，无需重编译"],
        ["P0", "单实例 + 前台 + 关闭 SteamVR 面板", "头显里看到应用主页/校园，不再是 SteamVR 界面", "必要时加单实例互斥与 SetForegroundWindow"],
        ["P1", "配置 OpenXR Interaction Profiles", "Trigger/B/Grip 均有映射", "Oculus Touch / HTC Vive / Valve Index / KHR Simple"],
        ["P1", "确认硬解路径（D3D11 或 Medium 进程）", "任务管理器 Video Decode 有占用，4K 不卡", "D3D12 下可能回退软解"],
        ["P2", "6K/4K 切换参数化、A1 踏频解析、HUD/日志", "可配置、可观测", "为正式演示做准备"],
        ["P2", "交付脚本：构建到 C 盘 + 复制视频 + 启动等待 SteamVR", "一键可复现", "减少人为失误"],
    ],
    widths=[1.8, 7.0, 5.4, 3.0],
    size=8.5,
)

doc.add_heading("4.4  风险与假设", level=2)
add_bullets(doc, [
    "假设演示机固定为当前 RTX 4060 Laptop + PICO 4 Pro + A1；换机需重新验证硬解与 OpenXR Runtime。",
    "假设 Windows 已安装 HEVC Video Extension；未安装时 6K HEVC 可能无法解码。",
    "假设 SteamVR 始终作为 OpenXR Runtime；若改用其他 Runtime，需重新配置。",
    "低完整性工作区是 DSH 环境特性；正式交付目录必须放在普通 Medium 目录。",
    "笔记本长时间 6K 解码 + VR 渲染存在散热/降频风险，建议插电 + 性能模式。",
])

# 5 Conclusion
doc.add_heading("5  结论（金字塔收束）", level=1)
doc.add_heading("5.1  三条结论", level=2)
add_bullets(doc, [
    "产品层：极简三段式成立，主页/骑行/暂停与 A1 联动逻辑已经落地，这是正确的演示形态。",
    "技术层：6K 必须走 HEVC 硬解；4K H.264 在 Unity 内仍有兼容问题，是当前 P0；VR 系统层已通，"
    "但应用焦点/多实例仍会把头显留在 SteamVR 界面。",
    "交付层：构建目录完整性、SteamVR 启动顺序、单实例、视频路径与素材版本，是比代码更容易翻车的四个交付前置条件。",
])

doc.add_heading("5.2  三条建议", level=2)
add_bullets(doc, [
    "先把 4K 视频换成 baseline/CFR 或 HEVC 4K，闭环“能播”；再谈 6K。",
    "把“单实例 + 等待 SteamVR + 前台窗口”固化成启动脚本，避免每次手动排障。",
    "把 OpenXR Profile、D3D11 硬解、6K/4K 参数做成可配置项，形成可复现的演示包。",
])

p(doc,
  "总体判断：项目已经越过“能不能跑”的阶段，进入“能不能稳定演示”的阶段。"
  "剩余问题都属于环境/兼容性/流程，不是架构性难题；按 P0→P1 顺序处理即可闭环。",
  first_indent=True)

# Appendix
doc.add_page_break()
doc.add_heading("附录 A  关键路径", level=1)
add_table(
    doc,
    ["用途", "路径"],
    [
        ["Unity 工程", "C:\\Users\\admin\\freebike"],
        ["主场景", "Assets/Scenes/panoTest.unity"],
        ["应用脚本", "Assets/Scripts/App/CampusRideApp.cs"],
        ["骑行数据脚本", "Assets/Scripts/PanoBikeSpeed.cs"],
        ["A1 桥接", "D:\\freebikeapk\\a1_bridge.py"],
        ["6K HEVC", "D:\\freebikeapk\\360vidio\\pro2\\6k60fps_hevc.mp4"],
        ["4K H.264", "D:\\freebikeapk\\360vidio\\pro2\\6k60fps_4k.mp4"],
        ["8K 主页", "Assets/Resources/homepage.jpg"],
        ["C 盘构建", "C:\\Users\\admin\\Desktop\\bikedemo"],
        ["Player 日志", "%USERPROFILE%\\AppData\\LocalLow\\DefaultCompany\\freebike\\Player.log"],
        ["SteamVR 配置", "D:\\Steam\\config\\steamvr.vrsettings"],
        ["备份目录", "D:\\freebikeapk\\_backup_20261027"],
    ],
    widths=[3.6, 13.0],
    size=8.5,
)

doc.add_heading("附录 B  日志关键字与命令", level=1)
add_table(
    doc,
    ["关键字 / 命令", "含义 / 用途"],
    [
        ["Runtime Name: SteamVR/OpenXR", "OpenXR 已连上 SteamVR"],
        ["XR_SESSION_STATE_FOCUSED", "应用拿到 VR 前台焦点"],
        ["XR_ERROR_RUNTIME_UNAVAILABLE", "SteamVR 未就绪或权限/完整性问题"],
        ["Process is running in low integrity mode", "进程低完整性，检查 icacls/构建目录"],
        ["主视频播不了", "VideoPlayer 准备失败，查编码/时间戳/解码设备"],
        ["Failed to create the D3D11 device", "硬解设备创建失败，考虑 D3D11"],
        ["icacls <path>", "查看 Mandatory Label 是否 Low"],
        ["python a1_bridge.py", "启动 A1 BLE→UDP 桥接"],
        ["ffmpeg -benchmark -c:v hevc_cuvid ...", "测 NVDEC 解码吞吐"],
    ],
    widths=[8.2, 8.4],
    size=8.5,
)

doc.add_heading("附录 C  版本与关键参数", level=1)
add_table(
    doc,
    ["项目", "值"],
    [
        ["Unity / OpenXR / XRI", "6000.6.6f1 / 1.18.0 / 3.6.1"],
        ["SteamVR / PICO SDK", "2.18.4 / 3.4.0"],
        ["GPU / CPU 线程", "RTX 4060 Laptop 8 GB / 22"],
        ["暂停按住时长", "0.25 s"],
        ["开始按钮", "560×132 px，offset (0,-0.12,2.2)，scale 0.001"],
        ["暂停面板", "720×460 px，距离 1.6 m"],
        ["A1 判定阈值", "≥5 W 播放；无数据/停踩暂停"],
        ["倍速范围", "1.0–1.25（idlePlayback=1，maxPlayback=1.25）"],
        ["UDP 端口", "50506（127.0.0.1）"],
    ],
    widths=[5.6, 11.0],
    size=8.5,
)

doc.add_heading("附录 D  术语表", level=1)
add_table(
    doc,
    ["术语", "解释"],
    [
        ["IMRaD", "Introduction / Methods / Results / Discussion，学术论文与分析报告的标准骨架。"],
        ["金字塔原理", "Barbara Minto：结论先行、以上统下、归类分组、逻辑递进。"],
        ["MECE", "Mutually Exclusive, Collectively Exhaustive，相互独立、完全穷尽。"],
        ["SCQA", "Situation / Complication / Question / Answer，引言叙事结构。"],
        ["Low integrity", "Windows 低完整性级别；Low 进程不能访问 Medium/High 共享资源。"],
        ["OpenXR Runtime", "实际提供 XR 会话的运行时；本机为 SteamVR。"],
        ["CPS", "Cycling Power Service，A1 使用 0x1818/0x2A63 上报功率。"],
    ],
    widths=[3.6, 13.0],
    size=8.5,
)

out = r"D:\freebikeapk\FreeBike_工作报告_2026-10-29.docx"
doc.save(out)
print("saved", out)
