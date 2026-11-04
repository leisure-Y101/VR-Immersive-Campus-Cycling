# FreeBike 项目交接文档（AI 续作必读）

最后更新：2026-10-29
文档位置：`D:\freebike\Docs\AI\FreeBike_项目交接文档.md`
工程位置：`D:\freebike`

> 2026-10-29 已完成迁移与目录重构：旧 `D:\freebikeapk` 已删除，`C:\Users\admin\freebike` 仍是指向 `D:\freebike` 的兼容 Junction（同一份文件，不是副本）；`FreebikeAPK` SMB 共享已改指向 `D:\freebike\Releases\Android`。视频在 `Media\`，构建在 `Releases\`，文档在 `Docs\`，工具在 `Tools\`。本文 §4 已改为新路径；其他章节中的 `D:\freebikeapk\...` 仅作历史记录。
>
> 使用方式：用户只需要说“下一步目标：XXX”。
> AI 先读本文件，再按“AI 协作规则”继续，不要重新探索工程、不要重复已验证/已失败的步骤。

---

## 0. TL;DR（30 秒版）

> **2026-10-25 起当前主线已切换到校园 360 全景 PC VR Demo（`panoTest` + `CampusRideApp`），见 §12。§1–§9 是旧 3D 工程阶段资料，仍可参考数据链路与踩坑，但场景/UI 不同。**

- **产品目标**：PICO 4 Pro 上的室内健身应用。真实自行车（ThinkRider A1 功率台）提供功率/踏频/速度；Azure Kinect 检测人体姿态与转向；未来在学校的 3D 场景里骑行/原地跑步；后期加入语音指令与 AI 教练。
- **当前状态**：PICO 端可运行，完整 UI 流程、占位自行车与轮子/脚踏动画、功率驱动、手柄调试模式、A1 BLE 框架、Kinect 骨架转向（PC 端）均已实现。最新 APK：`D:\freebike\Releases\Android\testbike1.9.apk`；上一版 `testbike1.8.apk`。
- **当前 UI 状态（2026-10-26）**：校园 360 PC VR Demo 已加入科技风独立世界空间主面板、运动准备页和主面板 Grip 平移；左右手、双手中点、平滑交接、1.6–3.2m 距离限制均已写入 `CampusRideApp` / `InputRouter`。位置不跨重启保存，待 Unity 编译和 PICO 4 Pro 实机验证。
- **当前阻塞**：等纽扣电池到货才能实测 A1 BLE；Kinect → PICO 的 UDP 链路尚未在头显内验证；双手离开车把检测被临时关闭。
- **新问题（2026-10-19）**：上/下坡明显眩晕（VIMS / 垂直 vection）。原因、修改计划与文献见 **§9**；**P0/P1 已于 2026-10-20 实现，P3 暗角待做**，需打包实测。
- **下一步（用户指定顺序）**：电池到 → A1 数据进 PICO → Kinect 骨架姿态接入 → 全链路跑通 → 换学校 3D 场景 → 加入原地跑步（PICO Motion Tracker）。
- **转向方案**：先用摄像头（Kinect 双手/肩膀）检测；不行再买转向角传感器。自行车模型继续用占位模型。

---

## 1. 目标与需求（用户原话归纳）

1. PICO 4 Pro 内运行真正的 VR 沉浸式骑行（360° 环顾，头部转动自然，不眩晕）。
2. 第一人称能看到车把；轮子/曲柄/脚踏随速度和踏频转动；默认视角高度 1.6m，可上下调 0.5m。
3. 真实自行车数据（功率/踏频/速度）通过 BLE 进入 PICO，驱动虚拟骑行与 HUD。
4. 方向控制不要手柄摇杆：优先用摄像头（Azure Kinect）检测双手/身体姿态；后期可换车把转向角传感器。
5. 手柄只用于界面操作（选择、确认、暂停）；没有传感器时提供“手柄调试模式”。
6. 后期加入：学校 3D 场景、原地跑步（PICO Motion Tracker）、语音指令、AI 健身教练、音乐。
7. 画质：不要模糊；建筑/车辆未来换高质量资产（当前是低模包）。
8. 稳定性：长时间佩戴不晕、不中断；设备断线可恢复。

---

## 2. 硬件与软件清单

### 硬件

| 设备 | 型号/信息 | 状态 |
|---|---|---|
| 头显 | PICO 4 Pro，型号 A8110，序列号 PA8210MGJ5160306G，OS 5.13.7（Android 10，GLES 3.2） | 正常 |
| 功率训练台 | ThinkRider A1（智骑 A1），蓝牙名 `ThinkRider_A1` | 等纽扣电池到货后实测 |
| 姿态摄像头 | Azure Kinect DK，序列号 MSFT20000028402912（Color 1.6.110 / Depth 1.6.79） | 正常（USB 3.0 链路较敏感） |
| PC | Windows，RTX 4060 Laptop + Intel Arc，WLAN 192.168.31.x（DHCP，曾为 .210/.212） | 正常 |
| PICO Motion Tracker | 计划用于原地跑步 | 未接入 |
| 转向角传感器 | 仅在摄像头方案失败时购买 | 未购买 |

### 软件

| 软件 | 路径/版本 |
|---|---|
| Unity Hub / Unity | 6000.6.6f1（Unity 6.6） |
| Android 构建模块 | OpenJDK + Android SDK & NDK（Unity Hub 安装） |
| adb | `C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` |
| PICO SDK | 3.4.0，嵌入包：`C:\Users\admin\freebike\Packages\com.unity.xr.picoxr` |
| XRI / XR Management | XRI 3.6.1、XR Management 4.6.1、core-utils 2.6.0（manifest 已钉版本） |
| Azure Kinect Sensor SDK | 1.4.2：`C:\Program Files\Azure Kinect SDK v1.4.2` |
| Azure Kinect Body Tracking SDK | 1.1.2：`C:\Program Files\Azure Kinect Body Tracking SDK` |
| .NET SDK | 10.0.402（编译 PC 端 Kinect 服务） |
| 手机调试工具 | nRF Connect（排查 A1 BLE 服务） |
| 文档工具 | Word/WPS |

---

## 3. 当前实现架构

### 3.1 数据链路（目标形态）

```
ThinkRider A1 ──BLE(FTMS/CPS)──> PICO(Android BLE 插件) ──> ThinkRiderBleSource ──> BikeTelemetry
                                                                        │
Azure Kinect ──USB3──> PC(KinectSteering.exe, DirectML骨骼) ──UDP:50505──> KinectSteeringReceiver ──> SteeringHub
                                                                        │
                                                              BikeKeyboardRide（速度/转向/贴地）
                                                                        │
                                                     BikeVisualRig（车轮/曲柄/脚踏）/ HUD / 结算
