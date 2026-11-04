# -*- coding: utf-8 -*-
from pathlib import Path
from shutil import copy2

from docx import Document
from docx.enum.text import WD_BREAK
from docx.shared import Pt

DOC = Path(r"D:\freebike\Docs\Reports\FreeBike_工作报告_2026-10-29.docx")
BACKUP = Path(r"D:\freebike\Docs\Reports\FreeBike_工作报告_2026-10-29_before_20261029_addendum.docx")

copy2(DOC, BACKUP)
doc = Document(DOC)


def find_para(prefix):
    for p in doc.paragraphs:
        if p.text.strip().startswith(prefix):
            return p
    return None


def p_before(target, text="", style=None, bold=False, size=None):
    p = target.insert_paragraph_before(text, style=style)
    if bold:
        for r in p.runs:
            r.bold = True
    if size:
        for r in p.runs:
            r.font.size = Pt(size)
    return p


def table_before(target, headers, rows):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.autofit = True
    hdr = table.rows[0].cells
    for i, h in enumerate(headers):
        hdr[i].text = h
        for p in hdr[i].paragraphs:
            for r in p.runs:
                r.bold = True
    for row in rows:
        cells = table.add_row().cells
        for i, val in enumerate(row):
            cells[i].text = str(val)
    target._p.addprevious(table._tbl)
    return table


# ---------------------------------------------------------------------------
# 1) One addendum conclusion in the executive summary (before 一页结论表)
# ---------------------------------------------------------------------------
summary_next = find_para("一页结论表")
if summary_next is not None:
    p_before(
        summary_next,
        "2026-10-29 增补结论：6K60 原片已在 PC 端以外部路径接入；Pro2 8K 等距柱状图已替换主页背景；"
        "Android APK 仍未包含 6K60，且 freebike1.0.exe 的视频是内嵌 VideoClip，不能直接替换文件。",
        style="List Bullet",
    )

# ---------------------------------------------------------------------------
# 2) Insert section 6 before 附录 A
# ---------------------------------------------------------------------------
appendix = find_para("附录 A")
if appendix is None:
    raise SystemExit("附录 A not found")

# page break before the new chapter
brk = appendix.insert_paragraph_before()
brk.add_run().add_break(WD_BREAK.PAGE)

p_before(appendix, "6  2026-10-29 增补：6K60 素材、8K 背景与交付边界", style="Heading 1")

p_before(appendix, "6.1  结论先行（金字塔顶层）", style="Heading 2")
p_before(
    appendix,
    "本增补不推翻前五章结论，只补充 2026-10-29 新增的素材、背景、Windows/Android 交付与 UI 原型事实。"
    "顶层结论有三条：第一，6K60 原片适合作为 PC 端外部引用，但不适合直接塞进当前 Android APK；"
    "第二，Pro2 的 8K 等距柱状图是有效全景背景，主页已替换，但骑行模式仍应使用 6K60 视频；"
    "第三，freebike1.0.exe 的视频已经编进 sharedassets0.resource，不能靠替换 MP4 解决，必须重建或改用新的 Windows 构建。",
)

p_before(appendix, "表 6-1  2026-10-29 增补结论（结论先行）", style="Caption")
table_before(
    appendix,
    ["层级", "结论", "证据 / 影响"],
    [
        ("素材层", "6K60 原片规格成立，但码率过高，需转码或验证硬解。", "6400×3200 / 59.94 / H.264 / 359 Mbps / 3.16GB / 70.41s。"),
        ("背景层", "Pro2 的 10 张单目图均为 8K 2:1 等距柱状，可作 360 背景。", "7680×3840；GPano ProjectionType=equirectangular；已选 output_1 替换 homepage。"),
        ("PC 交付", "Windows 可外置引用 6K60，换视频不必重打包；旧 freebike1.0 不可直接换。", "CampusRideDemo\\freebike.exe 读取 D 盘；freebike1.0 的 sharedassets0.resource 为旧 4K 254MB。"),
        ("Android 交付", "当前 APK 只有 4K/5.7K，没有 6K60；Android 不能读 D 盘。", "APK 内 assets/Videos 仅 bench_4k60 与 bench_57k30。"),
        ("交互层", "15 个新 UI 已出预览，但尚未接入 Unity；当前仍是旧 Prefab。", "D:\\freebikeapk\\UI\\preview；CampusRideUI.prefab 仍是旧布局。"),
    ],
)

