# 江汉大学校园 VR 骑行：方案对照与选型

版本：v2　日期：2026-10-20　作者：AI 助手
> v2 按 PICO 4 Pro 官方参数修正：512GB UFS 3.1（可全量本地预装）、20.6 PPD（8K 刚好 1:1）、眼动追踪与视线追踪渲染。
配套工程：`C:\Users\admin\freebike`（Unity 6000.6.6f1 + PICO SDK 3.4.0）
本文档路径：`D:\freebikeapk\Resources\方案对照与选型_江汉大学VR骑行.md`

---

## 0. 一句话结论

**不做建模，也不抄教程的"球体+一段视频"；用"实拍 360 素材 + 沿路线重放 + 分块加载"来把江大搬进头显。**
渲染基座选 Unity 官方 360 全景方案（`SkyboxPanoramicShader` + `VideoPlayer`/贴图），路线调度、分块加载、速度耦合三个脚本自研——因为调研下来，这个细分领域**没有现成的成熟开源轮子**（星标最高的只有 282，且只覆盖渲染层）。PICO 4 Pro 的 512GB 存储允许全量本地预装，服务器是可选增强而非必需。

---

## 1. 你的现状基线（已核实）

| 项目 | 规格 | 来源 | 在方案里的角色 |
|---|---|---|---|
| 头显 | PICO 4 Pro（A8110），PICO OS 5 / Android 10，GLES 3.2 | 交接文档 §2 | 唯一目标设备 |
| 算力 | 骁龙 XR2（7nm，2.84GHz）、**8GB LPDDR5** | PICO 官网参数页 | 决定 72Hz 稳定运行的上限 |
| 存储 | **512GB UFS 3.1** | PICO 官网参数页 | 不是瓶颈：12 km 全量预装也只占 6.5% |
| 显示 | 双目 2160×2160、72/90Hz、105° FOV、**20.6 PPD**、Pancake | PICO 官网参数页 | "8K 刚好 1:1"的清晰度基准 |
| 特色硬件 | **眼动追踪 ×2、面部追踪、彩色透视、自动 IPD 62–72mm** | PICO 官网参数页 | 眼动可开注视点渲染，省 GPU |
| 无线 | Wi-Fi 6（2×2 MIMO，2.4/5 GHz）、蓝牙 5.1 | PICO 官网参数页 | 流式可行，但一期不必依赖 |
| 引擎 | Unity 6000.6.6f1 | `ProjectVersion.txt` | 已定型，不升级 |
| XR SDK | PICO SDK 3.4.0（嵌入包）、XRI 3.6.1、XR Management 4.6.1 | 交接文档 §2 | 直接用，不再引入新 XR 框架 |
| 骑行数据 | ThinkRider A1，BLE，FTMS `0x2AD2` / CPS `0x2A63` | 交接文档 §3.2 | 已写 Java 插件 + C# 桥接，等电池实测 |
| 姿态/转向 | Azure Kinect DK + `KinectSteering.exe`（PC，DirectML，26fps）→ UDP 50505 | 交接文档 §3.1 | 转向输入，已有接收器 |
| 现有代码 | `RideUi` / `InputRouter` / `MenuPanel` / `RideSession` / `BikeKeyboardRide` / `BikeVisualRig` | 交接文档 §3.2 | 全部复用，只改"位移来源" |
| 渲染约束 | 仅 OpenGLES3、MSAA 关、MultiPass、RenderScale 1.0 | 交接文档 §3.4 | 决定了 3DGS 等 Vulkan 方案不可用 |
| 校园规模 | 2114 亩 ≈ 1.41 km²；道路约 12 km | 学校官网 + 用户口述 | 决定必须分区分层 |
| 学校已有数据 | 三维校园 GIS（gis.jhun.edu.cn）：VR 校园、地图下载、导航路线 | 官网实查 | **先去要数据**，可能省掉整个无人机阶段 |

> 结论：**硬件与软件基线足够**；而且 512GB 存储让"全量本地预装"成为可行项——**一期可以完全不依赖网络**，服务器只做备份、共享与换场景。

---

## 2. 六方案横向对照

评分：★1–5，★ 越多越好；"适配度"= 与 PICO 4 Pro / GLES3 / 12 km 校园的匹配程度。

