# PICO 4 Pro 上的 UI 交互实现指南

编写：2026-10-21　适用工程：`C:\Users\admin\freebike`（Unity 6000.6.6f1 + PICO SDK 3.4.0 + XRI 3.6.1）
配套代码：本目录 `Scripts/`（当前 UI 的 6 个源文件，可直接读）

---

## 0. 先看结论

1. **PICO 上的 UI 不是"网页"，是一块飘在 3D 空间里的布**（World Space Canvas），单位是米，不是像素。
2. 现在这套 UI 走的是**手柄摇杆焦点导航**（上下选、左右调值、A 确认），**没有射线、没有按钮点击**。
3. 想加"射线点按钮"，要做三件事：加 `EventSystem + XRUIInputModule`、给 Canvas 加 `TrackedDeviceGraphicRaycaster`、给控制器加 `XRRayInteractor`；**同时必须把 UI 元素的 `raycastTarget` 打开**（现在全是 `false`）。
4. PICO 4 Pro 还能用**手部追踪**和**眼动注视**，但这两个是"锦上添花"，上手柄射线之前别碰。

---

## 1. 环境与版本（改之前先确认）

| 项 | 值 | 说明 |
|---|---|---|
| 头显 | PICO 4 Pro | 单眼 2160×2160，105° FOV，20.6 PPD |
| 系统 | PICO OS 5.13.7 / Android 10 | 只支持 OpenGLES3 |
| Unity | 6000.6.6f1 | 不要升级 |
| PICO SDK | 3.4.0（嵌入包 `Packages/com.unity.xr.picoxr`） | **改过源码，不要重装** |
| XRI | 3.6.1 | 交互框架 |
| 渲染 | Mobile URP、MultiPass、MSAA 关、RenderScale 1.0 | 改 UI 不需要动这些 |
| 帧率 | 72Hz | UI 动画按 72fps 设计 |

---

## 2. PICO 上的四种输入方式

| 方式 | 原理 | 上手难度 | 适合什么 | 本项目现状 |
|---|---|---|---|---|
| ① 摇杆焦点导航 | 代码维护"当前选中项"，摇杆上下移动焦点 | ★ | 竖排菜单、设置页 | **已在用** |
| ② 手柄射线 | XRI 从手柄发一条线，指到 UI 上点击 | ★★ | 多列卡片、地图选点、图标 | 未实现 |
| ③ 手部追踪 | PICO 骨骼数据 → XRI 手部交互（捏合/戳） | ★★★★ | 无手柄场景、演示 | 只用于"手是否在车把上" |
| ④ 眼动注视 | 眼球方向 → 注视点 + 确认（扳机/捏合） | ★★★★ | 极简 UI、无障碍 | 未启用（`eyeTracking: 0`） |

**建议顺序：①已完成 → ②下一步 → ③④以后再说。**
原因：② 是行业标准做法，用户零学习成本；③④ 在骑行场景里反而不稳（手在握把上、眼睛在看路）。

---

## 3. 现有 UI 架构（读懂这 6 个文件就够了）

### 3.1 文件职责

| 文件 | 行数 | 职责 |
|---|---|---|
| `RideUi.cs` | 583 | 总控 + 所有界面的构建（**全部代码生成，场景里没有 Canvas**） |
| `MenuPanel.cs` | 184 | 通用焦点式菜单：动作 / 数值 / 开关三种条目 |
| `InputRouter.cs` | 101 | 手柄输入抽象：`NavX/NavY/Confirm/Back/StickX/StickY/TriggerValue` |
| `UiTheme.cs` | 70 | 配色、字体（`Resources/Fonts/simhei`）、圆角面板 |
| `RideSettings.cs` | 37 | 设置持久化（PlayerPrefs `fb.*`） |
| `RideSession.cs` | 23 | 本次骑行统计（时长/距离/均速/最高速） |
| `XR/HeadPoseDriver.cs` | 约 60 | 头部姿态驱动相机（UI 定位依赖它） |

### 3.2 状态机

