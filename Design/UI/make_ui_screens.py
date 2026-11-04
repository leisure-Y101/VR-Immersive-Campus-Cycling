# -*- coding: utf-8 -*-
import sys
from pathlib import Path

sys.path.insert(0, r"D:\freebike\Design\UI")
import make_ui_preview as ui
from PIL import Image, ImageDraw

OUT = Path(r"D:\freebike\Design\UI\preview")
BG = ui.background()


def bg(dim=175, left=215):
    return ui.overlay(BG.copy(), dim, left)


def panel(base, x=250, y=60, w=1100, h=780):
    base = ui.glow_panel(base, x, y, w, h, 28, ui.CYAN, 25)
    d = ImageDraw.Draw(base)
    ui.chamfer_panel(d, x, y, w, h, 28, fill=(4, 16, 27, 224), outline=(27, 98, 116, 255), width=2)
    return base


def title(d, s, sub=None, x=300, y=112):
    ui.text(d, (x, y), s, ui.font(ui.FONT_B, 34), ui.WHITE)
    if sub:
        ui.text(d, (x, y + 48), sub, ui.font(ui.FONT_L, 15), ui.MUTED)
    d.line((ui.pt(x), ui.pt(y + 82), ui.pt(x + 1000), ui.pt(y + 82)), fill=(20, 55, 70, 255), width=ui.pt(1))


def button(d, x, y, w, h, label, selected=False, icon_name=None):
    if selected:
        d.rounded_rectangle(ui.box(x - 2, y - 2, w + 4, h + 4), radius=ui.pt(16), outline=ui.CYAN, width=ui.pt(2))
    fill = (7, 38, 53, 245) if selected else (6, 24, 38, 235)
    outline = ui.CYAN if selected else (26, 66, 82, 255)
    d.rounded_rectangle(ui.box(x, y, w, h), radius=ui.pt(14), fill=fill, outline=outline, width=ui.pt(2))
    tx = x + w / 2
    if icon_name:
        ui.icon(d, icon_name, x + 34, y + h / 2, 14, ui.CYAN if selected else (150, 200, 212), 2)
        ui.text(d, (x + 58, y + h / 2), label, ui.font(ui.FONT_B, 16), ui.WHITE, "lm")
    else:
        ui.text(d, (tx, y + h / 2), label, ui.font(ui.FONT_B, 15), ui.WHITE, "mm")


def stat(d, x, y, w, h, label, value, icon_name):
    d.rounded_rectangle(ui.box(x, y, w, h), radius=ui.pt(14), fill=(6, 24, 38, 235), outline=(25, 70, 86, 255), width=ui.pt(1))
    ui.icon(d, icon_name, x + 34, y + 34, 13, ui.CYAN, 1.8)
    ui.text(d, (x + 32, y + 62), label, ui.font(ui.FONT_L, 13), ui.MUTED)
    ui.text(d, (x + 32, y + 88), value, ui.font(ui.FONT_B, 26), ui.WHITE)


def hud_group(base, x=420, y=540, scale=1.0):
    d = ImageDraw.Draw(base)
    # HR
    hw, hh = 260, 86
    d.rounded_rectangle(ui.box(x, y - 126, hw, hh), radius=ui.pt(15), fill=(4, 20, 32, 230), outline=(26, 92, 110, 255), width=ui.pt(1))
    ui.icon(d, "heart", x + 32, y - 84, 16, ui.RED, 2)
    ui.text(d, (x + 58, y - 112), "心率", ui.font(ui.FONT_L, 12), ui.MUTED)
    ui.text(d, (x + 58, y - 88), "136", ui.font(ui.FONT_B, 24), ui.WHITE)
    ui.text(d, (x + 112, y - 78), "bpm", ui.font(ui.FONT_L, 10), ui.MUTED)
    # bottom
    bx, by, bw, bh = x, y, 760, 150
    d.rounded_rectangle(ui.box(bx, by, bw, bh), radius=ui.pt(18), fill=(4, 17, 29, 230), outline=(25, 100, 118, 255), width=ui.pt(2))
    d.line((ui.pt(bx + 190), ui.pt(by + 24), ui.pt(bx + 190), ui.pt(by + bh - 24)), fill=(22, 65, 80, 255), width=ui.pt(1))
    d.line((ui.pt(bx + 560), ui.pt(by + 24), ui.pt(bx + 560), ui.pt(by + bh - 24)), fill=(22, 65, 80, 255), width=ui.pt(1))
    ui.text(d, (bx + 30, by + 34), "时长", ui.font(ui.FONT_L, 11), ui.MUTED)
    ui.text(d, (bx + 30, by + 56), "32:17", ui.font(ui.FONT_B, 26), ui.WHITE)
    ui.text(d, (bx + 345, by + 18), "当前速度", ui.font(ui.FONT_L, 11), ui.MUTED, "ma")
    ui.text(d, (bx + 345, by + 60), "12.0", ui.font(ui.FONT_B, 44), ui.WHITE, "mm")
    ui.text(d, (bx + 392, by + 76), "km/h", ui.font(ui.FONT_L, 12), ui.MUTED, "lm")
    ui.text(d, (bx + 610, by + 26), "距离", ui.font(ui.FONT_L, 11), ui.MUTED)
    ui.text(d, (bx + 610, by + 56), "12.8", ui.font(ui.FONT_B, 26), ui.WHITE)
    ui.text(d, (bx + 690, by + 68), "km", ui.font(ui.FONT_L, 11), ui.MUTED)