| 方案 | 真实感 | 可自由骑 | 数据量 | PICO 4 Pro 性能 | 工期(单人) | 适配度 | 结论 |
|---|---|---|---|---|---|---|---|
| A. 教程做法：单球 + 一段全景视频 | ★★★★★ | ✗（只能看） | 小（单文件） | 好 | 1 周 | ★★ | 只能当演示，不能上 12 km |
| B. 实拍 360 图片序列 + 沿路线重放 | ★★★★★ | 沿路 + 横向偏移 | 中（约 8.4 GB / 12 km） | 好（零解码） | 3 周 | ★★★★★ | **主线，选它** |
| C. 全景 + 建筑轮廓投影（VZfit 城市模式） | ★★★★ | 沿路 | 中 | 好 | +1 周 | ★★★★ | 支路/远景扩展 |
| D. 无人机倾斜摄影重建 | ★★（近景糊） | ✓（网格） | 大（10–30 GB） | 勉强 | 6–10 周 | ★★ | 只做远景与正射 |
| E. 3D Gaussian Splatting | ★★★★★ | 视实现 | 中 | **不可用**（GLES 不支持） | 1–2 月 | ★ | 二期技术储备 |
| F. 纯手工 3D 重建（Zwift 式） | ★ | ✓ | 小 | 好 | 数月 | ★ | 不做 |

### 关键数据账（为什么是 B 而不是 A/D）

| 项 | 数值 | 说明 |
|---|---|---|
| 12 km @ 4 m 间距 | 3000 帧 | 采样密度与视差跳变的平衡点 |
| 单帧 ASTC cubemap 1024/面 | ≈2.8 MB | 零解码，直接上传 GPU |
| 单帧 ASTC cubemap 2048/面 | ≈11 MB | 1:1 清晰度档 |
| 12 km 总量（1024/面） | ≈8.4 GB | 512GB 里占 1.6%，**可全量预装** |
| 12 km 总量（2048/面） | ≈33 GB | 占 6.5%，**也可全量预装** |
| 骑行中带宽需求（5 m/s，2048/面） | ≈14 MB/s | UFS 3.1 顺序读余量 10 倍以上 |
| 网络需求（若走流式） | ≈28 Mbps（1024/面） | VZfit 官方只要 5 Mbps |
| 首帧目标 | <2 s | 用 512/面占位图兜底 |
| 卡顿目标 | 0 次 >50 ms / 30 min | 比清晰度更重要的指标 |

### 清晰度换算（面板 20.6 PPD 是标尺）

每度像素数 PPD = 全景宽度 ÷ 360，与面板 PPD 对齐即为"1:1 不糊"。

| 全景规格 | 有效 PPD | 相对 20.6 PPD | 观感 |
|---|---|---|---|
| 4K（3840） | 10.7 | 52% | 明显糊 |
| 6K（6144） | 17.1 | 83% | 轻微发软 |
| **8K（7680）** | **21.3** | **103%** | **刚好 1:1，够清晰** |
| 12K（11520） | 32.0 | 155% | 过剩，面板吃不下 |
| 对应 cubemap 1024/面 | 11.4 | 55% | 骑行档（省带宽） |
| 对应 cubemap 2048/面 | 22.8 | 111% | 停车环顾档（1:1） |

---

## 3. 开源选型调研（含查证状态）

### 3.1 候选清单

数据取自 GitHub API 检索（2026-10-20），星标与协议为当时快照。