```
Boot（启动页）
  └→ Menu（主菜单）──→ Settings（训练设置）
        │                   └→ 倒计时 → Ride
        ├→ Quit（退出确认）
        └→ Countdown → Ride ⇄ Pause → Results → Menu
                                   └→ Notice（提示条）
```

`RideUi` 里用 `RideState` 枚举 + `ShowScreen()` 切换，每个界面是一个 `GameObject`，切换时只做 `SetActive`。

### 3.3 Canvas 参数（**改 UI 前必须知道**）

```csharp
// RideUi.BuildCanvas()
canvas.renderMode = RenderMode.WorldSpace;
canvasRt.sizeDelta = new Vector2(1200, 700);      // 像素
canvasRt.localScale = Vector3.one * 0.002f;       // 1 px = 2 mm
canvasRt.localPosition = new Vector3(0, eyeY - 0.03f, 1.5f);  // 眼前 1.5 m，比眼高低 3 cm
canvasRt.SetParent(ride.transform, false);        // 挂在 XR Origin 下，跟着车走
```

换算关系：

| 像素 | 实际尺寸 | 视角（1.5 m 处） |
|---|---|---|
| 1 px | 2 mm | — |
| 58 px（菜单项高） | 11.6 cm | 4.4° |
| 28 px（正文） | 5.6 cm 字高 | 2.1° |
| 1200 px（面板宽） | 2.4 m | 77° |

> ⚠️ 面板 2.4 m 宽、在 1.5 m 处占 77° 视场，**边缘要用眼球扫**。舒适区间是 40–60°，建议新面板宽度控制在 1.2–1.6 m。

### 3.4 淡入淡出遮罩

单独一块 Canvas，挂在相机下，距离 0.35 m，缩放 0.001。它是转场用的黑幕，**不要**把它当"头部锁定 UI"用。

---

## 4. 方式①：摇杆焦点导航（已实现，先别推翻）

**怎么工作**：`InputRouter` 每帧读摇杆 → `MenuPanel` 维护 `Index` → 高亮当前项 → 确认/左右调值。

**关键代码**（`MenuPanel`）：

```csharp
class Item {
    public GameObject go;
    public Image bg;      // 焦点高亮用背景色变化
    public Text label;
    public Text value;
    public Kind kind;     // Action / Value / Toggle
    public Func<string> get;
    public Action<int> adjust;
    public Action confirm;
}
```

**优点**：零学习成本、不依赖指针、帧率友好、手柄手部都能映射。
**缺点**：只能做"列表"，做不了"地图选点 / 图标网格 / 拖动滑块"。

**要新增一屏，就照抄一个 `MenuPanel` 实例**，别自己写一套 UI 逻辑。

---

## 5. 方式②：手柄射线（推荐的下一步）

### 5.1 五个步骤

**Step 1：EventSystem 换成 XR 的**

```csharp
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;   // XRUIInputModule

if (EventSystem.current == null) {
    var go = new GameObject("EventSystem");
    go.AddComponent<EventSystem>();
    go.AddComponent<XRUIInputModule>();   // 关键：不能用 StandaloneInputModule
}
```

> 场景里如果已经有 `StandaloneInputModule`，**必须删掉**，否则 XRUIInputModule 收不到事件，表现就是"射线能画出来但点不动"。

**Step 2：Canvas 加射线接收器**

```csharp
canvas.renderMode = RenderMode.WorldSpace;
canvas.worldCamera = xrCamera;                       // 不设这一条，UI 完全不显示
canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
```

**Step 3：控制器加射线**

在 XR Origin 的左右手控制器物体上：

- `XRController`（指定 Input Action：`XRI LeftHand/RightHand`）
- `XRRayInteractor`（Max Raycast Distance 3–5 m，Raycast Mask 只勾 UI 层）
- `LineRenderer` + `XRInteractorLineVisual`（视觉）
- 可选：`XRInteractorReticleVisual`（命中点小圆点）

**Step 4：让按钮真的能被点**

当前 `UiTheme.CreatePanel` / `CreateText` 里都是 `raycastTarget = false`。要点击的元素必须：

