# PICO Local Video Migration — 2026-10-25

## 已完成（代码/资源准备，真机验证待完成）

- 备份原 PC 场景与关键脚本到此目录。
- 新增 `Assets/Scripts/Video/IVideoBackend.cs`
- 新增 `Assets/Scripts/Video/UnityVideoBackend.cs`
- 新增 `Assets/Scripts/Video/PackagedVideoInstaller.cs`
- 新增 `Assets/Scripts/Video/PicoVideoBenchmark.cs`
- 新增独立场景 `Assets/Scenes/PicoVideoBenchmark.unity`
- 新增 `Assets/Editor/PicoBenchmarkBuilder.cs`
- 新增 `Assets/StreamingAssets/Videos/bench_4k60.mp4`（3840×1920, 59.94fps, H.264, ~84.6Mbps）
- 新增 `Assets/StreamingAssets/Videos/bench_57k30.mp4`（5760×2880, 29.97fps, HEVC, ~121Mbps）

正式场景 `panoTest.unity` 未改；当前 PC 版保持不变。

## 使用方法

1. 打开 Unity，等待导入两个大视频。
2. 菜单：`FreeBike/PICO/Create Local Video Benchmark Scene` 可重建测试场景。
3. 菜单：`FreeBike/PICO/Build Benchmark APK` 构建到：
   `D:\freebikeapk\Builds\PicoVideoBenchmark.apk`
4. 安装 APK 到 PICO 4 Pro，运行后：
   - Trigger / A：切换 4K60 / 5.7K30
   - B：暂停/继续
   - Primary：切换 0.9 / 1.0 / 1.1 倍速
   - OnGUI 面板显示实际解码尺寸、XR 眼纹理尺寸、GFX 类型、错误。

## 播放路径

`StreamingAssets/Videos` → 首次运行复制到
`Application.persistentDataPath/FreeBikeVideos/v1/` →
校验大小、SHA-256，写 `.ready` →
`Unity VideoPlayer` 用 `MaterialOverride` 写到球体 `_BaseMap`。

没有使用普通 `File.Exists` 判断 APK 内 StreamingAssets；Android 包内路径通过
`UnityWebRequest` + `DownloadHandlerFile` 流式释放。

## 仍需真机验证

- 5.7K HEVC 在 PICO 4 Pro 的硬件解码与帧调度。
- 4K60 / 5.7K30 的实际解码尺寸、XR 每眼尺寸、热机 30–60 分钟。
- 首次释放耗时、APK 体积、存储空间。
- 当前后端是 Unity VideoPlayer；AVPro/OES 路径尚未授权/安装，未做虚假实现。
- A1 局域网转发、完整应用迁移和最终默认规格尚未开始。

## 回退

原始文件备份在本目录；正式场景仍为 `C:\Users\admin\freebike\Assets\Scenes\panoTest.unity`。
删除新增场景/脚本和 `Assets/StreamingAssets/Videos` 即可回到改造前状态。