| 候选 | 星 | 协议 | 最后更新 | 它能提供什么 | 短板 |
|---|---|---|---|---|---|
| `Unity-Technologies/SkyboxPanoramicShader` | 282 | 未标注（默认版权保留） | 2018 | 360/180/cubemap 视频与静态贴图的 skybox 渲染 | 只有渲染层；无许可证，**不能直接抄代码**，只能参考做法 |
| `bodhid/UnityEquiCam` | 76 | MIT | 2018 | 等距柱状全景相机/渲染 | 年代老，仅渲染 |
| `makoto-unity/PanoramaVideoWithUnity` | 40 | MIT | 2015 | 全景视频最小示例 | Unity 5 时代，仅演示 |
| `RCL-Studio-X/Virtual-Tour-Template` | 2 | NOASSERTION | 2026-07 | 360 视频导览模板（热点、切换） | 星极少，无路线推进、无流式 |
| `sidequestlegend/StreetViewDepth` | 4 | MIT | 2024 | 从 Unity 访问 Google 街景 | 依赖外部街景数据，实验性 |
| `Pico-Developer/PICO-Unity-Integration-SDK` | 120 | NOASSERTION | 2026-04 | PICO 设备集成（你们已内嵌 3.4.0） | 不含全景骑行逻辑 |
| `Pico-Developer/splat_unreal` | 20 | NOASSERTION | 2025-04 | PICO 官方 3DGS（Unreal） | Unreal，且你们要 GLES3 |
| A-Frame（aframevr） | 未逐一核 | MIT | 活跃 | WebXR 全景，一天可出概念验证 | 浏览器内跑，接不了 BLE，性能受限 |

### 3.2 调研结论

1. **没有"全景骑行"的开源成品。** 最贴近的产品 VZfit 是闭源的，且它的核心价值在工程取舍（5 Mbps、12 ft 偏移限制、停车加载高清、三种舒适模式），不在代码。
2. **所有候选都只覆盖"渲染"这一层。** 而我们真正的工作量在：路线推进、分块流式、速度耦合、断流降级——这四件事没人替你写。
3. **3DGS 明确排除**：`aras-p/UnityGaussianSplatting` 的 README 写明只支持 D3D12 / Metal / Vulkan，"Anything using OpenGL or OpenGL ES" 不支持；而你们的构建只保留 GLES3。

### 3.3 选定方案

**渲染基座：Unity 官方 360 全景做法（`SkyboxPanoramicShader` 的等价自实现 + `VideoPlayer`/贴图）**
**调度层：自研三个脚本**（下面 §4.2）

理由：
- 官方做法的渲染路径与 PICO + GLES3 兼容性最好，没有额外依赖；
- 全景球渲染本身只有几十行 shader，**自写比接第三方更省事**（第三方反而带来 Unity 版本、渲染管线、许可证三重风险）；
- 需要自研的部分（路线/分块/耦合）无论如何都避不开，所以不存在"少写很多代码"的开源捷径。

**存储取舍（因为 512GB 而改写）：** 一期**默认本地优先**——把主线素材整包预装进设备（8K/2048 面约 33 GB，占 6.5%），运行时只从 UFS 3.1 顺序读，完全不依赖 Wi-Fi。服务器只做三件事：备份、换场景（下一条路线）、多设备共享。**这样把"网络抖动"这个最大的风险源从一期直接移除。**

> 许可证提醒：`SkyboxPanoramicShader` 仓库未标注许可证，**只能参考思路、不能复制文件**；若要交付或商用，自写实现或选 MIT 协议的 `UnityEquiCam` 作参考。

---

## 4. 选定架构

### 4.1 分层

| 层 | 内容 | 规格 / 备注 |
|---|---|---|
| C 层 远景 | 无人机正射 + 航拍球（或学校 GIS 的倾斜模型） | 常驻显存，几十 MB |
| B 层 支路 | 4K 全景，8 m 间距，只能看不能进 | 512/面 ASTC |
| A 层 主线 | 8K 全景，4 m 间距，可骑、可横向偏移、可分岔 | 1024/面 骑行，2048/面 停车 |
| D 层 近景 | 车把、前轮、曲柄（复用 `BikeVisualRig`） | 实时光照，1 万面以内 |

### 4.2 模块与文件

| 新文件 | 职责 | 依赖 |
|---|---|---|
| `Assets/Scripts/Street/PanoramaRoute.cs` | 路线样条、弧长 t、位置/朝向、双档贴图选择、硬切 | `BikeKeyboardRide` 速度 |
| `Assets/Scripts/Street/PanoramaChunkSource.cs` | chunk manifest、HTTP Range 下载、LRU 磁盘缓存、预取队列、断流降级 | UnityWebRequest |
| `Assets/Scripts/Street/PanoramaSphereRenderer.cs` | 全景球/天空盒渲染、双档切换、mip 与各向异性 | 自写 shader |
| `Assets/Scripts/Street/RideMotionBinder.cs` | 把 `Speed` 积分成弧长，替换现自由前进；输出横向偏移与分支选择 | `InputRouter`、`SteeringHub` |