# 05 sport prep
def sport_prep():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "运动骑行", "调整 HUD 到舒服的位置，然后开始")
    ui.text(d, (300, 230), "HUD 预览", ui.font(ui.FONT_B, 15), ui.CYAN)
    hud_group(base, x=440, y=500)
    # dim overlay note
    ui.text(d, (840, 470), "HUD 将跟随头部，不会固定在场景里", ui.font(ui.FONT_L, 13), ui.MUTED, "mm")
    button(d, 300, 690, 260, 58, "调整 HUD", icon_name="gear")
    button(d, 580, 690, 260, 58, "开始", selected=True, icon_name="bike")
    button(d, 860, 690, 220, 58, "返回主页", icon_name="exit")
    return base


# 06 hud adjust
def hud_adjust():
    base = bg(220, 260)
    d = ImageDraw.Draw(base)
    ui.text(d, (800, 76), "调整 HUD", ui.font(ui.FONT_B, 30), ui.WHITE, "mm")
    ui.text(d, (800, 118), "右手射线指向 HUD · 按住 Grip 抓住 · 松开保存 · B 取消", ui.font(ui.FONT_L, 14), ui.CYAN, "mm")
    # highlight bounds
    d.rounded_rectangle(ui.box(390, 330, 820, 360), radius=ui.pt(18), outline=(34, 211, 238, 100), width=ui.pt(2))
    hud_group(base, x=420, y=540)
    ui.text(d, (800, 760), "松开 Grip 即保存并返回", ui.font(ui.FONT_L, 12), ui.MUTED, "mm")
    return base


# 07 pause
def pause():
    base = bg(235, 250)
    d = ImageDraw.Draw(base)
    # dim full screen
    ov = Image.new("RGBA", base.size, (2, 9, 17, 135))
    base = Image.alpha_composite(base, ov)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle(ui.box(370, 180, 860, 540), radius=ui.pt(26), fill=(4, 16, 27, 235), outline=(27, 98, 116, 255), width=ui.pt(2))
    ui.text(d, (800, 250), "已暂停", ui.font(ui.FONT_B, 36), ui.WHITE, "mm")
    ui.text(d, (800, 296), "骑行计时与视频已暂停", ui.font(ui.FONT_L, 14), ui.MUTED, "mm")
    labels = (("继续骑行", "bike"), ("调整 HUD", "gear"), ("结束骑行", "exit"))
    for i, (lab, ic) in enumerate(labels):
        x = 430 + i * 260
        y = 390
        d.rounded_rectangle(ui.box(x, y, 230, 180), radius=ui.pt(18), fill=(6, 24, 38, 235), outline=(26, 92, 110, 255), width=ui.pt(2))
        ui.icon(d, ic, x + 115, y + 70, 24, ui.CYAN, 2.2)
        ui.text(d, (x + 115, y + 128), lab, ui.font(ui.FONT_B, 16), ui.WHITE, "mm")
    ui.text(d, (800, 630), "B 继续", ui.font(ui.FONT_L, 12), ui.MUTED, "mm")
    return base


# 08 sport summary
def sport_summary():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "骑行数据", "本次运动已完成")
    stat(d, 300, 280, 300, 145, "骑行时长", "32:17", "clock")
    stat(d, 630, 280, 300, 145, "最高心率", "168 bpm", "heart")
    stat(d, 960, 280, 300, 145, "骑行距离", "12.8 km", "pin")
    ui.text(d, (300, 470), "平均速度  12.0 km/h     ·     模拟心率", ui.font(ui.FONT_L, 13), ui.MUTED)
    button(d, 300, 560, 420, 60, "生成并保存报告", selected=True, icon_name="report")
    button(d, 750, 560, 420, 60, "返回主页", icon_name="exit")
    return base