p_before(appendix, "6.2  SCQA：情境、冲突、问题、答案", style="Heading 2")
p_before(
    appendix,
    "情境（S）：项目已经具备 PICO 4 Pro + Windows PC + A1 的 PC VR 演示链路，用户希望用 Pro2 的 6K60 素材与 8K 校园图提升画质。"
    "冲突（C）：6K60 原片为 359 Mbps 的 H.264，体积 3.16GB；PC 端可以用绝对路径直接引用，Android 端却无法访问 D 盘；"
    "同时旧的 freebike1.0.exe 把视频作为 VideoClip 编进 sharedassets0.resource，无法通过替换视频文件更新；新 UI 仍停留在预览稿。"
    "问题（Q）：如何在不破坏现有 A1/功率/倍速逻辑的前提下，让 PC 端用上 6K60、让主页用上 Pro2 8K 图，并明确 Android 与旧 exe 的边界？"
    "答案（A）：PC 端使用外部视频 URL；主页使用 Pro2 单目 equirect 图；Android 端必须转码、放入 StreamingAssets 并做首次释放；"
    "旧 freebike1.0 只能重建，不能替换；UI 预览需另行接入代码。",
)

p_before(appendix, "6.3  方法（IMRaD：Methods）", style="Heading 2")
for text in [
    "素材核验：用 ffmpeg -i 读取 6K60 原片的分辨率、帧率、编码、码率与时长；用 Pillow 读取 30 张 Pro2 图片的像素尺寸，并解析 XMP 的 GPano 字段。",
    "背景替换：选择单目 output_1，复制为 Assets/Resources/homepage.jpg，保留旧图备份，并将导入 Max Size 提升到 8192。",
    "PC 构建：新增 Assets/Editor/BuildWindowsCampusRide.cs，使用 BuildPipeline 构建 panoTest 到 D:\\freebikeapk\\CampusRideDemo，并检查构建日志与输出目录。",
    "Android 证据核验：检查 StreamingAssets/Videos 与 APK 的 zip 条目，确认 6K60 是否被打入包内；检查旧 freebike1.0 的 freebike_Data 资源体积。",
    "路径策略：PC 端允许绝对路径外部引用；Android 端必须通过 StreamingAssets + persistentDataPath 释放，不能使用 D:\\ 路径。",
]:
    p_before(appendix, text, style="List Bullet")

p_before(appendix, "6.4  结果（IMRaD：Results）", style="Heading 2")
p_before(appendix, "6.4.1  6K60 原片与当前素材事实", style="Heading 3")
p_before(appendix, "表 6-2  6K60 原片实测规格", style="Caption")
table_before(
    appendix,
    ["项目", "实测值", "说明"],
    [
        ("分辨率", "6400×3200", "2:1 等距柱状"),
        ("帧率", "59.94 fps", "60p 档位"),
        ("编码", "H.264 High", "yuvj420p / progressive"),
        ("码率", "约 359 Mbps", "远高于普通播放码率"),
        ("文件大小", "3,166,045,173 字节（约 3.16GB）", "70.41 秒"),
        ("音频", "AAC 48kHz 4.0", "Unity 侧以 Direct 音频输出"),
        ("路径", "D:\\freebikeapk\\360vidio\\pro2\\6k60fps.mp4", "当前 PC 端外部引用"),
    ],
)

