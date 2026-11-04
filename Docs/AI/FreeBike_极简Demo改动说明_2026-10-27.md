# FreeBike 极简 Demo 改动说明（2026-10-27）

## 1. 目标

在原 CampusRideApp 基础上做减法，只保留：

- **主页**：8K 全景背景 + 一个「开始」按钮。
- **骑行**：点开始后直接播放 6K 全景视频，不显示 HUD、心率、弹幕、报告等任何内容。
- **暂停**：骑行中**按住 B**（默认 0.25 s）→ 暂停，只有「继续 / 退出」两个按钮。
- **退出**：回到主页（不退出应用）。

## 2. 流程

```
启动
 └─ 主页（8K 校园全景 + 开始）
      └─ Trigger / Enter / Space → 骑行（6K 360 视频，循环播放）
            ├─ 按住 B ≥ 0.25 s → 暂停
            │     ├─ 摇杆上下选择，Trigger 确认
            │     ├─ 继续 → 回到视频
            │     └─ 退出 → 回主页
            └─ 退出后再次开始 → 视频从 0 秒重新播放
```

## 3. 修改的文件

工程：`C:\Users\admin\freebike`

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/App/CampusRideApp.cs` | **整体重写**为 Home / Riding / Paused 三态极简流程；删除 HUD、心率模型、补水弹幕、报告、五语言、设置/帮助/报告页、Grip 调面板、手柄射线、视频调试窗口、OnGUI 调试信息 |
| `Assets/Scripts/App/InputRouter.cs` | 新增 `BackHeld`（B 键按住状态），供「按住 B 暂停」使用；其余输入逻辑不动 |
| `Assets/Scripts/PanoBikeSpeed.cs` | 无功率输入时按 `1.0x` 播放；**取消低于 5W 自动暂停**；新增 `controlPlaybackState` 开关（默认关闭，播放/暂停交给 CampusRideApp） |

原始文件备份：`D:\freebikeapk\_backup_20261027\`
修改前的工作副本：`D:\freebikeapk\.staging-20261027\`

场景 `Assets/Scenes/panoTest.unity` **没有改**：CampusRideApp 仍挂在原物体上，新字段用默认值；旧的序列化字段会被 Unity 忽略。

## 4. 关键参数（Inspector 可调）

| 字段 | 默认 | 说明 |
|---|---|---|
| `pauseHoldSeconds` | 0.25 | 按住 B 多久触发暂停；改成 0.05 接近「按下即暂停」 |
| `startButtonOffset` | (0, -0.12, 2.2) | 开始按钮相对头显的位置/距离 |
| `startButtonSize` | (560, 132) | 按钮尺寸（UI 像素，scale=0.001 → 0.56 m 宽） |
| `pausePanelOffset` | (0, 0, 1.6) | 暂停面板距离 |
| `pauseDimAlpha` | 0.62 | 暂停时背景压暗程度 |
| `packagedVideoRelativePath` | `pro2/6k60fps.mp4` | StreamingAssets 内的优先路径 |
| `externalVideoPath` | `D:\freebikeapk\360vidio\pro2\6k60fps.mp4` | 包内找不到时的回退路径（开发机） |
| `loopVideo` | true | 视频循环 |

视频查找顺序：

1. `<Build>_Data/StreamingAssets/pro2/6k60fps.mp4`
2. `<Build>_Data/StreamingAssets/6k60fps.mp4`
3. `D:\freebikeapk\360vidio\pro2\6k60fps.mp4`

## 5. 打包步骤

1. Unity 6000.6.6f1 打开 `C:\Users\admin\freebike`，等编译完成（Console 不应有 `error CS`）。
2. Build Settings / Build Profiles：场景只勾 `Assets/Scenes/panoTest.unity`。
3. 构建 Windows 版到目标目录（建议新目录，别覆盖旧包）。
4. 构建完成后，把 `D:\freebikeapk\360vidio\pro2\6k60fps.mp4` 复制到
   `<构建目录>_Data\StreamingAssets\pro2\6k60fps.mp4`（目录不存在就新建）。
   - 不复制也能跑：会回退到 D 盘路径（只在本机有效）。
5. A1 骑行台：需要时先运行 `D:\freebikeapk\启动骑行台桥接.cmd`，保持窗口不关。
6. 头显：PICO 互联 / PICO Connect + SteamVR，进入应用后手柄 Trigger 点「开始」。

## 6. 验证情况

- 用工程自带的 `Assembly-CSharp.csproj` + MSBuild 做了完整 C# 编译校验：**0 error**（32 warning 均来自 PICO SDK 等既有代码）。
- 场景里有一个完全重复的 `PanoSphere (1)`（同样带 VideoPlayer + PanoBikeSpeed）。脚本在 `Awake()` 里把它关掉，避免两个 800 m 球重叠、两个 PanoBikeSpeed 抢 UDP 50506；不需要手动改场景。
- 未在 Unity 编辑器 / 头显内实跑，需你打开 Unity 编译、打包后实测：
  - 主页 8K 背景是否正常显示（不是黑底）；
  - 开始后视频是否立即播放、循环；
  - 按住 B 是否在 0.25 s 左右暂停；
  - 暂停面板摇杆能否在「继续 / 退出」间切换，Trigger 是否生效；
  - 退出后是否回主页，再次开始是否从 0 秒播放。

## 7. 注意事项 / 可再删的东西

- **2026-10-27 已清理旧素材**：`Assets/StreamingAssets/nighttest360vidio.mp4`（460 MB）和 `Assets/StreamingAssets/Videos/`（703 MB）已移出工程，备份在 `D:\freebikeapk\_backup_20261027\removed-streaming-assets\`。新包因此少约 1.16 GB；`PicoVideoBenchmark` 场景会缺这两段视频（该场景本来就不在构建列表里）。
- 现在 `Assets/StreamingAssets/` 是空的：**打包后如果要自带视频，必须把 `6k60fps.mp4` 复制到 `<构建目录>_Data\StreamingAssets\pro2\`**；不复制则回退到 `D:\freebikeapk\360vidio\pro2\6k60fps.mp4`。
- `Assets/Video/nighttest360vidio.mp4`（242 MB）是另一份未被构建引用的副本（只有 `_Recovery` 备份场景引用），不影响包体，想进一步瘦工程可以删。
- `CampusRideUI.prefab`、`CampusRideUI.cs`、`VideoTrafficOverlay.cs`、`FreeBikeControllerRays.cs` 等旧 UI/功能文件都还在，只是不再被调用；确认不需要后再删。
- A1 现在只影响播放倍速（1.00 → 1.25），**不会**因为不踩而暂停；想完全不接 A1，把 `PanoBikeSpeed.maxPlayback` 设为 1 即可。
- 暂停面板默认没有标题，只有两个按钮；需要「已暂停」标题或操作提示，可在 `BuildPauseUi()` 里加一行 `CreateText(...)`。
