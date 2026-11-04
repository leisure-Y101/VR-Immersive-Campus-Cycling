# -*- coding: utf-8 -*-
"""Generate diagrams for the FreeBike handover document (项目资料)."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_BOLD = r"C:\Windows\Fonts\msyhbd.ttc"

W, H = 2200, 1320
BG = "#F7FAFC"
INK = "#0B1F33"
MUTED = "#52677A"
TEAL = "#0E7490"
BLUE = "#1D4ED8"
AMBER = "#B45309"
LINE = "#D7E2EA"


def font(size: int, bold: bool = False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT, size)


def rounded(draw, xy, fill, outline, width=3, radius=18):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def center_text(draw, box, text, f, fill, line_gap=8):
    x0, y0, x1, y1 = box
    lines = text.split("\n")
    heights = []
    widths = []
    for line in lines:
        bb = draw.textbbox((0, 0), line, font=f)
        widths.append(bb[2] - bb[0])
        heights.append(bb[3] - bb[1])
    total_h = sum(heights) + line_gap * (len(lines) - 1)
    y = y0 + (y1 - y0 - total_h) / 2
    for line, w, h in zip(lines, widths, heights):
        draw.text((x0 + (x1 - x0 - w) / 2, y), line, font=f, fill=fill)
        y += h + line_gap


def arrow(draw, start, end, color, width=5, dashed=False):
    if dashed:
        # Simple dashed line with triangular head.
        import math

        x1, y1 = start
        x2, y2 = end
        dist = math.hypot(x2 - x1, y2 - y1)
        if dist < 1:
            return
        ux, uy = (x2 - x1) / dist, (y2 - y1) / dist
        step = 18
        t = 0
        while t < dist - 20:
            a = (x1 + ux * t, y1 + uy * t)
            b = (x1 + ux * min(t + 10, dist - 20), y1 + uy * min(t + 10, dist - 20))
            draw.line([a, b], fill=color, width=width)
            t += step
    else:
        draw.line([start, end], fill=color, width=width)
    # arrow head
    import math

    angle = math.atan2(end[1] - start[1], end[0] - start[0])
    size = 16
    left = (end[0] - size * math.cos(angle - 0.45), end[1] - size * math.sin(angle - 0.45))
    right = (end[0] - size * math.cos(angle + 0.45), end[1] - size * math.sin(angle + 0.45))
    draw.polygon([end, left, right], fill=color)


def lane_header(draw, y, title, subtitle, color):
    draw.ellipse((90, y - 9, 108, y + 9), fill=color)
    draw.text((125, y - 22), title, font=font(34, True), fill=INK)
    draw.text((125, y + 16), subtitle, font=font(23), fill=MUTED)


def box(draw, box_xy, title, sub, color, title_size=25, sub_size=19):
    rounded(draw, box_xy, "white", color, 3, 16)
    x0, y0, x1, y1 = box_xy
    if sub:
        center_text(draw, (x0 + 10, y0 + 8, x1 - 10, y0 + 58), title, font(title_size, True), INK)
        center_text(draw, (x0 + 10, y0 + 58, x1 - 10, y1 - 8), sub, font(sub_size), MUTED, 4)
    else:
        center_text(draw, box_xy, title, font(title_size, True), INK)


def make_architecture():
    img = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W, 8), fill=TEAL)
    d.text((80, 48), "FreeBike 系统总览：三条链路，一个当前主线", font=font(48, True), fill=INK)
    d.text((82, 112), "截至 2026-11-03 · 当前构建只把「视频链」和「VR 显示链」接入了主流程；姿态/转向链已实现但未接入 panoTest。",
           font=font(24), fill=MUTED)

    # Lane 1: data
    y = 190
    lane_header(d, y, "① 骑行数据链（已打通）", "ThinkRider A1 → BLE CPS → Python 桥接 → UDP 50506 → Unity 视频播放控制", TEAL)
    y0, y1 = y + 55, y + 215
    bw, bh = 270, 105
    xs = [80, 390, 700, 1010, 1320, 1630]
    labels = [
        ("ThinkRider A1", "功率台 / BLE\nCPS 0x1818 · 0x2A63"),
        ("a1_bridge.py", "Python + bleak\n解析瞬时功率"),
        ("UDP 50506", "127.0.0.1\nASCII 浮点功率"),
        ("PanoBikeSpeed", "超时 2 s\n≥5 W 才播放"),
        ("VideoPlayer", "播放 / 暂停\n倍速 1.00–1.25×"),
        ("PanoSphere", "约 800 m 直径\n内表面 / 6K 视频"),
    ]
    for x, (t, s) in zip(xs, labels):
        box(d, (x, y0, x + bw, y0 + bh), t, s, TEAL)
    for i in range(len(xs) - 1):
        arrow(d, (xs[i] + bw + 8, y0 + bh / 2), (xs[i + 1] - 8, y0 + bh / 2), TEAL)
    d.text((82, y1 - 34), "注意：当前主线只解析功率，不解析踏频/轮速；也没有阻力控制（FTMS 0x2AD9）。",
           font=font(20), fill=MUTED)

    # Lane 2: VR
    y = 475
    lane_header(d, y, "② VR 显示链（当前主线）", "PICO 4 Pro ← Wi-Fi 6 / PICO Connect ← SteamVR + OpenXR ← Unity URP 相机", BLUE)
    y0, y1 = y + 55, y + 215
    xs = [80, 390, 700, 1010, 1320, 1630]
    labels = [
        ("PICO 4 Pro", "A8110 · 2160×2160/眼\n72 Hz · 105°"),
        ("PICO Connect", "10.6.6\n无线串流 / USB 备用"),
        ("SteamVR", "2.18.4\nOpenXR Runtime"),
        ("Unity XR", "OpenXR 1.18.0\nXR Management 4.6.1"),
        ("Main Camera", "HeadPoseDriver\n6DoF 头追"),
        ("头显画面", "全景视频 / 主页\n不要停在 SteamVR 影院层"),
    ]
    for x, (t, s) in zip(xs, labels):
        box(d, (x, y0, x + bw, y0 + bh), t, s, BLUE)
    for i in range(len(xs) - 1):
        arrow(d, (xs[i] + bw + 8, y0 + bh / 2), (xs[i + 1] - 8, y0 + bh / 2), BLUE)
    d.text((82, y1 - 34), "启动顺序必须是：PICO Connect → SteamVR 就绪 → 再启动 freebike.exe；OpenXR 只初始化一次。",
           font=font(20), fill=MUTED)

    # Lane 3: Kinect / steering
    y = 760
    lane_header(d, y, "③ 姿态 / 转向链（代码已实现，未接入当前主线）", "Azure Kinect DK → PC 骨骼服务 → UDP 50505 → 旧 3D 分支 / 未来 3D 路口", AMBER)
    y0, y1 = y + 55, y + 215
    bw2 = 300
    xs = [80, 430, 780, 1130, 1480, 1830]
    labels = [
        ("Azure Kinect DK", "Depth NFOV\n30 fps · USB 3.0"),
        ("KinectSteering.exe", "DirectML 骨骼\n约 26 fps"),
        ("UDP 50505", "FBSTEER|...\n广播 / 点对点"),
        ("KinectSteeringReceiver", "Unity 接收器\n0.5 s 超时"),
        ("SteeringHub", "ISteeringSource\n−1 左 / +1 右"),
        ("3D 路口 / 车把", "尚未实现\n转向传感器待接入"),
    ]
    for x, (t, s) in zip(xs, labels):
        box(d, (x, y0, x + bw2, y0 + bh), t, s, AMBER, 24, 18)
    for i in range(len(xs) - 1):
        arrow(d, (xs[i] + bw2 + 8, y0 + bh / 2), (xs[i + 1] - 8, y0 + bh / 2), AMBER, 4, dashed=(i >= 3))
    d.text((82, y1 - 34), "现状：panoTest 不读取 SteeringHub；Kinect 服务只服务旧低模分支/后续路口原型。",
           font=font(20), fill=MUTED)

    # bottom note
    d.rounded_rectangle((80, 1120, W - 80, 1245), radius=16, fill="#EAF6F8", outline="#B8E3EA", width=2)
    d.text((105, 1142), "当前最小演示流程", font=font(25, True), fill=TEAL)
    d.text((105, 1183), "8K 主页（开始） → 6K 全景视频（A1 有功率才播、停踩暂停） → 按住 B 0.25 s 暂停 → 继续 / 退出（退出即关闭应用）",
           font=font(22), fill=INK)
    d.text((W - 310, 1270), "FreeBike · 项目资料", font=font(19), fill="#8AA0B2")

    img.save(OUT / "architecture.png", quality=95)


def make_flow():
    W2, H2 = 2200, 980
    img = Image.new("RGB", (W2, H2), BG)
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W2, 8), fill=TEAL)
    d.text((70, 42), "当前主流程（panoTest · CampusRideApp）", font=font(44, True), fill=INK)
    d.text((72, 100), "三段式状态机：主页 / 骑行 / 暂停。没有 HUD、设置、报告、语言、Grip 或射线菜单。", font=font(23), fill=MUTED)

    # states
    boxes = [
        (80, 230, 420, 430, "主页 Home", "8K 等距柱状全景\n开始按钮跟随头部", TEAL),
        (600, 230, 940, 430, "骑行 Riding", "6K 全景视频\n无 HUD，只有画面", TEAL),
        (1120, 230, 1460, 430, "暂停 Paused", "摇杆上下选择\nTrigger 确认", AMBER),
    ]
    for x0, y0, x1, y1, t, s, c in boxes:
        rounded(d, (x0, y0, x1, y1), "white", c, 4, 20)
        center_text(d, (x0 + 15, y0 + 20, x1 - 15, y0 + 95), t, font(31, True), INK)
        center_text(d, (x0 + 20, y0 + 105, x1 - 20, y1 - 20), s, font(21), MUTED, 6)

    # Home -> Ride
    arrow(d, (428, 330), (592, 330), TEAL)
    center_text(d, (430, 240, 590, 315), "Trigger\nEnter / Space", font(16), TEAL, 3)

    # Ride -> Paused
    arrow(d, (948, 330), (1112, 330), AMBER)
    center_text(d, (950, 240, 1110, 315), "按住 B\n≥ 0.25 s", font(16), AMBER, 3)

    # Paused -> Continue (back to ride)
    arrow(d, (1112, 395), (948, 395), TEAL)
    center_text(d, (952, 405, 1108, 440), "继续", font(17), TEAL)

    # Paused -> Exit
    arrow(d, (1468, 330), (1632, 330), "#B91C1C")
    center_text(d, (1470, 275, 1630, 315), "选择退出", font(16), "#B91C1C")

    rounded(d, (1640, 230, 2120, 430), "#FDF2F2", "#B91C1C", 4, 20)
    center_text(d, (1660, 255, 2100, 340), "退出 Exit", font(31, True), "#991B1B")
    center_text(d, (1665, 345, 2095, 415), "调用 Application.Quit()\n关闭应用，不返回主页", font(21), MUTED, 5)

    # conditions to video
    rounded(d, (550, 530, 990, 665), "#FFF8E8", AMBER, 3, 16)
    center_text(d, (570, 545, 970, 650), "视频播放条件\nA1 功率 ≥ 5 W 且 2 s 内有 UDP 包", font(25, True), INK, 8)
    arrow(d, (720, 440), (720, 522), AMBER)
    center_text(d, (735, 460, 930, 495), "进入骑行后", font(17), AMBER)

    # bottom notes
    d.rounded_rectangle((70, 730, 2130, 890), radius=16, fill="#EAF6F8", outline="#B8E3EA", width=2)
    d.text((95, 750), "关键实现位置", font=font(24, True), fill=TEAL)
    d.text((95, 792), "CampusRideApp.cs：状态机、8K 主页、视频路径、暂停面板、退出；PanoBikeSpeed.cs：UDP 50506、功率→倍速、自动暂停；InputRouter.cs：Trigger/B/摇杆。",
           font=font(19), fill=INK)
    d.text((95, 834), "无 A1 时：进入骑行会停在首帧（当前 allowKeyboardFallback=false，这是设计行为）；桌面调试可临时改为 true 或用 UDP 探针。",
           font=font(18), fill=MUTED)
    d.text((W2 - 300, 946), "FreeBike · 项目资料", font=font(18), fill="#8AA0B2")
    img.save(OUT / "flow.png", quality=95)


if __name__ == "__main__":
    make_architecture()
    make_flow()
    print("saved:", OUT / "architecture.png")
    print("saved:", OUT / "flow.png")
