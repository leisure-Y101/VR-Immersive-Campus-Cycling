# PICO 4 Pro 沉浸式校园骑行 UI - Unity Starter

本包提供运行时生成的世界空间菜单、运动准备页、运动 HUD、暂停页、总结页、设置、多语言入口、报告列表/详情、退出确认、摇杆焦点导航，以及基础的 Grip HUD 调整逻辑。

## 文件

- `CyclingVRApp.cs`：运行时 UI、页面导航、演示运动数据、报告保存。
- `CyclingButtonHover.cs`：当前摇杆焦点放大并向观察者方向轻微凸出。
- `CyclingHudGrabber.cs`：按住 Grip 调整 HUD；摇杆上推推远、下拉拉近；手柄左右转动平移；Trigger 确认，B 取消。

## 使用前提

- Unity 项目已安装 TextMeshPro，并通过 `Window > TextMeshPro > Import TMP Essential Resources` 导入资源。
- 场景中已有 XR Origin / XR Rig 和 XR Camera。
- 已安装与当前 Unity 版本兼容的 PICO XR / OpenXR 与 XR Interaction Toolkit。
- `Assets/Resources/homepage.jpg` 是 360° 等距柱状全景图；不要使用普通透视照片代替。也可在 Inspector 中设置 `panoramaOverride`。

## 安装步骤

1. 将三个 `.cs` 文件复制到 `Assets/Scripts/CyclingVR/`。
2. 在场景创建空物体 `CyclingVRApp`，挂载 `CyclingVRApp.cs`。
3. 将 XR Camera 拖入 `Vr Camera` 字段；未指定时脚本尝试使用 `Camera.main`。
4. 将 360° 全景图放到 `Assets/Resources/homepage.jpg`，确保导入设置允许作为纹理使用。
5. 确保 TMP Essential Resources 已导入。
6. Canvas 使用 World Space，场景中保留 XR Origin / Camera 即可。按钮不依赖射线或悬停事件。
7. 在 PICO 设备上确认摇杆 `primary2DAxis`、食指扳机 `triggerButton`、握把 `gripButton` 和 B 键 `secondaryButton` 的输入映射。不同 PICO SDK 版本可能需要在 `CyclingHudGrabber.cs` 和 `CyclingVRApp.cs` 中调整 InputFeature 映射。
8. 构建前在 Player Settings 中配置 Android、ARM64、XR 插件和 PICO 设备所需设置。

## 当前实现说明

- UI 使用 Unity UI + TextMeshPro 在运行时生成，方便先验证布局；它不是最终精细美术稿。
- 主页六个按钮按浅弧线近似排列；Unity UI 默认矩形按钮，后续可替换成自定义切角/弧形 Sprite。
- 背景通过 `Skybox/Panoramic` 显示全景图。
- 模拟心率与骑行数据用于原型演示，不是实际传感器数据。
- 速度基于 `12 × videoPlaybackMultiplier`，范围 10.2–15.0 km/h；当前倍率由 Inspector 配置，尚未与真实视频播放控制器绑定。
- 距离按显示速度积分。
- 报告暂存在 PlayerPrefs，适合原型；正式应用建议使用 JSON 文件或 SQLite，并按 Android/PICO 可用的持久化路径保存。
- `D:\freebike\Docs\RuntimeReports\CyclingReport` 是 Windows 路径，PICO Android 设备不能直接使用该路径。
- 报告中的卡路里目前为估算演示值。
- 纯沉浸式结束目前通过公开方法 `EndImmersiveRide()` 触发；请将其绑定到你的结束操作。
- 暂停页的 B 键处理和实际 PICO 控制器映射需要在目标设备验证。
- 该包是 UI 与基础状态流的起点，尚未接入真实心率、真实视频控制、正式 Android 报告导出和应用级 QA。

## 调试提示

如果文字不显示或中文方块：
- 确认已导入 TMP Essential Resources。
- 为 TextMeshPro 指定包含中文字符的 TMP Font Asset。默认 TMP 字体通常不包含完整中文字符；建议创建中文字体资产并将其设为 `TMP_Settings.defaultFontAsset` 或在脚本中显式赋值。

如果摇杆无法切换按钮：
- 确认手柄能被 `InputDevices.GetDeviceAtXRNode` 找到。
- 确认摇杆映射为 `primary2DAxis`，并达到约 0.55 的偏转阈值。
- 确认 Trigger 映射为 `triggerButton`；按住 Grip 时，Trigger 用于确认 HUD 位置。

如果背景不是完整 360°：
- 确认图片是等距柱状投影（Equirectangular），通常宽高比为 2:1。
- 确认纹理没有被错误裁剪。
