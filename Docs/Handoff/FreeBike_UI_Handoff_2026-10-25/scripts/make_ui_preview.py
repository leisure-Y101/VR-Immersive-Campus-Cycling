# -*- coding: utf-8 -*-
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps

W, H = 1600, 900
S = 2
OUT = Path(r"D:\freebikeapk\UI\preview")
OUT.mkdir(parents=True, exist_ok=True)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"
FONT_L = r"C:\Windows\Fonts\msyhl.ttc"

CYAN = (34, 211, 238)
CYAN_DIM = (21, 130, 155)
WHITE = (238, 247, 252)
MUTED = (150, 190, 205)
DARK = (4, 15, 26)
PANEL = (5, 20, 33)
RED = (255, 77, 77)


def font(path, size):
    return ImageFont.truetype(path, int(size * S))


def pt(v):
    return int(v * S)


def box(x, y, w, h):
    return (pt(x), pt(y), pt(x + w), pt(y + h))


def background():
    p = Path(r"D:\freebikeapk\360vidio\homepage.jpg")
    if p.exists():
        im = Image.open(p).convert("RGB")
        # equirectangular: center crop 16:9
        im = ImageOps.fit(im, (W * S, H * S), method=Image.Resampling.LANCZOS)
        im = im.filter(ImageFilter.GaussianBlur(2.2 * S))
        return im.convert("RGBA")
    return Image.new("RGBA", (W * S, H * S), (8, 20, 32, 255))


def overlay(base, alpha=150, left_alpha=235):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle((0, 0, base.width, base.height), fill=(2, 9, 17, alpha))
    # left readability gradient
    grad = Image.new("L", (base.width, 1), 0)
    gd = ImageDraw.Draw(grad)
    for x in range(base.width):
        t = x / base.width
        gd.point((x, 0), fill=int(left_alpha * max(0.0, 1.0 - t * 1.8)))
    grad = grad.resize((base.width, base.height))
    dark = Image.new("RGBA", base.size, (2, 9, 17, 255))
    dark.putalpha(grad)
    layer = Image.alpha_composite(layer, dark)
    return Image.alpha_composite(base, layer)


def chamfer_panel(draw, x, y, w, h, cut=26, fill=(5, 20, 33, 220), outline=(30, 110, 130, 255), width=2):
    c = cut
    pts = [
        (pt(x + c), pt(y)), (pt(x + w - c), pt(y)), (pt(x + w), pt(y + c)),
        (pt(x + w), pt(y + h - c)), (pt(x + w - c), pt(y + h)),
        (pt(x + c), pt(y + h)), (pt(x), pt(y + h - c)),
        (pt(x), pt(y + c)),
    ]
    draw.polygon(pts, fill=fill)
    draw.line(pts + [pts[0]], fill=outline, width=pt(width), joint="curve")


def glow_panel(base, x, y, w, h, cut=26, color=(34, 211, 238), strength=42):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    chamfer_panel(d, x, y, w, h, cut, fill=(0, 0, 0, 0), outline=color + (strength,), width=4)
    layer = layer.filter(ImageFilter.GaussianBlur(10 * S))
    return Image.alpha_composite(base, layer)


def text(d, xy, s, f, fill=WHITE, anchor="la"):
    d.text((pt(xy[0]), pt(xy[1])), s, font=f, fill=fill, anchor=anchor)