p_before(appendix, "6.4.2  Pro2 背景图判断", style="Heading 3")
p_before(
    appendix,
    "文件夹内共有 30 张 JPG：10 张 output_1–output_10 为单目 8K 等距柱状图，20 张 _Left/_Right 为左右眼立体图。"
    "全部图片均为 7680×3840、比例 2.0000、RGB，XMP 中均含 GPano:ProjectionType=equirectangular，"
    "拍摄/拼接软件为 Insta360 Pro2。因此 10 张单目图可以直接作为 360 背景；左右眼图不适合做单张背景。",
)
p_before(appendix, "表 6-3  Pro2 背景图事实", style="Caption")
table_before(
    appendix,
    ["项目", "结果", "证据"],
    [
        ("单目图", "output_1–output_10，共 10 张", "7680×3840 / 2:1"),
        ("立体图", "_Left / _Right，共 20 张", "不建议作单张背景"),
        ("投影", "equirectangular", "GPano XMP"),
        ("已选背景", "output_1.jpg", "已复制为 Assets/Resources/homepage.jpg"),
        ("导入上限", "8192", "homepage.jpg.meta"),
    ],
)

p_before(appendix, "6.4.3  PC 端交付结果", style="Heading 3")
p_before(
    appendix,
    "Windows 构建已使用脚本完成：Assets/Editor/BuildWindowsCampusRide.cs 将 panoTest 构建到 "
    "D:\\freebikeapk\\CampusRideDemo\\freebike.exe；整个目录约 1.53GB。该构建的视频为外部文件，"
    "运行时读取 D:\\freebikeapk\\360vidio\\pro2\\6k60fps.mp4，因此换视频不需要重新打 Windows 包；"
    "但改 UI、场景或背景图仍需要重建。",
)
p_before(appendix, "表 6-4  Windows 构建结果", style="Caption")
table_before(
    appendix,
    ["项目", "结果", "证据"],
    [
        ("构建脚本", "BuildWindowsCampusRide.Build", "Assets/Editor/BuildWindowsCampusRide.cs"),
        ("输出", "D:\\freebikeapk\\CampusRideDemo\\freebike.exe", "Build Finished, Result: Success"),
        ("目录大小", "约 1.53GB", "Get-ChildItem 统计"),
        ("视频位置", "外部 D 盘路径", "6k60fps.mp4"),
        ("旧 freebike1.0", "视频内嵌，不能直接替换", "sharedassets0.resource 254,268,048 字节"),
    ],
)

p_before(appendix, "6.4.4  Android APK 事实", style="Heading 3")
p_before(
    appendix,
    "当前 Android APK 只包含 bench_4k60.mp4（254MB）与 bench_57k30.mp4（482MB），没有 6k60fps.mp4；"
    "CampusRideApp 仍优先读 D 盘绝对路径，Android 上 File.Exists 返回 false，回退路径也不存在，因此 6K60 不会出现在 APK 里。"
    "要交付 Android 6K60，必须先转码、放入 StreamingAssets/Videos，并实现 Android 首次释放到 persistentDataPath。",
)

p_before(appendix, "6.4.5  UI 与自行车数据现状", style="Heading 3")
p_before(
    appendix,
    "新 UI 已生成 15 个 1600×900 预览：主页、运动 HUD、倒计时、按钮状态、运动准备、HUD 调整、暂停、运动总结、操作说明、设置、报告列表、报告详情、退出、沉浸骑行、沉浸总结。"
    "但这些仍是 PNG 预览，不是 Unity Prefab；3D 弧线按钮、双手射线悬停、准备页、HUD 调整、倒计时、报告详情尚未接入 CampusRideApp。"
    "自行车方面，panoTest 里没有启用的 3D 自行车模型；Bikerig 被隐藏；当前只有 A1 功率驱动视频播放与估算速度，没有踏频、轮速、曲柄角或转向数据。",
)