现有代码改动：
- `BikeKeyboardRide.ApplyMotion`：位移改由 `RideMotionBinder` 驱动（贴地/落差吸附逻辑在校园平路场景可关）；
- `RideUi`：HUD 增加"缓存/网络/清晰度档位"状态；
- `ThinkRiderBleSource`：不变，继续供功率/踏频；
- `BikeVisualRig`：不变，作为 D 层。

### 4.3 PICO 4 Pro 的三项设备红利（别浪费）

| 红利 | 官方口径 | 怎么用 |
|---|---|---|
| **眼动追踪 + 视线追踪渲染** | "重点渲染视线焦点，注视区域保持清晰，降低 GPU 负载、提升性能" | 二期开注视点渲染，把省下的 GPU 留给 2048/面高清；注意官网注明"适配应用上线时间请以实际情况为准"，需先用 PICO SDK 验证 API 可用性 |
| **512GB UFS 3.1** | 官方参数 | 全量本地预装，一期去掉网络依赖（见 §3.3） |
| **自动 IPD 62–72mm** | 官方参数 | 多人轮流体验不必手动调，减少"晕"的个体因素（对应交接文档 §9 P6） |
| Wi-Fi 6（2×2 MIMO） | 官方参数 | 若后期要流式，带宽不是问题；真正要防的是 AP 漫游与 Portal 认证 |

---

## 5. 与教程做法的接口对照表

| 教程文件/脚本 | 它解决的问题 | 我们的对应实现 | 状态 | 处理方式 |
|---|---|---|---|---|
| `sphere` + `VideoPlayer` + VideoClip | 全景素材播放 | `PanoramaSphereRenderer` + `PanoramaRoute` | 待写 | 升级为"图片序列 chunk + 双档" |
| `VideoManager`（未公开源码） | 播放/暂停 | `PanoramaRoute` 内的推进与暂停 | 待写 | 播放速率由速度决定，而非固定 1.0 |
| `UIMainPanel` | 计时、HUD、喝水提醒 | `RideUi` + `RideSession` | 已有 | 加 HUD 字段即可 |
| `SerialportHelper`（未公开源码） | 蓝牙串口读自行车 | `ThinkRiderBleSource` + `ThinkRiderBle.jar` | 已有框架，待实测 | SPP → BLE GATT（FTMS/CPS） |
| `SteamVR_InteractRay` / `RayHandler` | 激光笔交互 | `InputRouter` + `MenuPanel` | 已有 | 用 XRI 射线替代 SteamVR |
| `Stereo3D` + 第二相机 | 平面屏幕的互补色/偏振 3D | 无 | — | **丢弃**（VR 本身即立体） |
| `SteamVR_*` 资源包 | PC VR 运行时 | PICO SDK + XRI | 已有 | 不引入 SteamVR |
| `Pano/` 目录 | 全景素材 | `Chunks/` + `manifest.json` | 待建 | 规范化命名与校验 |
| 单场景切换（射线点按钮） | 换场景 | 路线分段 + 分支节点 | 待写 | 换"关卡"为单位，不再切整场景 |

差异提示：教程是**"视频自己播、人不动"**，我们是**"人踩多快、世界走多快"**。这一条是整个方案的分水岭。

---

## 6. 素材与数据规范

### 6.1 拍摄

1. 先定骑行主线（建议 3–5 km）：主校门 → 图书馆 → 三角湖环湖路 → 操场 → 回主校门。
2. 路上标出岔路口（分支节点）和"值得停下环顾"的点（高清升级点）。
3. 地面 360：车把上方 1.5 m，8K/30fps 匀速骑一遍（10–15 km/h），同步录 GPX。
4. 无人机：沿路线 20–30 m 低空环绕 + 地标环绕；正射一套（对应广西交院案例：约 0.3 km²/架次，江大约 5 架次）。

### 6.2 命名与结构

```
Chunks/
  route_main/
    chunk_000000/            # 每 200 m 一个
      manifest.json          # t,x,z,yaw,hash,size,lod
      f_000000_512.astc      # 512/面（占位，秒开）
      f_000000_1024.astc     # 1024/面（骑行档）
      f_000000_2048.astc     # 2048/面（停车环顾，1:1）
      ...
    chunk_000001/
  route_branch_lake/
Routes/
  route_main.json            # 样条控制点 + 总长 + 分支表
```