def icon(d, kind, cx, cy, r, color=WHITE, w=3):
    x, y = pt(cx), pt(cy)
    rr = pt(r)
    ww = max(1, pt(w))
    if kind == "headset":
        d.arc((x - rr, y - rr * 0.8, x + rr, y + rr * 1.1), 190, 350, fill=color, width=ww)
        d.rounded_rectangle((x - rr, y - rr * 0.1, x - rr * 0.35, y + rr * 0.6), radius=pt(5), outline=color, width=ww)
        d.rounded_rectangle((x + rr * 0.35, y - rr * 0.1, x + rr, y + rr * 0.6), radius=pt(5), outline=color, width=ww)
    elif kind == "bike":
        d.ellipse((x - rr, y + rr * 0.15, x - rr * 0.35, y + rr * 0.8), outline=color, width=ww)
        d.ellipse((x + rr * 0.35, y + rr * 0.15, x + rr, y + rr * 0.8), outline=color, width=ww)
        d.line((x - rr * 0.65, y + rr * 0.45, x - rr * 0.1, y - rr * 0.45, x + rr * 0.55, y + rr * 0.45), fill=color, width=ww, joint="curve")
        d.line((x - rr * 0.1, y - rr * 0.45, x + rr * 0.2, y + rr * 0.45), fill=color, width=ww)
    elif kind == "book":
        d.rounded_rectangle((x - rr * 0.85, y - rr * 0.65, x + rr * 0.85, y + rr * 0.7), radius=pt(4), outline=color, width=ww)
        d.line((x, y - rr * 0.55, x, y + rr * 0.55), fill=color, width=ww)
        d.line((x - rr * 0.65, y - rr * 0.25, x - rr * 0.16, y - rr * 0.25), fill=color, width=ww)
        d.line((x + rr * 0.16, y - rr * 0.25, x + rr * 0.65, y - rr * 0.25), fill=color, width=ww)
    elif kind == "gear":
        for a in range(0, 360, 45):
            import math
            xx = x + math.cos(math.radians(a)) * rr * 0.9
            yy = y + math.sin(math.radians(a)) * rr * 0.9
            d.line((x, y, xx, yy), fill=color, width=ww)
        d.ellipse((x - rr * 0.55, y - rr * 0.55, x + rr * 0.55, y + rr * 0.55), outline=color, width=ww)
    elif kind == "report":
        d.rounded_rectangle((x - rr * 0.75, y - rr * 0.8, x + rr * 0.75, y + rr * 0.8), radius=pt(4), outline=color, width=ww)
        for i, hh in enumerate((0.3, 0.55, 0.42)):
            bx = x - rr * 0.38 + i * rr * 0.38
            d.line((bx, y + rr * 0.55, bx, y + rr * 0.55 - rr * hh), fill=color, width=ww)
    elif kind == "exit":
        d.arc((x - rr * 0.7, y - rr * 0.75, x + rr * 0.7, y + rr * 0.75), 90, 270, fill=color, width=ww)
        d.line((x + rr * 0.1, y, x + rr * 0.95, y), fill=color, width=ww)
        d.line((x + rr * 0.65, y - rr * 0.3, x + rr * 0.95, y), fill=color, width=ww)
        d.line((x + rr * 0.65, y + rr * 0.3, x + rr * 0.95, y), fill=color, width=ww)
    elif kind == "heart":
        d.arc((x - rr, y - rr * 0.35, x, y + rr * 0.55), 120, 330, fill=color, width=ww)
        d.arc((x, y - rr * 0.35, x + rr, y + rr * 0.55), 210, 420, fill=color, width=ww)
        d.line((x - rr * 0.92, y + rr * 0.08, x, y + rr * 0.9), fill=color, width=ww)
        d.line((x + rr * 0.92, y + rr * 0.08, x, y + rr * 0.9), fill=color, width=ww)
    elif kind == "clock":
        d.ellipse((x - rr * 0.75, y - rr * 0.75, x + rr * 0.75, y + rr * 0.75), outline=color, width=ww)
        d.line((x, y, x, y - rr * 0.45), fill=color, width=ww)
        d.line((x, y, x + rr * 0.38, y + rr * 0.2), fill=color, width=ww)
    elif kind == "pin":
        d.arc((x - rr * 0.55, y - rr * 0.8, x + rr * 0.55, y + rr * 0.15), 0, 360, fill=color, width=ww)
        d.line((x - rr * 0.38, y + rr * 0.05, x, y + rr * 0.8), fill=color, width=ww)
        d.line((x + rr * 0.38, y + rr * 0.05, x, y + rr * 0.8), fill=color, width=ww)