# 09 help
def help_screen():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "手柄操作说明", "PICO 4 Pro 控制器")
    # left controller
    for i, side in enumerate(("左手", "右手")):
        x = 360 + i * 430
        y = 270
        # controller body
        d.rounded_rectangle(ui.box(x, y, 130, 220), radius=ui.pt(46), fill=(7, 26, 40, 235), outline=(30, 100, 118, 255), width=ui.pt(2))
        d.ellipse(ui.box(x + 22, y + 36, 86, 86), outline=ui.CYAN, width=ui.pt(3))
        d.ellipse(ui.box(x + 40, y + 54, 50, 50), fill=(8, 52, 64, 255), outline=(44, 160, 178, 255), width=ui.pt(1))
        d.rounded_rectangle(ui.box(x + 44, y + 150, 42, 40), radius=ui.pt(10), outline=(30, 100, 118, 255), width=ui.pt(1))
        ui.text(d, (x + 65, y + 245), side, ui.font(ui.FONT_B, 16), ui.WHITE, "mm")
    # actions
    ui.text(d, (300, 540), "左手", ui.font(ui.FONT_B, 14), ui.CYAN)
    ui.text(d, (300, 568), "摇杆移动 / 选择", ui.font(ui.FONT_L, 15), ui.WHITE)
    ui.text(d, (300, 598), "Menu 返回", ui.font(ui.FONT_L, 15), ui.WHITE)
    ui.text(d, (820, 540), "右手", ui.font(ui.FONT_B, 14), ui.CYAN)
    ui.text(d, (820, 568), "射线悬停 / 交互", ui.font(ui.FONT_L, 15), ui.WHITE)
    ui.text(d, (820, 598), "Trigger 确认 · Grip 抓取", ui.font(ui.FONT_L, 15), ui.WHITE)
    button(d, 300, 660, 520, 56, "返回主页", icon_name="exit")
    return base


# 10 settings
def settings():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "设置", "语言与 HUD")
    ui.text(d, (300, 230), "语言", ui.font(ui.FONT_B, 15), ui.CYAN)
    langs = ("中文", "English", "Español", "Français", "Português")
    for i, lang in enumerate(langs):
        y = 270 + i * 52
        selected = i == 0
        d.rounded_rectangle(ui.box(300, y, 520, 44), radius=ui.pt(8), fill=(7, 38, 53, 235) if selected else (6, 24, 38, 220), outline=ui.CYAN if selected else (24, 60, 76, 255), width=ui.pt(1))
        ui.text(d, (322, y + 22), lang, ui.font(ui.FONT_B if selected else ui.FONT_L, 15), ui.WHITE if selected else (170, 205, 216), "lm")
        if selected:
            d.line((ui.pt(778), ui.pt(y + 22), ui.pt(786), ui.pt(y + 30), ui.pt(798), ui.pt(y + 10)), fill=ui.CYAN, width=ui.pt(2))
    button(d, 300, 590, 520, 56, "重置 HUD 位置", icon_name="gear")
    button(d, 300, 670, 520, 56, "返回主页", icon_name="exit")
    return base


# 11 reports list
def reports():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "数据报告", "点击一行查看详情")
    rows = [
        ("2026-10-25", "32:17", "12.8 km", "168 bpm"),
        ("2026-10-24", "28:45", "10.3 km", "162 bpm"),
        ("2026-10-23", "41:12", "15.6 km", "174 bpm"),
        ("2026-10-22", "26:37", "9.8 km", "158 bpm"),
    ]
    for i, (date, t, dist, hr) in enumerate(rows):
        y = 250 + i * 82
        d.rounded_rectangle(ui.box(300, y, 960, 64), radius=ui.pt(10), fill=(6, 23, 36, 230), outline=(24, 62, 78, 255), width=ui.pt(1))
        ui.text(d, (330, y + 32), date, ui.font(ui.FONT_L, 14), ui.WHITE, "lm")
        ui.text(d, (540, y + 32), t, ui.font(ui.FONT_L, 14), (155, 195, 208), "lm")
        ui.text(d, (700, y + 32), dist, ui.font(ui.FONT_L, 14), (155, 195, 208), "lm")
        ui.text(d, (860, y + 32), hr, ui.font(ui.FONT_L, 14), (155, 195, 208), "lm")
        ui.text(d, (1210, y + 32), "›", ui.font(ui.FONT_B, 24), ui.CYAN, "rm")
    button(d, 300, 650, 520, 56, "返回主页", icon_name="exit")
    return base