```csharp
img.raycastTarget = true;                       // 背景板接射线
var btn = go.AddComponent<Button>();            // 加 Button
btn.onClick.AddListener(() => { ... });
```

**Step 5：和现有焦点导航共存**

建议：`RideUi` 增加一个 `enum UiInputMode { Focus, Ray }`，设置页里可切换。射线模式下暂停 `MenuPanel` 的焦点高亮，避免两套高亮打架。

### 5.2 射线常见问题对照表

| 现象 | 原因 | 修法 |
|---|---|---|
| 射线画出来了，点不中 | Canvas 没有 `TrackedDeviceGraphicRaycaster` | 加上 |
| 点中了没反应 | `raycastTarget=false` 或没挂 `Button` | 打开 / 加 Button |
| 完全没事件 | EventSystem 还是 `StandaloneInputModule` | 换 `XRUIInputModule` |
| UI 在编辑器里看不见 | `canvas.worldCamera` 为空 | 指定 XR 相机 |
| 射线穿墙点到后面的 UI | Raycast Mask 包含全部层 | 只勾 UI 层 |
| 手柄模型挡住 UI | 控制器渲染层与 UI 层冲突 | 调整 Layer 或近裁剪 |

---

## 6. 方式③：手部追踪

**项目已开启**：`PXR_ProjectSetting` 里 `handTracking: 1`。现有代码里已经有调用范例（`Bike/HandsOnHandlebarDetector.cs`）：

```csharp
using Unity.XR.PXR;

HandJointLocations joints = new HandJointLocations();
if (PXR_HandTracking.GetJointLocations(HandType.HandLeft, ref joints)) {
    var palm = joints.jointLocations[(int)HandJoint.JointPalm].pose.Position.ToVector3();
}
```

注意三点：
1. `PXR_HandTracking` 在 `Unity.XR.PXR` 命名空间下，**手部追踪没开时会抛异常**，调用前要 try/catch 或先判断开关；
2. 手部追踪与手柄是**互斥/切换**关系，切模式要做 UI 提示（项目里已有"手柄调试模式"提示的先例）；
3. 手部 UI 交互建议用 XRI 的 **Poke Interactor（戳）+ Ray Interactor（远距离）**，戳比捏更自然。

---

## 7. 方式④：眼动（PICO 4 Pro 独有）

**当前状态：关闭**（`PXR_ProjectSetting.eyeTracking: 0`）。

开启后可以做：
- **注视 + 确认**：眼睛看哪，扳机/捏合确认（适合骑行中不抬手）
- **视线追踪渲染**：只把注意力区域渲染清楚，省 GPU（官方文档有，需应用适配）

风险：
- 需要用户校准，骑行中眼动数据抖动大；
- 官方注明"适配应用上线时间以实际情况为准"，先在设置页做成可选开关，别当主交互。