def main_menu():
    base = overlay(background(), 145, 242)
    base = glow_panel(base, 76, 62, 1090, 776, 28, CYAN, 25)
    d = ImageDraw.Draw(base)
    chamfer_panel(d, 76, 62, 1090, 776, 28, fill=(4, 16, 27, 218), outline=(27, 98, 116, 255), width=2)

    # top chrome
    d.line((pt(112), pt(110), pt(112 + 88), pt(110)), fill=CYAN, width=pt(2))
    text(d, (112, 116), "PICO 4 PRO  /  360° CAMPUS RIDE", font(FONT_B, 11), CYAN_DIM)
    text(d, (112, 160), "沉浸式校园骑行", font(FONT_B, 44), WHITE)
    text(d, (116, 222), "PICO 4 Pro  ·  360° 校园骑行", font(FONT_L, 17), MUTED)
    d.line((pt(114), pt(260), pt(1030), pt(260)), fill=(20, 55, 70, 255), width=pt(1))

    text(d, (114, 292), "选择骑行方式", font(FONT_B, 16), (114, 190, 205))
    text(d, (980, 294), "右手射线 · Trigger 确认", font(FONT_L, 12), (92, 135, 150), anchor="ra")

    # arc cards
    labels = [
        ("纯沉浸式体验", "只看风景", "headset"),
        ("运动骑行", "记录数据", "bike"),
        ("操作说明", "", "book"),
        ("设置", "", "gear"),
        ("数据报告", "", "report"),
        ("退出", "", "exit"),
    ]
    card_w, card_h, gap = 140, 178, 16
    start_x = 128
    base_y = 420
    arc = [0, 16, 28, 28, 16, 0]
    for i, (label, desc, icon_name) in enumerate(labels):
        selected = i == 0
        ww = card_w + (8 if selected else 0)
        hh = card_h + (8 if selected else 0)
        x = start_x + i * (card_w + gap) - (4 if selected else 0)
        y = base_y + arc[i] - (4 if selected else 0)
        if selected:
            base = glow_panel(base, x, y, ww, hh, 24, CYAN, 55)
            d = ImageDraw.Draw(base)
        fill = (7, 38, 53, 238) if selected else (6, 22, 36, 225)
        outline = CYAN if selected else (24, 62, 78, 255)
        d.rounded_rectangle(box(x, y, ww, hh), radius=pt(26), fill=fill, outline=outline, width=pt(2))
        # top accent
        if selected:
            d.line((pt(x + 24), pt(y + 8), pt(x + ww - 24), pt(y + 8)), fill=CYAN, width=pt(2))
        icon(d, icon_name, x + ww / 2, y + 56, 22 if selected else 20, CYAN if selected else (142, 195, 210), 2.5)
        text(d, (x + ww / 2, y + 102), label, font(FONT_B, 16 if len(label) < 7 else 14), WHITE, "mm")
        if desc:
            text(d, (x + ww / 2, y + 128), desc, font(FONT_L, 11), (118, 165, 180), "mm")
        if selected:
            text(d, (x + ww / 2, y + hh - 20), "●  已选择", font(FONT_L, 9), CYAN, "mm")

    # footer
    text(d, (114, 766), "摇杆 / 射线选择    ·    Trigger 确认    ·    B 返回", font(FONT_L, 12), (102, 148, 162))
    text(d, (1130, 766), "v0.1  /  PLACEHOLDER BG", font(FONT_L, 10), (68, 108, 122), "ra")
    return base


def hud_screen():
    base = overlay(background(), 140, 150)
    d = ImageDraw.Draw(base)
    # top left heart
    base = glow_panel(base, 62, 54, 330, 112, 18, CYAN, 25)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle(box(62, 54, 330, 112), radius=pt(18), fill=(4, 20, 32, 220), outline=(26, 92, 110, 255), width=pt(2))
    icon(d, "heart", 104, 108, 24, (255, 96, 96), 2.5)
    text(d, (148, 74), "心率", font(FONT_B, 18), (152, 200, 214))
    text(d, (148, 100), "142", font(FONT_B, 34), WHITE)
    text(d, (220, 112), "bpm", font(FONT_L, 15), MUTED)
    text(d, (334, 148), "模拟心率", font(FONT_L, 9), (96, 140, 152), "ra")

    # bottom panel
    bx, by, bw, bh = 360, 650, 880, 190
    base = glow_panel(base, bx, by, bw, bh, 24, CYAN, 35)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle(box(bx, by, bw, bh), radius=pt(24), fill=(4, 17, 29, 226), outline=(25, 100, 118, 255), width=pt(2))
    d.line((pt(bx + 230), pt(by + 30), pt(bx + 230), pt(by + bh - 30)), fill=(22, 65, 80, 255), width=pt(1))
    d.line((pt(bx + 650), pt(by + 30), pt(bx + 650), pt(by + bh - 30)), fill=(22, 65, 80, 255), width=pt(1))

    icon(d, "clock", bx + 56, by + 65, 18, CYAN, 2)
    text(d, (bx + 86, by + 51), "时长", font(FONT_L, 13), MUTED)
    text(d, (bx + 86, by + 78), "32:17", font(FONT_B, 30), WHITE)

    text(d, (bx + bw / 2, by + 36), "当前速度", font(FONT_L, 14), (140, 190, 205), "ma")
    text(d, (bx + bw / 2, by + 84), "12.0", font(FONT_B, 64), WHITE, "mm")
    text(d, (bx + bw / 2 + 62, by + 104), "km/h", font(FONT_L, 16), MUTED, "lm")
    text(d, (bx + bw / 2, by + 158), "视频基准 12 km/h · 估算", font(FONT_L, 9), (94, 138, 152), "mm")

    icon(d, "pin", bx + bw - 246, by + 65, 18, CYAN, 2)
    text(d, (bx + bw - 206, by + 51), "距离", font(FONT_L, 13), MUTED)
    text(d, (bx + bw - 206, by + 78), "12.8", font(FONT_B, 30), WHITE)
    text(d, (bx + bw - 126, by + 92), "km", font(FONT_L, 13), MUTED)

    text(d, (62, 686), "运动骑行  ·  心率 / 时长 / 距离 / 速度", font(FONT_L, 14), (132, 178, 192))
    text(d, (1540, 686), "B 暂停", font(FONT_L, 12), (110, 155, 170), "ra")
    return base