```

### 3.2 Unity 脚本职责（`Assets/Scripts/`）

| 文件 | 职责 |
|---|---|
| `App/RideUi.cs` | 应用主控：启动页→主菜单→设置→倒计时→骑行→暂停→结算→退出；设备连接拦截；手柄调试模式提示；Kinect 转向优先级；清晰度设置；HUD |
| `App/InputRouter.cs` | 键盘 + PICO 左右手柄（仅 UI：摇杆/确认/返回/暂停）；暴露 StickX/StickY/Trigger |
| `App/MenuPanel.cs` | 焦点式菜单（动作/数值/开关），支持标题/副标题/提示 |
| `App/UiTheme.cs` | 颜色、字体（Resources/Fonts/simhei）、圆角面板 |
| `App/RideSettings.cs` | 训练设置持久化（PlayerPrefs `fb.*`）；不参与 groundOffset |
| `App/RideSession.cs` | 时长/距离/最高速/均速统计 |
| `BikeKeyboardRide.cs` | 功率→目标速度平滑；地面贴合；空气墙；**转向=旋转整车/相机（已改回）** + 转向输入平滑 |
| `Bike/BikeVisualRig.cs` | 占位自行车（车架/车把/前叉/双轮/曲柄/脚踏）；轮子按速度、曲柄脚踏按踏频；`BindExternal()` 无缝替换真实模型 |
| `Bike/BikeTelemetry.cs` | `BikeTelemetry` 结构 + `IBikeDataSource` 接口 + 连接状态枚举 |
| `Bike/ThinkRiderParser.cs` | FTMS 0x2AD2、CPS 0x2A63 解析；曲柄→踏频、车轮→速度（含回绕） |
| `Bike/ThinkRiderBleSource.cs` | Android BLE 桥接（Java 插件消息）、连接状态、超时 |
| `Bike/SimulatedBikeSource.cs` | 编辑器模拟功率（W/S 踩踏） |
| `Bike/ControllerBikeSource.cs` | 手柄调试模式：扳机/摇杆踩踏、摇杆向下刹车 |
| `Bike/ControllerSteeringSource.cs` | 手柄调试模式右摇杆转向 |
| `Bike/HandsOnHandlebarDetector.cs` | PICO 手部追踪检测双手是否在车把上（当前默认关闭） |
| `Bike/KinectSteeringReceiver.cs` | UDP 50505 接收 `FBSTEER` 包，实现 `ISteeringSource` |
| `Bike/SteeringSensor.cs` | `ISteeringSource` + `SteeringHub` 预留接口 |
| `XR/HeadPoseDriver.cs` | 运行时读取 XR Head 姿态驱动相机；禁用旧的 TrackedPoseDriver；编辑器 1.6m 模拟 |
| `Assets/Editor/ThinkRiderManifestPostProcessor.cs` | 构建时注入 BLE + INTERNET 权限（不改主 Manifest） |

### 3.3 场景结构（当前施工场景）

`Assets/BrokenVector/LowPolyRoadPack/Demo/Low Poly Road Pack Demo.unity`

```
XR Origin (根，挂 BikeKeyboardRide，groundOffset=0，位置 0,1,-10)
 └─ Main Camera (TrackedPoseDriver 无绑定 → 运行时被 HeadPoseDriver 禁用)