# 12 report detail
def report_detail():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "报告详情", "2026-10-25  ·  运动骑行")
    fields = [
        ("骑行时长", "32:17"), ("最高心率", "168 bpm"),
        ("平均心率", "146 bpm"), ("骑行距离", "12.8 km"),
        ("卡路里", "286 kcal"), ("平均速度", "12.0 km/h"),
        ("最高速度", "15.0 km/h"), ("心率来源", "模拟"),
    ]
    for i, (k, v) in enumerate(fields):
        col = i % 2
        row = i // 2
        x = 330 + col * 450
        y = 260 + row * 92
        ui.text(d, (x, y), k, ui.font(ui.FONT_L, 13), ui.MUTED)
        ui.text(d, (x, y + 24), v, ui.font(ui.FONT_B, 20), ui.WHITE)
    button(d, 330, 680, 420, 56, "返回报告列表", icon_name="report")
    return base


# 13 exit
def exit_modal():
    base = bg(235, 250)
    ov = Image.new("RGBA", base.size, (2, 9, 17, 155))
    base = Image.alpha_composite(base, ov)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle(ui.box(430, 250, 740, 400), radius=ui.pt(22), fill=(4, 16, 27, 240), outline=(27, 98, 116, 255), width=ui.pt(2))
    ui.icon(d, "exit", 800, 330, 26, ui.CYAN, 2.4)
    ui.text(d, (800, 390), "确定退出应用吗？", ui.font(ui.FONT_B, 26), ui.WHITE, "mm")
    ui.text(d, (800, 432), "退出后将关闭 PICO 4 Pro 应用", ui.font(ui.FONT_L, 13), ui.MUTED, "mm")
    button(d, 500, 500, 270, 56, "取消")
    button(d, 830, 500, 270, 56, "确认退出", selected=True)
    return base


# 14 immersive riding
def immersive_riding():
    base = bg(150, 100)
    d = ImageDraw.Draw(base)
    # top left warning
    d.rounded_rectangle(ui.box(52, 48, 300, 64), radius=ui.pt(16), fill=(60, 8, 18, 170), outline=(170, 40, 56, 255), width=ui.pt(1))
    ui.icon(d, "heart", 82, 80, 14, ui.RED, 2)
    ui.text(d, (108, 68), "心率超限", ui.font(ui.FONT_B, 14), (255, 170, 170))
    ui.text(d, (108, 88), "136 bpm", ui.font(ui.FONT_L, 10), (255, 130, 130))
    # top right alert
    d.rounded_rectangle(ui.box(1110, 48, 430, 100), radius=ui.pt(12), fill=(55, 8, 15, 200), outline=(190, 45, 58, 255), width=ui.pt(2))
    ui.text(d, (1140, 66), "心率过高，请注意休息", ui.font(ui.FONT_B, 17), (255, 190, 190))
    ui.text(d, (1140, 96), "请放慢踏频，调整呼吸", ui.font(ui.FONT_L, 11), (255, 150, 150))
    # no HUD, no bike
    ui.text(d, (800, 830), "沉浸式模式 · 无数据 HUD", ui.font(ui.FONT_L, 11), (150, 180, 190), "mm")
    return base


# 15 immersive summary
def immersive_summary():
    base = bg(190, 230)
    base = panel(base)
    d = ImageDraw.Draw(base)
    title(d, "骑行已结束", "本次沉浸式骑行完成")
    stat(d, 300, 300, 300, 150, "骑行时长", "32:17", "clock")
    stat(d, 630, 300, 300, 150, "最高心率", "168 bpm", "heart")
    stat(d, 960, 300, 300, 150, "骑行距离", "12.8 km", "pin")
    button(d, 300, 570, 960, 60, "返回主页", selected=True, icon_name="exit")
    return base


ui.save(sport_prep(), "05_sport_prep.png")
ui.save(hud_adjust(), "06_hud_adjust.png")
ui.save(pause(), "07_pause.png")
ui.save(sport_summary(), "08_sport_summary.png")
ui.save(help_screen(), "09_help.png")
ui.save(settings(), "10_settings.png")
ui.save(reports(), "11_reports.png")
ui.save(report_detail(), "12_report_detail.png")
ui.save(exit_modal(), "13_exit.png")
ui.save(immersive_riding(), "14_immersive_riding.png")
ui.save(immersive_summary(), "15_immersive_summary.png")
