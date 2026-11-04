# FreeBike

PICO 4 Pro + Windows PC + ThinkRider A1 的校园 360° 沉浸式骑行 Demo。

## 当前主形态

```text
Windows PC VR
PICO 4 Pro
PICO 互联 / PICO Connect
SteamVR + OpenXR
Unity 6000.6.6f1
```

主场景：

```text
Assets/Scenes/panoTest.unity
```

骑行数据：

```text
A1 骑行台 -> BLE CPS 0x1818/0x2A63 -> a1_bridge.py -> UDP 50506 -> PanoBikeSpeed
```

视频：

```text
6K60 原片：Media/Video/6k60fps.mp4
优化版：Media/Video/6k60fps_hevc.mp4
```

> 视频、APK、Windows 构建、Unity Library 不进入 Git 仓库；它们保留在本机 `Media/`、`Releases/`，通过 GitHub Releases 或外部网盘分发。

## 目录

```text
Assets/                 Unity 资源、脚本、场景、UI Prefab
Packages/               Unity 包与 PICO SDK
ProjectSettings/        Unity 工程设置
Tools/                  A1 桥接、Python 依赖、构建工具
Docs/                   AI 文档、工作日志、报告、运行时报告
Design/                 UI 预览、参考图、模型
Media/                  6K 视频、全景图（Git 忽略）
NativePlugins/          Android BLE 原生插件源码
Releases/               最新两个 Windows / Android / Benchmark 构建（Git 忽略）
```

最新构建（本机）：

```text
Windows   Releases\Windows\demo_2026-10-27\freebike.exe
          上一版 Releases\Windows\freebike2.0_2026-10-26\FreeBike.exe
Android   Releases\Android\testbike1.9.apk
          上一版 Releases\Android\testbike1.8.apk
Benchmark Releases\Benchmark\PicoVideoBenchmark.apk
```

## 打开工程

1. Unity Hub 打开 `D:\freebike`。`C:\Users\admin\freebike` 只是兼容 Junction；原 `D:\freebikeapk` 已删除，SMB 共享 `FreebikeAPK` 现指向 `D:\freebike\Releases\Android`。
2. Unity 版本：`6000.6.6f1`。
3. 打开 `Assets/Scenes/panoTest.unity`。
4. 先 Play 检查菜单与 6K 视频；PC VR 需先启动 PICO 互联和 SteamVR。

## 构建

Windows：

```text
FreeBike -> Build Windows Campus Ride
脚本默认输出：D:\freebike\Releases\Windows\CampusRideDemo\freebike.exe
现有最新归档：D:\freebike\Releases\Windows\demo_2026-10-27\freebike.exe
上一版归档：D:\freebike\Releases\Windows\freebike2.0_2026-10-26\FreeBike.exe
```

Android：

```text
File -> Build Profiles -> Android
Scene List 只勾选 Assets/Scenes/panoTest.unity
Build App Bundle 关闭
输出：D:\freebike\Releases\Android\testbike<版本>.apk
```

## 数据

- 报告目录：`Docs/RuntimeReports/CyclingReport`
- 历史工作日志：`Docs/WorkLogs`
- 最新技术报告：`Docs/Reports/FreeBike_工作报告_2026-10-29.docx`

## 已知限制

- 6K60 原片为 6400×3200 / H.264 / 359 Mbps，PC 端硬解能力需实测。
- 当前新 UI 预览在 `Design/UI`，尚未全部接入 Unity。
- Android APK 尚未包含 6K60；Android 不能使用 D 盘绝对路径。
- `freebike1.0.exe` 的视频是内嵌 VideoClip，不能直接替换 MP4。
- 自行车 3D 模型和踏频/轮速数据尚未接入。

## 许可与素材

第三方素材、PICO SDK、字体和 Pro2 原片的使用范围以各自许可为准；大型媒体不随 Git 仓库分发。

公开仓库前必须确认：

- `Packages/com.unity.xr.picoxr`、`Packages/xyz.staggartcreations.skyboxes`、`Assets/Urban_Props_Pack_Rozity`、`Assets/BrokenVector`、`Assets/Polyeler` 是第三方 SDK / Asset Store 素材，可能不允许再分发。若许可不允许，请改用私有仓库，或将它们从 Git 排除并在文档中说明获取方式。
- `Media/`、`Releases/`、`Tools/deps/`、`Tools/ffmpeg/`、`Library/` 已被 `.gitignore` 排除，不要强行提交。