参考：[PXR_EyeTracking API](https://developer-cn.picoxr.com/reference/unity/client-api/PXR_EyeTracking/)

---

## 8. 尺寸 / 距离 / 字号 经验表

按 `localScale = 0.002`（1 px = 2 mm）：

| 元素 | 建议像素 | 实际尺寸 | 备注 |
|---|---|---|---|
| 正文字号 | ≥ 22 px | ≥ 4.4 cm | 再小就看不清 |
| 标题 | 40–72 px | 8–14 cm | 启动页可到 72 |
| 菜单项高度 | ≥ 50 px | ≥ 10 cm | 现在 58 px，合适 |
| 按钮最小尺寸 | ≥ 90×45 px | 18×9 cm | 射线点击容错 |
| 面板宽度 | ≤ 800 px | ≤ 1.6 m | 超过要转头 |
| 面板距离 | 1.2–2.0 m | — | 现在 1.5 m，合适 |
| 面板下沿 | ≥ 眼高 -20 cm | — | 别低到需要低头 |

**配色**：深底浅字（现方案 `PanelBg 0.07` + `TextMain 0.95` 就对），避免大面积纯白（Pancake 屏会眩光）。

---

## 9. 性能与舒适（PICO 4 是移动芯片，别乱来）

1. **不要每帧重建 UI**：现在是"构建一次 + SetActive"，保持这个做法；改文字才更新，别每帧 `text = ...`（会触发 Canvas rebuild）。
2. **用 `CanvasGroup.alpha` 做淡入淡出**，别频繁 `SetActive` 大子树。
3. **少用全屏半透明**：overdraw 直接吃帧率，黑幕只在转场用。
4. **不要做 head-lock UI**（面板焊死在眼前）：会晕。现在是"挂在 XR Origin 上、固定在世界里"，正确。
5. **动画时长 0.15–0.25 s**，72Hz 下不用补间库，`Mathf.SmoothStep` 够了。
6. **一屏信息别超过 7 项**：VR 里没有"滚动习惯"，超了就分页。

---

## 10. 中文与字体

- 现在用 `Resources/Fonts/simhei.ttf`（9.5 MB）+ uGUI 动态字体。
- 动态字体的坑：**第一次显示新字时会重建图集**，可能卡一帧；生僻字多了图集会溢出（表现为字变方块）。
- 建议：把界面上会用到的字**预先渲染一次**（启动时 `font.RequestCharactersInTexture("常用字...")`），或者换 **TextMeshPro + SDF**（更清晰、支持描边，但要改所有 `Text` 为 `TMP_Text`，工作量大，二期再做）。

---

## 11. 测试与调试

| 做什么 | 怎么做 |
|---|---|
| 打包 | Unity → Build → `testbike1.x.apk` |
| 安装 | `adb install -r xxx.apk`，或走 SMB 共享 |
| 看日志 | `adb logcat -s Unity` |
| 看帧率 | PICO 开发者中心的 Metrics HUD |
| 免戴头显预览 | 编辑器里 `HeadPoseDriver` 会模拟 1.6 m 视角，可以直接看面板位置 |

**改 UI 的验证清单**（每次改完过一遍）：

- [ ] 启动页 → 主菜单 → 设置，三屏都能进出
- [ ] 焦点高亮跟随摇杆，无跳格
- [ ] 面板在视野中央，不用转头就能看全
- [ ] 最小字号在 1.5 m 处能看清（拿手机拍一张对比）
- [ ] 转场不闪、不黑屏超过 0.3 s
- [ ] 72 fps 不掉（改完 UI 必看 Metrics HUD）

---

## 12. 给 UI 同学的一周顺序建议

| 天 | 目标 |
|---|---|
| 1 | 读这 6 个文件 + 跑起来看现有界面，画出状态机 |
| 2 | 用 `MenuPanel` 复刻一个新界面（不改框架），练手 |
| 3 | 加射线：EventSystem + Canvas Raycaster + XRRayInteractor，**只让一个按钮可点** |
| 4 | 把主菜单改成可点，保留摇杆导航作为备用 |
| 5 | 视觉打磨：配色、间距、字号、动效（0.2 s） |
| 6 | 真机测试 + 性能（Metrics HUD），修穿透/遮挡 |
| 7 | 写一页改动说明，交回主工程合并 |

---

## 13. 参考链接

- PICO 输入与追踪：<https://developer-cn.pico-interactive.com/document/unity/input-mapping/>
- PXR_HandTracking API：<https://pdocor.pico-interactive.com/reference/unity/xr/2.2.0/class_unity_1_1_x_r_1_1_p_x_r_1_1_p_x_r___hand_tracking.html>
- PXR_EyeTracking API：<https://developer-cn.picoxr.com/reference/unity/client-api/PXR_EyeTracking/>
- PICO 设计规范（视觉/移动）：<https://developer-cn.picoxr.com/document/design/visual-design/>
- XRI 官方文档（Ray Interactor / UI）：Unity 包管理器里打开 `XR Interaction Toolkit` 的 Documentation

---

## 14. 一句话提醒

**别在副本里改完就以为工程也变了**——本目录的 `Scripts/` 是 2026-10-21 的快照，真工程在 `C:\Users\admin\freebike\Assets\Scripts\App\`。改完要么把文件发回来合并，要么直接在工程里改。