### 6.3 关键规则

- 每帧必须带 `yaw`（路线切线方向），否则前进时"原地打转"；
- 硬切不用交叉淡入（移动中淡入必重影）；
- ASTC 离线烘焙，运行时零解码；
- 每个 chunk 带 sha256，断点续传与坏块重下。

---

## 7. 三周计划与验收

| 周 | 目标 | 产出 |
|---|---|---|
| 1 | 200 m 真实素材端到端打通 | 本地预装 + 头显 + 沿路线前进 + 车把叠加 + 三档降级 |
| 2 | 主线 3–5 km 采集与流水线脚本 | 一条能骑完的完整路线，72 fps |
| 3 | 分支路口、HUD/结算、全量预装包 | 可演示版 + 验收报告 |

验收指标：
1. 首帧 < 2 s（512 档占位）；
2. 连续 30 分钟骑行零 stall，最长卡顿 < 50 ms；
3. 断网骑行 30 分钟不受影响（本地预装）；
4. 72 fps，掉帧率 < 1%；
5. 停车后 1 s 内升级到 2048 档；
6. 主观眩晕评分（0–10）比现低模场景下降 ≥ 3 分。

---

## 8. 风险与待确认

| 风险 | 影响 | 应对 |
|---|---|---|
| 校园 Wi-Fi 有 Portal 认证 / AP 隔离 | 后台下载中断、UDP 广播失败 | 一期本地预装；走流式时用专用 5 GHz 路由器 + PC 当服务器 |
| 360 拍摄时人多/车多 | 画面出现"穿越"人影 | 选清晨/假期拍；关键帧做修补 |
| 8K 素材处理耗时长 | 拖慢进度 | 流水线脚本先行，可夜间批处理 |
| 学校 GIS 数据拿不到 | 远景要自己做 | 无人机倾斜摄影兜底（5 架次） |
| UFS 随机读抖动 | 换帧瞬间卡顿 | chunk 连续存放（每个 chunk 一个大文件）+ 预取 2 chunk；上线前实测读速 |
| 眼动渲染需应用适配 | 二期性能红利可能落空 | 先用 PICO SDK 的 eye tracking / foveation 接口做最小验证，再决定是否纳入 |

待用户确认：
1. 主线选哪 3–5 km；
2. 学校 GIS 数据能否拿到。

---

## 附：证据来源

- 江汉大学 2114 亩 / 三角湖 / 60 万 m² 建筑：<https://bkzs.jhun.edu.cn/b6/f8/c7318a177912/page.htm>
- 学校三维校园 GIS（VR 校园 / 地图下载）：<https://gis.jhun.edu.cn/>
- 广西交院航测案例（18 像控点、2 架次、5000+ 张、0.3 km²/架次）：<https://yurenhao.sizhengwang.cn/a/gxjtzyjsxytmjzgcxyjcssswszlsgcyjzxjsdzb/250920/2223425.shtml>
- VZfit（5 Mbps、12 ft、三种渲染模式、停车加载高清）：<https://virzoom.com/vzfit-explorer-help/>
- `UnityGaussianSplatting` 不支持 OpenGL ES：<https://github.com/aras-p/UnityGaussianSplatting>
- Cesium WorldLens（Quest 3 流式 3D Tiles，>70 fps）：<https://cesium.com/blog/2026/01/06/worldlens-delivers-high-performance-vr-exploration/>
- 教程原文：`D:\freebikeapk\AI文档\VR全景视频自行车沉浸式健身制作教程(1).docx`
- PICO 4 Pro 官方参数（512GB UFS3.1 / 2160×2160 / 20.6 PPD / 105° / 眼动 / Wi-Fi 6）：<https://www.picoxr.com/cn/products/pico4-pro/specs>
- PICO 4 Pro 产品页（视线追踪渲染、自动 IPD）：<https://www.picoxr.com/cn/products/pico4-pro>

> 未核实项：Rouvy / Kinomap 的技术细节（站点 403 或纯 JS），本文未引用其任何参数。
