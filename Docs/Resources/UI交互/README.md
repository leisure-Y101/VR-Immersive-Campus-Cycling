# UI 交互 · 交接包

给负责 UI 交互的同学。生成日期：2026-10-21
对应工程：`C:\Users\admin\freebike`（Unity 6000.6.6f1 + PICO SDK 3.4.0）

---

## 目录内容

```
UI交互/
├─ README.md                     ← 本文件
├─ PICO_UI交互实现指南.md          ← 先读这个（实现原理 + 步骤 + 坑）
├─ Scripts/                      ← 当前 UI 代码快照（只读参考）
│   ├─ RideUi.cs                 总控 + 所有界面构建（583 行）
│   ├─ MenuPanel.cs              通用焦点式菜单（动作/数值/开关）
│   ├─ InputRouter.cs            手柄输入抽象
│   ├─ UiTheme.cs                配色 / 字体 / 面板
│   ├─ RideSettings.cs           设置持久化
│   ├─ RideSession.cs            骑行统计
│   └─ XR/HeadPoseDriver.cs      头部姿态驱动相机（UI 定位依赖）
└─ Fonts/
    └─ simhei.ttf                界面中文字体（9.5 MB）
```

---

## 三句话说明现状

1. **UI 全是代码生成的**，场景里没有 Canvas，也不用 Prefab。改界面 = 改 `RideUi.cs` 里的 `BuildXxx()`。
2. **交互靠手柄摇杆**（上下选、左右调值、A 确认），没有射线、没有按钮点击。
3. **面板是 World Space**，1200×700 px、缩放 0.002、眼前 1.5 m——所以 1 px = 2 mm。

---

## 重要：这里是副本，不是工程

`Scripts/` 是快照。改完有两种回流方式：

- **推荐**：直接在工程里改（`Assets/Scripts/App/`），改完打包测试；
- 或者：在这份副本里改，改完把文件发回来合并——**别以为改完副本工程就变了**。

---

## 依赖（脱离工程单独打不开）

这些脚本依赖工程里的场景与设置，单独拷出来无法运行：

| 依赖 | 在哪 |
|---|---|
| 场景 | `Assets/BrokenVector/LowPolyRoadPack/Demo/Low Poly Road Pack Demo.unity` |
| XR Origin（脚本挂载点） | 场景根下的 `XR Origin` |
| PICO 设置 | `Assets/Resources/PXR_ProjectSetting.asset`（handTracking=1，eyeTracking=0） |
| XR 插件 | PICO SDK 3.4.0（嵌入包，改过源码，别重装） |

---

## 建议阅读顺序

1. `PICO_UI交互实现指南.md` 的 §3（现有架构）
2. `RideUi.cs` 的 `BuildCanvas()` 与 `ShowScreen()`
3. `MenuPanel.cs` 全文（184 行，最容易上手）
4. 指南 §5（加射线的五个步骤）