p_before(appendix, "6.5  讨论（IMRaD：Discussion）", style="Heading 2")
p_before(
    appendix,
    "第一，PC 与 Android 是两条不同的交付路径。PC 可以用 D 盘绝对路径外置引用 6K60，换视频成本最低；"
    "Android 必须把视频放进 APK 或首次释放到应用目录，不能使用 D 盘路径。把两者混在一起，就会出现“PC 正常、APK 没视频”的假象。",
)
p_before(
    appendix,
    "第二，6K60 原片 359 Mbps 的 H.264 对解码器和磁盘都是高负载。上一轮已验证 H.264 硬解通常受 4096×4096 限制，"
    "6.4K H.264 可能落入软解；因此 6K60 应先验证播放，必要时转 HEVC 或降低码率，再谈清晰度。",
)
p_before(
    appendix,
    "第三，旧 freebike1.0.exe 不能补丁式替换视频。其 freebike_Data/sharedassets0.resource 为 254,268,048 字节，"
    "正好对应旧 4K 视频；视频是 Unity VideoClip 资源，不是外置 MP4。正确做法是使用新的 CampusRideDemo 构建，或重新打包。",
)
p_before(
    appendix,
    "第四，新 UI 仍是设计资产而非运行功能。预览图可以指导布局，但不能替代 Prefab、XR Interaction Toolkit 射线、HUD 根节点与倒计时状态机；"
    "在接入前，现有 CampusRideUI.prefab 仍应视为旧版。",
)

p_before(appendix, "6.6  行动项（MECE，按优先级）", style="Heading 2")
p_before(appendix, "表 6-5  2026-10-29 增补行动项", style="Caption")
table_before(
    appendix,
    ["优先级", "类别", "动作", "验收标准"],
    [
        ("P0", "PC 视频", "用 CampusRideDemo 播放 6K60；若卡顿，转 6K60 HEVC/降码率。", "连续播放不中断，任务管理器可见硬解。"),
        ("P0", "旧包边界", "停止用 freebike1.0 替换视频；固定使用 CampusRideDemo 或重建。", "不再出现“换 MP4 没效果”。"),
        ("P1", "Android", "转 60 秒 120–150Mbps 版本；放 StreamingAssets；实现首次释放；重打 APK。", "APK 安装后无需 D 盘即可播放 6K60。"),
        ("P1", "UI", "把 15 个预览接入 Prefab + XRI 双手射线、HUD/准备/倒计时/报告详情。", "真机可射线悬停、确认、返回。"),
        ("P1", "自行车", "决定是否加入 3D 车把/模型；明确功率、踏频、轮速、曲柄映射。", "模型可见且数据驱动，不伪造踏频。"),
        ("P2", "交付", "补充 Windows/Android 两套启动说明与路径检查脚本。", "换机可按文档复现。"),
    ],
)

p_before(appendix, "6.7  一句话总结", style="Heading 2")
p_before(
    appendix,
    "PC 端已经可以直接引用 6K60，Pro2 的 8K 单目图也已经可以替换主页背景；但 Android APK 还没有 6K60，"
    "旧的 freebike1.0.exe 也不能直接换视频，新 UI 仍只是预览。下一步应按“PC 验证 → Android 内嵌 → UI 接入”的顺序推进。",
)

# ---------------------------------------------------------------------------
# 3) Update report version cell if present
# ---------------------------------------------------------------------------
try:
    t = doc.tables[0]
    if len(t.rows) > 1 and len(t.columns) > 1 and "报告版本" in t.rows[1].cells[0].text:
        t.rows[1].cells[1].text = "v1.1（2026-10-29 增补版）"
except Exception:
    pass

doc.save(DOC)

# Verify
check = Document(DOC)
print("paragraphs", len(check.paragraphs), "tables", len(check.tables))
print("heading6", any(p.text.strip().startswith("6  2026-10-29 增补") for p in check.paragraphs))
print("backup", BACKUP)