PICO_Manager (PXR_Manager)
Map / 建筑 / 道路 / 天空盒 / Near Ground(Sand 材质) 等
运行时自动创建：RideUI、BikeVisual(Rig)、Cameras/UI、BLE/手柄/Kinect 数据源
```

Build Settings 只启用该场景（SampleScene 已禁用）。

### 3.4 关键参数（当前默认）

| 参数 | 值 |
|---|---|
| 最高速度 | 36 km/h（设置可调 10–60） |
| 功率→速度 | `kph = 8 + powerW × 0.16`，再按加速度/减速度平滑 |
| 转向速度 | 50°/s（`turnSpeedDeg`），乘灵敏度 0.5–1.5 |
| 转向平滑 | `Lerp(steer, 1-exp(-10·dt))` |
| 视角高度 | 1.6m 基准，设置 ±0.5m（`eyeHeightOffset`） |
| 占位车把 | 高 0.98m、前伸 0.52m、握把半宽 0.21m、轮半径 0.335m |
| 手柄调试 | 扳机/摇杆上=踩踏，摇杆下=刹车，右摇杆=转向，B=暂停 |
| Kinect 转向 | 双手连线相对肩膀连线偏角；最大 45°；死区 0.05；掉置信度保持 0.6s；默认反向 `invert=true` |
| UDP | 50505，广播 255.255.255.255 + 本机子网广播 |
| 渲染 | Mobile_RPAsset：RenderScale=1.0、MSAA=Off；MultiPass；纹理 mip 0、各向异性强制开启 |

---

## 4. 路径速查

### Unity 工程

- 工程根：`D:\freebike`
- 主场景：`D:\freebike\Assets\Scenes\panoTest.unity`
- 脚本：`D:\freebike\Assets\Scripts\`（见 §3.2）
- 编辑器脚本：`D:\freebike\Assets\Editor\`
- 资源：`Assets\Resources\Fonts\simhei.ttf`、`Assets\Resources\PXR_ProjectSetting.asset`
- URP 资产：`Assets\Settings\Mobile_RPAsset.asset`、`PC_RPAsset.asset`
- PICO 设置：`Assets\XR\Settings\PXR_Settings.asset`
- Android 插件：`Assets\Plugins\Android\ThinkRiderBle.jar`
- 插件源码：`NativePlugins\ThinkRiderBle\src\com\freebike\ble\ThinkRiderBlePlugin.java`
- 插件构建脚本：`NativePlugins\ThinkRiderBle\build.ps1`（javac + jar，输出到 Assets/Plugins/Android）
- Unity 日志：`D:\freebike\Logs\Editor.log`（打开工程后自动生成，Git 忽略）

### 构建产物 / 安装

- 构建目录：`D:\freebike\Releases\`
- Windows 最新：`Releases\Windows\demo_2026-10-27\freebike.exe`
- Windows 上一版：`Releases\Windows\freebike2.0_2026-10-26\FreeBike.exe`
- Android 最新：`Releases\Android\testbike1.9.apk`
- Android 上一版：`Releases\Android\testbike1.8.apk`
- Benchmark：`Releases\Benchmark\PicoVideoBenchmark.apk`
- SMB 共享：`FreebikeAPK` → `D:\freebike\Releases\Android`；头显文件管理“局域网”登录 Windows 账号 `admin` + 开机密码
- adb 路径见 §2

### PC 端 Kinect 转向服务

- 工程：`D:\freebike\Tools\KinectSteering`
- 源码：`Program.cs`；项目：`KinectSteering.csproj`（net10.0，引用两个 C# 封装 DLL）
- 可执行：`Tools\KinectSteering\bin\Release\net10.0\KinectSteering.exe`
- 运行日志：`...\bin\Release\net10.0\run.log`（同时控制台输出）
- Kinect 工具：`C:\Program Files\Azure Kinect Body Tracking SDK\tools`（k4arecorder / k4aviewer / k4abt_simple_3d_viewer / dnn_model_2_0_op11.onnx）
- 模型路径在代码里是绝对路径 `...\tools\dnn_model_2_0_op11.onnx`

### 文档 / 日志 / 报告

- 本交接文档：`D:\freebike\Docs\AI\FreeBike_项目交接文档.md`
- Word 版：`D:\freebike\Docs\AI\FreeBike_项目交接文档.docx`（由 md 生成）
- 历史日报 / 周报：`D:\freebike\Docs\WorkLogs\`
- 技术报告：`D:\freebike\Docs\Reports\FreeBike_工作报告_2026-10-29.docx`（上一版 `..._before_20261029_addendum.docx`）
- 报告素材与生成脚本：`D:\freebike\Docs\Reports\media\`、`Docs\Reports\_scripts\`
- 运行时报告：`D:\freebike\Docs\RuntimeReports\CyclingReport\`
- 构建 / 迁移日志：`D:\freebike\Docs\Logs\`
- 迁移记录：`D:\freebike\Docs\Migration\`
- AI 生成资料：`D:\freebike\Docs\Resources\`（安装清单、方案选型、TestPano、校园数据说明、UI 交互包）
- UI 交接包：`D:\freebike\Docs\Handoff\FreeBike_UI_Handoff_2026-10-25\`

### 媒体

- 6K60 原片：`D:\freebike\Media\Video\6k60fps.mp4`
- 优化版：`D:\freebike\Media\Video\6k60fps_hevc.mp4`
- 全景静帧：`D:\freebike\Media\Stills\`

### 网络 / 传输（可能变化）

- PC WLAN：`192.168.31.x`（DHCP，曾 .210/.212）；头显同一 Wi-Fi（SSID：VR_01）
- UDP：50505（PC→PICO 广播）、50506（A1 桥接）
- 防火墙规则（已加）：`FreebikeAPK8000`(TCP8000)、`FreebikeSMB445`(TCP445)、`FreebikeNetBIOS`(UDP137-138)
- 注意：SMB 共享对 Everyone 只读，项目暂停/交付前建议删除共享与规则

---

## 5. 已完成 / 未完成 / 已知问题

### 已完成（并验证过）

1. 应用 UI 全流程（启动页/主菜单/训练设置/倒计时/骑行 HUD/暂停/结算/退出确认）。
2. 中文界面（simhei 动态字体）；世界空间面板固定在骑行装置前方。
3. 头部追踪修复：`HeadPoseDriver` 接管相机，旧的 TrackedPoseDriver 被禁用（原因见 §6.1）。
4. PICO 4 Pro 构建与安装：包名 `com.freebike.ride`、API29、GLES3、IL2CPP/ARM64、横屏、MSAA 关。
5. 第一人称占位自行车 + 轮子/曲柄/脚踏动画；`BindExternal` 支持替换真实模型。
6. 功率驱动速度、HUD 功率/踏频；手柄只做 UI；无传感器手柄调试模式（5 秒提示）。
7. ThinkRider BLE 框架：Java 插件（扫描/连接/订阅/消息）、C# 桥接、FTMS/CPS 解析、权限注入、PXR 手部追踪配置。
8. Kinect DK 硬件验证：30 秒录制 0 错误；Body Tracking DirectML 平均 38.7ms/帧（约 26fps）。
9. PC 端骨骼转向：肩膀参考系 + 双手偏角；反向与 45° 量程已按实测调整；断线自动重试。
10. PICO 端 UDP 接收器（`KinectSteeringReceiver`）与 SteeringHub 优先级（Kinect > 手柄）。
11. 清晰度：RenderScale 0.8→1.0、纹理 mip 关闭、各向异性强制开启。
12. 转向方案回退：由“世界固定”改回“视角跟随行进方向”，并加转向平滑（用户实测认为世界固定不实用）。

### 未完成 / 待验证

1. **A1 真机 BLE**：未实际连接；功率/踏频/速度读取、断线重连未验证。等纽扣电池。
2. **Kinect → PICO UDP 端到端**：头显状态栏未确认显示“Kinect 转向已连接”；PC 端程序需保持运行；广播可能被 Wi-Fi/AP 隔离拦截，备选点对点 UDP。
3. **双手离开车把检测**：实现完成但误触发（走两步就暂停），当前 `handsPauseEnabled=false` 关闭；需修正坐标/置信度后再启用。
4. **真实自行车模型**：仍用占位；用户决定先不换。
5. **高质量场景/建筑**：当前是低模包，纹理分辨率低；真正提升需替换资产。
6. **UI 重构**：已完成科技风可编辑 Prefab；见 §12.6。
7. **语音指令 / AI 教练 / 音乐**：未实现；PICO SDK 有 `SpeechService`（ASR），推荐云端 LLM+TTS。
8. **原地跑步 + PICO Motion Tracker**：未接入。
9. **学校 3D 场景**：未导入/替换。
10. **阻力控制（FTMS 0x2AD9）**：未实现（一期只读）。
11. **一键画质优化（编辑器批处理/质量档位）**：方案已确认可做，未实现。
12. **舒适模式**（转向限速、视野收缩）：未实现；当前仅转向平滑。
13. **坡道眩晕舒适包**：P0（垂直限速/去吸附）与 P1（地面射线只认道路）已于 2026-10-20 实现，**实测眩晕 10 → 4 分**；P2 在新场景下不需要；**P3 舒适暗角仍未做**（见 §9）。

### 已知问题/风险

- PICO SDK 3.4.0 官方仅验证到 Unity 6.3，本项目用 6.6，属超范围；已修已知编译问题，运行期风险仍在。
- Azure Kinect USB 3.0 链路敏感：长线/充电线会 Code 43 或深度相机掉线；需要短认证线、直插、加固。
- PICO adb 连接不稳定（offline/掉线）；安装优先用 SMB。
- Kinect 需要看到上半身（1.5–2.5m）；下车会报 `no body`（正常）。
- TensorRT 模式在 RTX 4060 上初始化失败；用 DirectML。
- 转向输入依赖 Kinect 手/肩可见性；遮挡或置信度低时会短暂保持/回中。

---

## 6. 踩坑与失败方案（重要，避免重复）

### 6.1 “2D 大屏 / 画面贴脸 / 转头世界跟着转”
- 根因：Main Camera 上的 `TrackedPoseDriver` **没有任何输入绑定**（`m_SingletonActionBindings: []`），头部姿态从未应用。
- 修复：`HeadPoseDriver` 运行时读取 `XRNode.Head` 的 `devicePosition/deviceRotation` 并驱动相机；禁用旧的 TrackedPoseDriver。
- 注意：不要重新启用场景里的旧 TrackedPoseDriver；否则会覆盖/冲突。

### 6.2 Unity 6.6 与 PICO SDK 3.4.0 兼容
- `Object.GetInstanceID()` 在 Unity 6.6 是硬错误 → 已把 `Packages/com.unity.xr.picoxr/SpatialAudio/Runtime/PXR_Audio_Spatializer_SceneGeometry.cs` 改为 `GetEntityId().GetHashCode()`（仅用于文件名）。
- 损坏的 `SpatialAudio/Samples/material/unity_logo.mat` 导致导入时 MaterialPostprocessor 空引用刷屏 → 已删除整个 `Packages/com.unity.xr.picoxr/SpatialAudio/Samples`。
- GameActivity → 必须使用 **Activity**（Player Settings → Application Entry Point），否则构建失败。
- URP + GLES3 + MultiPass 时 **MSAA 必须关闭**，否则 PICO 校验报错。
- 图形 API 只保留 **OpenGLES3**（PICO 4 + Unity 6 的 Vulkan 有已知问题）。
- 16KB 对齐警告只影响 Android 15+（PICO 4 是 Android 10），可忽略。
- TensorRT 模式初始化失败（SDK 内置 TRT 7.2 与 RTX 4060 驱动不兼容）→ 用 DirectML。

### 6.3 PICO 检视面板 NRE
- `PXR_ManagerEditor.OnInspectorGUI` 假设场景有 XRI 的 `XROrigin`；我们是自建 XR Origin 空物体 → 选中 PICO_Manager 时报 NRE。
- 已在该 SDK 编辑器脚本加 `xrOrigin != null` 保护。编辑器噪音，不影响构建。

### 6.4 Unity 编辑器噪音（可忽略）
- `KeyNotFoundException: services-gateway`（Unity Connect/Services 内部）。
- `generators.ai.unity.com` 错误（Unity AI 助手联网）。
- Sentis/Shader 警告、PICO 示例 prefab 缺脚本、MeshCollider pre-bake 警告。

### 6.5 Kinect USB 3.0
- 现象：只枚举麦克风/HID，深度相机不出现；SDK 报 `libusb unavailable` / Code 43。
- 原因：USB 2.0 链路（充电线/手机线/长线）或接触不良。
- 处理：短认证 USB3 线、直插蓝色 USB-A 3.0 口、避开扩展坞；关闭 USB 选择性暂停（powercfg）；插紧/做应力释放；拔插可恢复深度相机。
- 判断成功：设备管理器出现 `Azure Kinect Depth Camera`，`k4arecorder --list` 输出 `Index:0 ... Depth:1.6.79`。

### 6.6 PICO adb 掉线
- 现象：`offline` / 设备消失。
- 处理：头显内确认“允许 USB 调试”、保持亮屏、换后置口/短线；不稳定时改用 SMB 传输 APK、头显文件管理安装。

### 6.7 手部离开车把误触发
- 现象：上车/走两步就暂停。
- 链接：早期版本置信度/坐标处理不严；当前已整体关闭（`RideUi.handsPauseEnabled=false`）。
- 修复方向：用掌心/手腕组合、加置信度滞回、在车把附近定义空间区域、必要时改用 Kinect 手部检测。

### 6.8 转向两次改法（已试错）
- 方案 A（已回退）：转向时相机/世界完全固定，只改行进方向与车体 → 眩晕减轻但用户认为“视角与前进方向不一致、不实用”。
- 方案 B（当前）：转向旋转整车/相机，视角跟随行进方向；增加 `steer` 低通平滑（`1-exp(-10dt)`）。
- 若仍晕：降低 `turnSpeedDeg`（50→35）、增加舒适模式（转向时视野收缩/暗角）、限制最大转向速率。

### 6.9 UDP 广播注意
- PC 发送到 255.255.255.255 + 子网广播；Android 端需 `INTERNET` 权限（已注入）。
- 若头显收不到：改为发给 PICO 的具体 IP（点对点）或检查路由器 AP 隔离/访客网络。
- 状态栏显示“Kinect 转向已连接”依赖 `KinectSteeringReceiver.Available`（0.5s 内有包）。

### 6.10 其他
- 桌面重定向：用户桌面是 `D:\Users\admin\Desktop`。
- 工程内 `Packages/com.unity.xr.picoxr` 是嵌入包，修改会直接生效；升级 SDK 前备份补丁点。
- SMB 共享与防火墙规则是临时措施；交付前清理。

---

## 7. 构建 / 安装 / 测试手册

### 7.1 Unity 构建 APK
1. 打开工程 `C:\Users\admin\freebike`，等编译完成或用 Ctrl+R。
2. Console 只应有黄字；出现 `error CS` 必须先修。
3. File → Build Profiles（或 Build Settings）→ Android。
4. 确认 Scene List 仅勾选 `Low Poly Road Pack Demo`。
5. Build → 输出到 `D:\freebikeapk\testbike1.x.apk`。
6. 安装：头显文件管理 → 局域网 → PC（账号 `admin` + 开机密码）→ `FreebikeAPK` → 复制 APK → 安装；或 `adb install -r <apk>`。

### 7.2 PC 端 Kinect 服务
```powershell
cd C:\Users\admin\freebike\Tools\KinectSteering
dotnet build -c Release
# 运行（工作目录必须是 tools，便于加载模型/CUDA DLL）：
$tools = "C:\Program Files\Azure Kinect Body Tracking SDK\tools"
Start-Process ".\bin\Release\net10.0\KinectSteering.exe" -WorkingDirectory $tools
```
- 日志：`bin\Release\net10.0\run.log`
- 快捷键（控制台窗口聚焦）：`C`=重新校零，`M`=手/侧倾模式，`I`=反向，`Q`=退出
- 启动后保持直行姿势约 1.5 秒自动校零；`invert=true`（左转=负、右转=正）
- 断线会自动重试；USB 深度相机掉线时需拔插

### 7.3 快速自检
- Kinect 设备：`k4arecorder --list` 应输出 `Index:0 ... Depth:...`
- 骨骼查看器（会占用设备，先关 PC 服务）：`k4abt_simple_3d_viewer.exe`
- UDP 本机收包测试：在 PC 上监听 50505 应收到 `FBSTEER|...`
- 头显状态栏：`Kinect 转向已连接`（收到包才会显示）

### 7.4 头显内测试清单
1. 进入应用 → 启动页 → 主菜单。
2. 无 A1：等 5 秒 → 右侧“调试模式”提示 → 进入手柄模式。
3. 扳机/摇杆上踩踏 → 车速、轮子、曲柄、HUD 功率/踏频变化。
4. 转车把（Kinect）→ 视角跟随行进方向（当前行为）。
5. B 暂停 → 继续/结束/返回主菜单。
6. 结算数据（时长/距离/均速/最高速）正确。
7. 检查清晰度与头晕情况；如晕，记录出现场景（转向/加速/坡道）。

---

## 8. 下一步路线图（按用户指定顺序）

### Phase 1：A1 真机 BLE（等纽扣电池）
- 目标：`ThinkRider_A1` 扫描/连接/订阅 `0x2AD2` 与 `0x2A63`，读到功率/踏频/速度。
- 步骤：
  1. 手机 nRF Connect 确认服务（关闭 ThinkRider Tools / OnelapFit，避免单连接占用）。
  2. PICO 端确认 BLE 权限（首次会弹窗）、能扫描到设备。
  3. 验证 `ThinkRiderBleSource` 状态与数值；修解析边界。
  4. 训练开始前强制要求 A1 已连接（现有逻辑已拦截）。
  5. 数据超时 → 暂停 + 重连提示。
- 验收：HUD 功率/踏频随真实踩踏变化；轮子/曲柄同步；断线可恢复。
- 暂不做阻力控制（0x2AD9）。

### Phase 2：Kinect 骨架接入（姿态监视 + 转向）
- 目标：把已跑通的 PC 骨骼服务正式接入 PICO；状态栏可见；转向可用；同时采集姿态用于后续分析。
- 步骤：
  1. 确保 PC 程序常驻、同一 Wi-Fi。
  2. 若广播收不到 → 改为点对点 UDP（需要 PICO IP）。
  3. 头显内验证“Kinect 转向已连接”、转向方向/死区/量程。
  4. 增加姿态输出（肩/髋/手），为“双手离开车把暂停”和动作分析做准备。
- 验收：无手柄也能转向；断线自动回退手柄；不增加眩晕。

### Phase 3：全链路稳定
- 目标：A1 数据 + Kinect 转向/姿态 + UI + HUD 同时稳定运行 30–60 分钟。
- 内容：延迟测量、断线重连、权限、发热/帧率、舒适参数（转向限速/视野收缩）。
- 验收：连续骑行不中断、无明显眩晕、数据与画面一致。

### Phase 4：学校 3D 场景替换
- 目标：用学校的 3D 模型替换当前低模城市；保留骑行/跑步玩法。
- 注意：模型面数、材质、碰撞、烘焙、LOD；性能预算（XR2 目标 72fps）。
- 验收：场景可骑、可跑；帧率达标；无明显穿模/悬空。

### Phase 5：原地跑步 + PICO Motion Tracker
- 目标：新增“跑步模式”，用 Motion Tracker 识别原地跑步动作/步频。
- 内容：设备配对、骨骼/姿态输入、步频→速度映射、模式切换 UI。
- 验收：跑步动作驱动前进；与骑行模式互不干扰。

### Phase 6：体验完善
- 语音指令（PICO `SpeechService`）+ 云端 AI 教练/TTS。
- 音乐播放与 ducking。
- UI 重构（主流 3D 赛车风格）。
- 高质量资产、写实自行车模型（`BindExternal` 命名约定见 §3.2）。
- 一键画质优化（编辑器批处理 + URP 质量档位）。
- 转向角传感器备选接入 `SteeringHub`。

---

## 9. 坡道眩晕专项（下一步修改点，2026-10-19 新增）

> 用户反馈：戴头显上坡/下坡时明显眩晕，像晕机；平路相对可接受。本节记录原因、为什么这样改、以及下一步实施顺序。

### 9.1 结论（一句话）

这是**视觉诱发晕动症（VIMS / cybersickness）**：眼睛看到身体在坡道上上下运动，但前庭没有对应的俯仰/加速度。当前实现又把“垂直变化”做得过于直接（只升降不俯仰、落差直接吸附、射线可能打到桥面），所以上/下坡最明显。

### 9.2 当前实现的原因（前因后果）

| # | 原因（代码/场景） | 为什么会晕 | 表现 |
|---|---|---|---|
| 1 | 相机只升降、不俯仰（`BikeKeyboardRide.ApplyMotion` 只改 `position.y`） | 视觉有垂直运动，前庭没有俯仰/重力变化 → 视觉-前庭冲突 | 电梯感、晕机感 |
| 2 | 大落差 3m 直接吸附（`Mathf.Abs(p.y - targetY) > 3f` 时 `p.y = targetY`） | 垂直位置瞬间跳变，加速度无穷大，前庭无法预期 | 过坡顶/坡底/桥接缝时突然一沉或一抬 |
| 3 | `groundMask = ~0`，向下射线打所有层 | 可能命中桥面、上层道路、建筑，而不是脚下路面 | 高度瞬间切换、上下“弹跳” |
| 4 | 速度只由功率决定，与坡度无关（`kph = 8 + power×0.16`） | 真实上坡会减速、下坡会加速；恒速+高度变化让视觉预期与身体感受矛盾 | 上坡像被“推上去”，下坡像“飘下去” |
| 5 | 地图包含桥梁/高架/环路/跳跃等陡坡与接缝 | 坡度变化率大 → 垂直加速度大 | 强刺激，最容易晕 |
| 6 | 地面平滑只做指数插值，不限制垂直速度/加速度 | 平滑解决抖动，但不限制突变 | 起伏时“浮沉” |
| 7 | 没有舒适暗角/视野收缩 | 外围光流全量进入，vection 更强 | 周边画面拉扯感 |
| 8 | MSAA 关闭、RenderScale=1.0、低模大色块 | 边缘闪烁/抖动；若掉帧更严重 | 画面“沙沙”抖，久了更晕 |
| 9 | 头部姿态与垂直运动叠加，无 late-latching/预测调优 | 头动与画面不同步，延迟被放大 | 头动时画面“拖” |
| 10 | HUD 下倾 22°，位于视线下方 | 坡道上低头看码表，视觉冲突叠加 | 看 HUD 更晕 |
| 11 | 坐姿水平，但虚拟坡度不改变身体姿态 | 真实上坡应前倾；身体没有对应反馈 | 姿态线索矛盾 |
| 12 | IPD/佩戴/个体差异 | 双眼图像不一致或前庭敏感 | 更容易晕 |

补充说明：

- **垂直方向比水平方向更容易诱发晕动症**。水平移动时人还能靠视觉光流“接受”前进；上下坡的垂直加速度是前庭系统最敏感的部分。
- **“相机只升降”是最关键的放大因素**：真实骑行上坡时，身体和车一起俯仰，视野地平线也变化；我们只有高度变化，大脑收到的信号是“身体在电梯里上下”，而不是“在骑车爬坡”。
- **“落差吸附”是第二关键**：任何瞬时位置跳变都会造成极强的眩晕，哪怕只有几厘米。
- **速度与坡度不联动**让大脑的“运动预期”落空：上坡视觉上应该变慢，实际速度不变，冲突叠加。

### 9.3 修改计划（按优先级；每完成一步重新打包对比）

#### P0：垂直运动限速/限加速度，去掉 3m 吸附

- **改哪里**：`Assets/Scripts/BikeKeyboardRide.cs` → `ApplyMotion`
- **怎么做**：
  - 记录上一帧的 Y，目标高度变化按“最大垂直速度（建议 1.2–1.5 m/s）+ 最大垂直加速度”限幅；
  - 删除 `> 3f` 直接吸附，改为限速过渡；
  - 地面高度突变超过阈值时，宁可短暂“悬浮”或忽略，也不要瞬移。
- **为什么**：垂直加速度是晕动症最强诱因；把“无界跳变”变成“可预期的缓慢变化”，前庭系统才能接受。
- **验收**：过坡顶/坡底/桥接缝无跳变；连续上下坡 5 分钟无明显“电梯感”。

#### P1：地面射线只认道路

- **改哪里**：`BikeKeyboardRide.IsOnRoad / HasGround / ApplyMotion`，以及 `groundMask`
- **怎么做**：
  - 射线命中后校验 `hit.collider.transform.IsChildOf(roadRoot)`；不是道路则忽略，沿用上一次有效高度；
  - 或为道路单独建 Layer，把 `groundMask` 限制到该层。
- **为什么**：当前 `groundMask=~0` 会命中高架/桥面/建筑，导致高度瞬间切换；这是“突然升降”的常见来源。
- **验收**：在高架/桥梁旁骑行不再突然升高/降低。

#### P2：坡度-速度联动

- **改哪里**：`BikeKeyboardRide.TargetSpeed`
- **怎么做**：
  - 用射线法线或前后采样计算坡度角；
  - 上坡乘 0.75–0.9，下坡乘 1.05–1.2（仍受最大速度限制）。
- **为什么**：真实骑行上坡减速、下坡加速；让视觉运动与身体预期一致，减少冲突。
- **验收**：上坡明显变慢、下坡略快；主观眩晕下降。

#### P3：舒适暗角（vignette）

- **改哪里**：新建 `Assets/Scripts/XR/ComfortVignette.cs`，挂在相机上；`RideUi` 增加开关。
- **怎么做**：
  - 相机前方一块全屏黑色渐变遮罩（中心透明、边缘黑），alpha 由速度/坡度/转向强度平滑驱动；
  - 只收窄视觉边缘，不改变 FOV、不旋转画面。
- **为什么**：减少外围光流是主流 VR 舒适方案，对 vection 最直接有效。
- **验收**：开启后上下坡眩晕明显降低；可一键关闭；不影响看 HUD。

#### P4：地平线策略 A/B（可选）

- 方案 A：相机保持水平（当前），只有高度变化；
- 方案 B：只让车体/车把轻微俯仰，相机仍保持水平（视觉提示，不转视角）；
- **为什么**：真实上坡身体前倾，但让相机俯仰会引入新的视觉-前庭冲突；车体俯仰能提供线索又不转视角。
- **验收**：两种模式各骑 5 分钟，选主观更舒适者。

#### P5：帧率与清晰度权衡

- 目标稳定 72fps；若不足，RenderScale 1.0→0.95、阴影距离 50→30、关闭不必要的后处理；
- MSAA 仍按 PICO 校验关闭（URP+GLES3+MultiPass 要求）。
- **为什么**：judder 会显著放大眩晕，稳定帧率比“更清晰”更重要。

#### P6：佩戴与适应

- 检查 IPD 与佩戴松紧；短时多次使用；可加风声/胎噪、远处固定参考物、虚拟鼻子等辅助。

### 9.4 总原则（为什么不继续“把世界钉死”）

- 已试过“相机完全不转、世界固定”：眩晕下降，但视角与前进方向不一致，实用性差（用户实测），且横向滑动仍可能诱发不适。
- 正确方向是**减少“没有身体动作对应的视觉运动”**，而不是让视觉与运动脱节。
- 优先级：先处理垂直加速度与错误高度来源（P0/P1），再处理视觉预期（P2），最后用暗角兜底（P3）。

### 9.5 参考文献

1. Reason, J. T., & Brand, J. J. (1975). *Motion Sickness*. Academic Press.（sensory conflict 理论）
2. LaViola, J. J. (2000). A discussion of cybersickness in virtual environments. *ACM SIGCHI Bulletin*, 32(1), 47–56. https://doi.org/10.1145/333329.333344
3. Keshavarz, B., & Hecht, H. (2011). Validating an efficient method to quantify motion sickness. *Human Factors*, 53(4), 415–426. https://doi.org/10.1177/0018720811403736
4. Palmisano, S., et al. (2015). Future challenges for vection research. *Frontiers in Psychology*, 6:193. https://doi.org/10.3389/fpsyg.2015.00193
5. Moullec, Y., et al. (2025). To Use or Not to Use Viewpoint Oscillations When Walking in VR? *IEEE TVCG*. https://doi.org/10.1109/TVCG.2024.3436858
6. Balancing the Virtual Path: Influence of Amplitude, Frequency, and Input Method on VR Locomotion (2026). *IEEE TVCG*. https://doi.org/10.1109/TVCG.2026.3680614
7. Factors affecting vection and motion sickness in a passive virtual reality driving simulation (2024). *Scientific Reports*. https://doi.org/10.1038/s41598-024-80778-4
8. Palmisano, S., et al. (2026). Contributions of headset IPD fit, vection and sway to cybersickness. *Applied Ergonomics*, 137:104821. https://doi.org/10.1016/j.apergo.2026.104821
9. Keshavarz, B., et al. (2026). Individual-difference factors in VIMS and vection. *J Vestib Res*, 36(4), 193–206. https://doi.org/10.1177/09574271251388971
10. Preregistered test of whether a virtual nose reduces cybersickness (2024). *Cognitive Research*. https://doi.org/10.1186/s41235-024-00593-3
11. Cross-modal synchrony between music and visual motion modulates vection and comfort in VR (2026). https://doi.org/10.1007/s10055-026-01356-9

### 9.6 状态

- **2026-10-20 已实施 P0 + P1（轻量版）+ 转向降速**，见下方记录；P2 在新的校园场景方案下不再需要，P3（暗角）待做。
- **已改（`BikeKeyboardRide.cs`）**：
  - P0：删除 `|Δy|>3m 立即吸附`，改为「指数平滑 + `maxVerticalSpeed=1.2 m/s` 限幅」，任何落差都按速度过渡，无瞬移；
  - P1：新增 `TryRaycastGround` 只认「朝上的水平面」（`minGroundNormalY=0.5`，排除建筑立面/桥侧面），`TryGetGround` 再叠加 `roadRoot`（`Map`）限定；空中墙判定仍用宽松版 `HasGround`，避免卡死在边界；
  - 转向：`turnSpeedDeg` 50 → 35（脚本默认值 + 场景序列化值都已改）。
- **未做**：P2（坡度-速度联动，新场景不需要）、P3（舒适暗角）、P4–P6。
- **实测结果（2026-10-20，用户主观评分）**：改前 **10 分**（很晕）→ 改后 **4 分**，降 6 分（60%）。用户判断剩余 4 分主要来自**素材低清/低模 + 画面抖动**，而非运动逻辑。
- **剩余 4 分的归因与对策**：① 边缘闪烁（MSAA 被禁 + 纹理 mip 关闭 + 低模大色块）→ 用 URP 后处理 FXAA 代替 MSAA、恢复 mip 或加 mip bias；② 无舒适暗角 → P3；③ 素材本身低清 → **新校园场景（8K 实拍、21.3 PPD）会直接消除这一项**。
- 建议：旧低模场景不再投入优化，只把 P3 暗角做成可复用组件，其余随新场景一起解决。

---

## 10. AI 协作规则（省 token 必读）

1. **先读本文件**，按 §4 路径直接打开目标文件；不要重新扫描整个工程。
2. 一次只做一个目标；用户说目标后，先给 3–6 步计划，再动手。
3. 不要重复已验证的步骤（见 §5 已完成）；不要重复已失败方案（见 §6）。
4. 修改 Unity 工程文件前，先 `Read` 确认上下文；改完在本文档“当前状态/变更记录”追加一行。
5. **不要修改**：Azure Kinect / PICO SDK 的驱动与安装目录、`ProjectSettings` 中的构建参数（除非任务明确要求，并记录）。
6. 用户执行 Unity 编译和打包（无自动化桥）；AI 负责改代码、给操作步骤、核对文件。
7. 回复用中文、简洁；代码注释可用中文；不要输出无关总结。
8. 每次任务结束更新本文档：日期、改了什么、验证结果、下一步。
9. 涉及硬件测试时，先给最短验证路径（能 1 分钟确认的先确认）。
10. 若信息不足，只问 1–3 个关键问题，避免长问卷。

### 会话恢复模板（用户直接复制）

```
按 D:\freebikeapk\AI文档\FreeBike_项目交接文档.md 继续。
下一步目标：<例如：A1 电池到了，把功率/踏频接进 PICO>
约束：不重复已完成内容；先给计划再改。
```

---

## 11. 变更记录

| 日期 | 内容 |
|---|---|
| 2026-10-17 | UI 框架（RideUi/InputRouter/MenuPanel/RideSettings/RideSession）、桌面试玩通过 |
| 2026-10-18 | PICO SDK 3.4.0 接入、Android 配置、XR 场景化、APK 构建与装机、日工作报告 |
| 2026-10-19 | 头部追踪修复、占位自行车、Kinect 骨骼验证与转向服务、手柄调试模式、清晰度 1.0、转向方案两次调整 |
| 2026-10-19 | 本文档建立；下一步等待纽扣电池 → A1 BLE → Kinect 姿态链路 → 学校场景 → 跑步 |
| 2026-10-19 | 新增 §9 坡道眩晕专项：原因（垂直 vection/相机只升降/落差吸附/射线命中桥面/速度不联动）、修改计划 P0–P6、参考文献；状态：未实现 |
| 2026-10-20 | 校园场景方案定型：放弃建模，改「实拍 360 图片序列 + 沿路线重放 + 分块加载」；产出《方案对照与选型_江汉大学VR骑行》(md/docx) 于 `D:\freebikeapk\Resources`；依据 VZfit / 3DGS / Cesium / 广西交院航测 等调研；AI 产物目录改为 `Resources` |
| 2026-10-20 | 核实 PICO 4 Pro 官方参数：512GB UFS3.1 / 8GB RAM / 2160×2160 / 20.6 PPD / 105° / 眼动追踪+视线追踪渲染 / Wi-Fi 6；结论改为「本地全量预装优先，服务器只做备份/换场景/共享」；8K 全景 ≈21.3 PPD，刚好 1:1 |
| 2026-10-20 | 新增队友：产出《开发环境安装清单_新队友.md》（Unity 6000.6.6f1 + Android 模块、.NET 10.0.402、AK SDK 1.4.2 / BodyTracking 1.1.2、绝对路径待改点、自检清单） |
| 2026-10-20 | 眩晕 P0/P1 落地：`BikeKeyboardRide` 去掉 3m 瞬移吸附（改 1.2 m/s 垂直限速）、地面射线只认水平朝向面 + roadRoot 限定、`turnSpeedDeg` 50→35（脚本+场景均已改）；**实测眩晕 10 → 4 分**（testbike1.6），剩余归因为低模/闪烁 |
| 2026-10-20 | 下载 6 张真实等距柱状测试全景（5.3K–14.4K，CC BY-SA，Wikimedia，经 Clash 代理）到 `Resources\TestPano`，用于 `PanoramaRoute` 管线验证；四个街景源调研结论（Mapillary 需 token / KartaView 需 key / Panoramax 该区域无 360 / Wikimedia 文件服务器被墙）记录在《测试素材说明_全景替身数据.md》 |
| 2026-10-21 | 新建《UI交互》交接包（`Resources\UI交互\`）：复制 UI 相关 7 个脚本 + 字体，撰写《PICO_UI交互实现指南》（四种输入方式、射线五步、尺寸/字号换算、性能与坑、一周计划），交给负责 UI 的同学 |
| 2026-10-25 | 校园 360 PC VR Demo 成型：`panoTest` 场景 + `CampusRideApp`（主菜单/双模式/HUD/心率报警/补水弹幕/总结/JSON 报告/五语言）、A1 UDP 50506 + W/S 回退；菜单改用 360 校园图独立内球，homepage 导入上限 8192；修正心率漂移模型、骑行计时、平均心率、模拟心率标识；Build 场景列表只启用 panoTest；`runInBackground=1`。详见 §12 |
| 2026-10-26 | 科技风可编辑 UI Prefab 已生成并导入 Unity：`Assets/Resources/Prefabs/CampusRideUI.prefab`；`CampusRideApp` 优先加载 Prefab，RowTemplate 统一生成菜单行；交接位置见 §12.6。 |
| 2026-10-26 | 最新 UI 交互修订：HUD 改为视野正中偏下，暂停时合并到暂停面板；菜单/暂停/准备/设置/总结页加入可调深蓝遮罩；移除设置页重置 HUD 按钮；Grip 面板平移扩展为所有非骑行页面；Trigger 为唯一手柄确认，摇杆不再做 VR 菜单导航；新增 `FreeBikeControllerRays` 显示双手控制器位置/射线并支持射线悬停选择；接入 `nighttest360vidio.mp4` 编辑器调试路径和 StreamingAssets 备份。已用生成的 Assembly-CSharp 工程做静态编译校验；完整 Unity 编译待用户打开编辑器。|
| 2026-10-26 | 科技风 UI 交互落地：`CampusRideApp` 主面板改为独立世界空间 Canvas，加入冰青描边与顶部/底部光带；HUD 同步加入深色玻璃底、冰青描边和光带；运动入口改为“运动准备页”；`InputRouter` 增加左右手 Grip 按下/释放与手柄位姿；主页/运动准备页支持单手或双手 Grip 平移主面板，双手以中点平移，固定旋转/缩放，距离限制 1.6–3.2m，松手保留位置且重启恢复默认；新增 `CampusRideApp` Inspector 布局参数，可手动调整主面板尺寸、偏移、距离、颜色、字号、行按钮和 HUD 默认位置；主面板位置未写入 PlayerPrefs。已在 Unity 6000.6.6f1 编辑器 Play 验证 Prefab 实例化与脚本引用；待 PICO 4 Pro 实机验证。|

---

## 12. 校园 360 PC VR Demo（当前主线，2026-10-25）

### 12.1 运行形态

- PC 端 Unity 渲染，PICO 4 Pro 通过 **PICO 互联 / PICO Connect + SteamVR + OpenXR** 显示；不是旧 PICO Link，也不是头显独立 APK。
- 360 视频贴到 PanoSphere 内表面；A1 功率由 PC 上的 Python BLE 桥接发 UDP 50506；无 A1 时 W/S 键盘回退。

### 12.2 关键文件

| 文件 | 说明 |
|---|---|
| `D:\freebike\Assets\Scenes\panoTest.unity` | 当前唯一启用的构建场景；含 Camera、PanoSphere、`CampusRideApp` 根物体（InputRouter + CampusRideApp） |
| `Assets\Scripts\App\CampusRideApp.cs` | 自包含应用：菜单、设置、帮助、报告、双模式、HUD、心率/补水提醒、总结、五语言 |
| `Assets\Scripts\App\InputRouter.cs` | 手柄/键盘统一输入；摇杆、Trigger、B、Grip |
| `Assets\Scripts\PanoBikeSpeed.cs` | UDP 50506 接收 A1 功率；驱动视频播放速度；<5W 暂停；W/S 回退 |
| `Assets\Resources\homepage.jpg` | 主菜单 360 校园全景（源 `D:\freebike\Media\Stills\homepage_source.jpg`，导入上限 8192） |
| `D:\freebike\Media\Video\6k60fps_hevc.mp4` | 当前外部调试视频（`CampusRideApp.externalVideoPath`）；APK 内备用 `StreamingAssets/Videos/6k60fps_hevc.mp4` |
| `D:\freebike\Tools\A1\a1_bridge.py` | A1 BLE → UDP 50506 桥接 |
| `D:\freebike\Docs\RuntimeReports\CyclingReport\` | 运动报告 JSON 输出目录（代码内固定） |

### 12.3 交互与规则

- 最新 UI 输入：VR 手柄射线悬停菜单行，确认只用食指 Trigger，B 返回/暂停；摇杆不再选择菜单。
- 主面板：主页、准备页、设置、暂停、总结等非骑行页均可单手或双手 Grip 平移，固定旋转/缩放，距离限制 1.6–3.2m；骑行中锁定。
- HUD：运动数据位于视野正中偏下；暂停时隐藏独立 HUD，将时长/距离/心率合并到暂停面板。
- 菜单页和暂停页：原 360 背景上启用深蓝遮罩，透明度可在 `CampusRideApp` Inspector 调整。
- 视频调试：默认尝试 `D:\freebike\Media\Video\6k60fps_hevc.mp4`，不存在时回退 APK 内 `StreamingAssets/Videos/6k60fps_hevc.mp4`；Inspector 可开关视频调试窗口并查看分辨率/FPS/播放状态。

- 主菜单：VR 手柄射线悬停菜单行，食指 Trigger 确认，B/返回键返回；摇杆不再选择 VR 菜单。
- 语言：中文 / English / Español / Français / Português，默认中文，PlayerPrefs `FreeBike.Language` 记忆。
- 纯沉浸式：无 HUD；心率 ≥130 时中央红色安全提示（含当前心率），5s 显示，间隔 20s 再提醒。
- 运动骑行：HUD 位于视野正中偏下，仅心率（标注模拟）/时长/距离；同样 130 阈值报警，以红色弹幕显示。
- 暂停：骑行中按 B；HUD 数据合并到暂停面板，可继续或结束；主面板可用单手/双手 Grip 调整，骑行中锁定。
- 补水：首次 60s，之后每 10 分钟；右侧进入、向左滚动的 5s 弹幕，始终跟随头部视野。
- 结束：两种模式都显示“骑行时长 / 最高心率 / 骑行距离”；运动模式可“生成并保存报告”，JSON 固定写入 `D:\freebike\Docs\RuntimeReports\CyclingReport`；沉浸式只有返回主页。
- 心率是模拟值（功率→目标心率 + 心率漂移），不是医疗数据；报告 `simulatedHeartRate=true`。

### 12.4 构建前检查

1. Unity 6000.6.6f1 打开 `D:\freebike`，等编译完成（Console 不应有 `error CS`）。
2. Build Profiles / Build Settings 场景列表只勾选 `Assets/Scenes/panoTest.unity`；`PicoVideoBenchmark`、`SampleScene`、低模 Demo 均不勾。
3. Target Platform 选 Windows（PC VR），XR Plug-in Management 按当前 PICO Connect + SteamVR 的 OpenXR 配置；`Run In Background` 已开启（代码里也强制开启）。
4. 打包输出建议 `D:\freebike\Releases\Windows\CampusRideDemo\freebike.exe`；现有最新归档为 `Releases\Windows\demo_2026-10-27\freebike.exe`。
5. 先编辑器 Play 用 W/S 验证视频、菜单、HUD、报警、总结与报告，再进头显测手柄。

### 12.5 已知限制 / 下一步

- **PICO 无线连接仍未解决**：PC IP 192.168.31.210，头显与 PC 需同网段；优先用网线/路由器 5GHz，检查专用网络、防火墙、AP 隔离，并在头显启动 PICO 互联、PC 启动 PICO Connect + SteamVR。
- 当前视频是夜间测试片，正式演示前替换为校园白天 360 视频（保持 4K H.264、等距柱状、2:1）。
- A1 不接时只能 W/S；正式演示前先运行 `a1_bridge.py` 并用 UDP 50506 确认功率。
- 菜单使用 `FreeBikeControllerRays` 显示左右手柄位置与射线，射线悬停行并由 Trigger 确认；实现基于工程已安装的 Unity XR 原生接口，不调用未确认的 PICO 私有 API。`CampusRideUI.prefab` 已生成，运行时优先加载该 Prefab；如资源缺失才回退旧 UI。
- 报告路径是 Windows 绝对路径；若改到头显独立 APK 需改为 `Application.persistentDataPath`。

### 12.6 可视化 UI 接手方式（2026-10-26）

- Prefab：`D:\freebike\Assets\Resources\Prefabs\CampusRideUI.prefab`
- 重新生成：Unity 菜单 `Tools > Campus Ride > Create Editable UI Prefab`。
- 主面板：Prefab Mode 中打开 `MainPanel`，可直接编辑 RectTransform、颜色、字号、间距。
- 标题/说明：`MainPanel/Title`、`MainPanel/Subtitle`、`MainPanel/Body`。
- 统一按钮样式：`MainPanel/Rows/RowTemplate`；运行时每个菜单行从该模板复制，调整一次即可统一所有入口。
- HUD：`RideHUD/HrCard`、`RideHUD/TimeCard`、`RideHUD/DistCard`；可直接调整卡片位置、宽高、描边颜色、字号。
- 科技切角：选中 `MainPanel/CutCornerFrame` 或各卡片的 `Frame`，修改 `Corner Cut`、`Thickness`、`Color`。
- 反馈：`Danmaku/Text`、`HeartAlert/Text` 可调整位置、字体、颜色和背景。
- `CampusRideApp` 会在运行时从 `Resources/Prefabs/CampusRideUI` 自动实例化，并把 HUD/弹幕挂到头显相机；主面板位置不写入 PlayerPrefs，便于上线前确定默认值。
- 设计建议：保留深海军蓝底 `#051421`、冰青描边 `#22D3EE`、主文字 `#EEF7FC`、次文字 `#96BEC9`、警示红 `#FF4D4D`。

### 12.7 本次补充（2026-10-26）

- 清理旧交互文案与废弃 HUD 调整状态：帮助页各语言改为“射线悬停 + 食指 Trigger 确认 + B 返回/暂停 + Grip 全局移动主面板”。
- 删除旧的 `AdjustHud` 状态与摇杆调 HUD 逻辑；主面板仅在骑行期间锁定，其他页面均可通过 Grip 调整。





| 2026-10-26 | 追加 PICO XR 输入兼容性兜底：Trigger/Grip 同时读取按钮与模拟轴（阈值 0.75），保持 Trigger 为唯一确认键；静态检查确认 Unity 日志已生成 Assembly-CSharp.dll、无 error CS，调试视频绝对路径配置成功，StreamingAssets 备份存在。界面自动化验证因实体 Escape 中断，PICO 4 Pro 射线/Grip 仍需实机确认。|
| 2026-10-26 | 按用户要求撤回主页深蓝蒙版：`ShowMenu()` 现在关闭 `DeepBlueMenuOverlay`，主页显示原始 360 背景；运动准备、设置、帮助、报告、暂停、总结页继续保留蒙版。|