def countdown():
    base = overlay(background(), 220, 250)
    d = ImageDraw.Draw(base)
    # big ring
    cx, cy, r = 800, 430, 118
    for rr, alpha, wd in ((r + 16, 30, 10), (r + 2, 130, 3)):
        d.ellipse(box(cx - rr, cy - rr, rr * 2, rr * 2), outline=CYAN + (alpha,), width=pt(wd))
    d.ellipse(box(cx - r, cy - r, r * 2, r * 2), fill=(4, 20, 33, 220), outline=(26, 120, 140, 255), width=pt(2))
    text(d, (cx, cy - 8), "3", font(FONT_B, 128), WHITE, "mm")
    text(d, (cx, cy + 84), "准备出发", font(FONT_B, 18), CYAN, "mm")
    text(d, (cx, cy + 138), "3 秒后开始骑行", font(FONT_L, 13), MUTED, "mm")
    # corner marks
    for x, y, dx, dy in ((80, 80, 1, 1), (1520, 80, -1, 1), (80, 820, 1, -1), (1520, 820, -1, -1)):
        d.line((pt(x), pt(y), pt(x + 54 * dx), pt(y)), fill=(25, 92, 110, 255), width=pt(1))
        d.line((pt(x), pt(y), pt(x), pt(y + 54 * dy)), fill=(25, 92, 110, 255), width=pt(1))
    text(d, (800, 760), "沉浸式校园骑行", font(FONT_B, 16), (180, 220, 230), "mm")
    return base


def button_states():
    img = Image.new("RGBA", (pt(1600), pt(900)), (4, 14, 25, 255))
    d = ImageDraw.Draw(img)
    text(d, (70, 52), "ARC BUTTON / STATES", font(FONT_B, 22), CYAN)
    text(d, (70, 88), "300 × 380  /  portrait  /  9-slice  /  hover 1.12x + 2cm", font(FONT_L, 13), MUTED)
    for i, (label, color, fill) in enumerate((("NORMAL", (24, 70, 88, 255), (6, 24, 38, 235)), ("HOVER", CYAN, (7, 42, 56, 245)))):
        x, y, w, h = 150 + i * 420, 170, 300, 380
        if i == 1:
            img = glow_panel(img, x, y, w, h, 38, CYAN, 40)
            d = ImageDraw.Draw(img)
        d.rounded_rectangle(box(x, y, w, h), radius=pt(38), fill=fill, outline=color, width=pt(2))
        d.line((pt(x + 32), pt(y + 14), pt(x + w - 32), pt(y + 14)), fill=color, width=pt(2))
        icon(d, "headset", x + w / 2, y + 128, 44, CYAN if i else (150, 200, 212), 3)
        text(d, (x + w / 2, y + 246), "纯沉浸式体验", font(FONT_B, 24), WHITE, "mm")
        text(d, (x + w / 2, y + 292), "只看风景", font(FONT_L, 15), MUTED, "mm")
        text(d, (x + w / 2, y + 334), label, font(FONT_L, 11), color, "mm")
        text(d, (x + 28, y + 22), "01", font(FONT_L, 11), color)
    text(d, (70, 660), "主菜单卡片基准：300×380；运行时按弧线排列；选中放大 1.12，向观察者凸出 2cm。", font(FONT_L, 15), MUTED)
    text(d, (70, 700), "文字由 Unity Text 动态渲染，不烘进底图。", font(FONT_L, 15), MUTED)
    return img

def save(im, name):
    im = im.convert("RGB").resize((W, H), Image.Resampling.LANCZOS) if im.size != (W, H) else im.convert("RGB")
    im.save(OUT / name, quality=95)
    print("saved", OUT / name, im.size)


save(main_menu(), "01_main_menu.png")
save(hud_screen(), "02_sport_hud.png")
save(countdown(), "03_countdown.png")
save(button_states(), "04_button_states.png")
