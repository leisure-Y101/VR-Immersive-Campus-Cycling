# FreeBike UI 修改文件清单

日期：2026-10-25  
用途：交接给其他设计/开发人员继续修改。

> 重要：新主页、圆弧按钮、3D 悬停、运动准备页、HUD 调整、倒计时、报告详情目前只有 PNG 预览，**尚未接入 Unity 代码**。  
> `CampusRideApp.cs` 里仍然是旧的运行时 UI。

---

## 0. 已确认需求与交互约定

### 0.1 总体方向

```text
设备：PICO 4 Pro
内容：360° 校园骑行
风格：深色玻璃 / 海军蓝 / 青色强调 / 切角面板
参考图：只参考风格，不照抄原图
背景：360 等距柱状图，用户后续提供
当前占位：Assets/Resources/homepage.jpg
不显示 3D 车把，不显示整车模型
主页、设置、帮助、报告、总结等使用同一块世界空间主面板切换内容
```

### 0.2 主页

标题：

```text
沉浸式校园骑行
PICO 4 Pro · 360° 校园骑行
```

六个按钮，顺序固定：

```text
1. 纯沉浸式体验    副文字：只看风景
2. 运动骑行        副文字：记录数据
3. 操作说明
4. 设置
5. 数据报告
6. 退出
```

按钮形态：

```text
沿一条浅弧线排列
按钮本身轻微弧形
第一项默认选中/悬停
```

3D 悬停：

```text
射线停在按钮上：放大到 1.12 倍，向观察者方向凸出 2cm
射线离开：回到 1.0 倍
点击：Trigger
右手射线优先；右手未命中时左手可用
```

### 0.3 页面流程

运动模式：

```text
主页
→ 运动骑行
→ 运动准备页
   ├─ 调整 HUD
   ├─ 开始
   └─ 返回主页
→ 调整 HUD
   ├─ 右手射线指向 HUD
   ├─ 按住 Grip 抓住
   ├─ 移动手柄调整位置和远近
   ├─ 松开 Grip 即保存
   └─ B 取消
→ 自动返回来源页面
→ 开始
→ 3 秒倒计时：3 → 2 → 1 → GO
→ 正式骑行
   ├─ B 暂停
   ├─ 暂停页：继续骑行 / 调整 HUD / 结束骑行
   └─ 结束骑行
→ 运动总结
   ├─ 生成并保存报告
   └─ 返回主页
```

沉浸式模式：

```text
主页
→ 纯沉浸式体验
→ 3 秒倒计时
→ 无数据 HUD 骑行
→ 结束
→ 总结
→ 返回主页
```

### 0.4 空间与跟随关系

| 界面 | 空间 | 是否跟随头部 |
|---|---|---|
| 主页 | 世界空间，正前方约 2.2m | 否 |
| 360 背景 | 球体 | 否 |
| 运动准备页 | 同一块世界面板 | 否 |
| HUD 调整 | HUD 组本身贴合视野 | 是 |
| 运动 HUD 底部 | 相机子物体，底部中央 | 是 |
| 心率 | 相机子物体，左上独立 | 是 |
| 倒计时 | 相机子物体，正前方 | 是 |
| 心率提醒 | 相机子物体，上方/中央 | 是 |
| 补水弹幕 | 相机球壳，约 2.2m | 是 |
| 暂停 | 世界空间，约 1.8m | 否 |
| 运动总结 | 世界空间，约 1.8m | 否 |
| 设置/帮助/报告/退出 | 同一块世界面板 | 否 |
| 报告详情 | 同一块世界面板 | 否 |

关键结论：

```text
只有 HUD、心率、倒计时、弹幕贴合视野。
主页和菜单类页面放在世界空间，不跟随头部。
```

### 0.5 双手柄交互

```text
左右手都支持射线
右手优先
右手未命中按钮时，左手可用
射线悬停放大
Trigger 确认
B 返回/取消
Grip 用于抓住 HUD
```

HUD 调整：

```text
来源：运动准备页或暂停页
操作：指向 HUD → 按住 Grip → 移动 → 松开 Grip
松开 = 保存并自动返回来源页面
不需要再按确认
B = 取消，恢复上一次保存的位置
Trigger 在调整模式里不作为确认键
```

返回规则：

```text
从运动准备页进入调整 → 回到运动准备页
从暂停页进入调整 → 回到暂停页
```

### 0.6 运动 HUD

HUD 分两组，但作为一组一起移动：

```text
左上：心率
底部中央：速度 / 时长 / 距离
```

左上心率：

```text
心率
136 bpm
模拟心率
```

底部 HUD：

```text
左侧：时长
中央：速度（大字）
右侧：距离
```

明确不显示：

```text
功率
踏频
卡路里
地图
路线名
菜单按钮
```

速度保留，因为用户确认参考图方案。  
心率是模拟值，必须有“模拟心率”标记。

### 0.7 速度与距离

视频基准速度：

```text
12 km/h = 视频 1.0 倍
```

当前播放倍率范围：

```text
0.85 – 1.25
```

显示速度：

```text
显示速度 = 12 × 视频播放倍率
```

范围：

```text
0.85 倍 → 10.2 km/h
1.00 倍 → 12.0 km/h
1.25 倍 → 15.0 km/h
```

规则：

```text
最高允许显示 15.0，不封顶到 12
暂停时显示 0
做平滑，避免跳变
距离按显示速度积分
HUD 速度保留一位小数
```

### 0.8 心率与提醒

```text
心率阈值：130 bpm
超限提醒必须包含当前心率
例如：心率 136，慢一点
显示 5 秒
仍高于阈值时，间隔 20 秒再次提醒
运动模式：HUD 心率区域变红/闪烁
沉浸式模式：中央/上方心率提醒
```

补水提醒：

```text
Demo：第 1 分钟提醒一次
正常：每 10 分钟提醒一次
仅运动模式
弹幕从右向左
约 5 秒
```

### 0.9 倒计时

```text
运动模式和沉浸式都使用
3 → 2 → 1 → GO
每秒切换
贴合视野，正前方中央
视频暂停在第一帧
倒计时结束才开始计时、距离、速度
先做画面，提示音后续接入
```

### 0.10 运动准备页

```text
标题：运动骑行
副标题：调整 HUD 到舒服的位置，然后开始
HUD 预览：底部中央 + 左上心率
按钮：
调整 HUD
开始
返回主页
```

### 0.11 暂停页

```text
已暂停
继续骑行
调整 HUD
结束骑行
```

### 0.12 总结页

运动模式：

```text
骑行数据
骑行时长
最高心率
骑行距离

生成并保存报告
返回主页
```

沉浸式模式：

```text
骑行已结束
骑行时长
最高心率
骑行距离

返回主页
```

总结页只保留三项核心数据，速度不进总结页。

### 0.13 报告

保存目录：

```text
D:\freebikeapk\CyclingReport
```

列表页字段：

```text
日期
时长
距离
最高心率
右侧箭头
```

点击一行进入详情页。详情页字段：

```text
日期
骑行时长
最高心率
平均心率
骑行距离
卡路里
平均速度
最高速度
心率来源（模拟）
```

### 0.14 设置与语言

```text
设置
语言
中文
English
Español
Français
Português

重置 HUD 位置
返回主页
```

语言使用展开列表，点击立即切换。

### 0.15 视觉规范

```text
主色：#061421
面板：#051421
青色：#22D3EE
文字：#EEF7FC
次级文字：#96BEC9
警示红：#FF4D4D

主页面板：约 1090×776
菜单卡片：约 140×178
按钮基准：300×380（竖版，9-slice）
HUD 底部：约 880×190
HUD 心率：约 330×112
中央提醒：约 900×120
倒计时：中央圆环
```

文字不烘进底图，必须由 Unity Text 动态渲染，保证五种语言切换。

### 0.16 尚未完成

```text
新 UI 尚未接入 Unity
目前只有 PNG 预览和 Pillow 生成脚本
没有 Prefab / Theme / ScriptableObject
没有透明九宫格切图
没有 PICO 双手射线接入
没有 3D 按钮悬停逻辑
没有准备页、HUD 调整、倒计时、报告详情的 Unity 实现
背景图尚未提供
提示音尚未接入
手柄帮助页仍是线稿，最终需换 PICO 官方模型
```

---

## 1. UI 设计稿

目录：

```text
D:\freebikeapk\UI\preview\
```

总览：

```text
D:\freebikeapk\UI\preview\00_contact_sheet.png
```

单页：

```text
D:\freebikeapk\UI\preview\01_main_menu.png
D:\freebikeapk\UI\preview\02_sport_hud.png
D:\freebikeapk\UI\preview\03_countdown.png
D:\freebikeapk\UI\preview\04_button_states.png
D:\freebikeapk\UI\preview\05_sport_prep.png
D:\freebikeapk\UI\preview\06_hud_adjust.png
D:\freebikeapk\UI\preview\07_pause.png
D:\freebikeapk\UI\preview\08_sport_summary.png
D:\freebikeapk\UI\preview\09_help.png
D:\freebikeapk\UI\preview\10_settings.png
D:\freebikeapk\UI\preview\11_reports.png
D:\freebikeapk\UI\preview\12_report_detail.png
D:\freebikeapk\UI\preview\13_exit.png
D:\freebikeapk\UI\preview\14_immersive_riding.png
D:\freebikeapk\UI\preview\15_immersive_summary.png
```

说明：

- 这些是 Pillow 生成的 PNG 预览图；
- 没有 PSD / Figma / 分层源文件；
- 只能作为布局、配色和尺寸参考；
- 正式使用需要重新绘制九宫格和透明切图。

---

## 2. UI 生成脚本

```text
D:\freebikeapk\UI\make_ui_preview.py
D:\freebikeapk\UI\make_ui_screens.py
```

---

## 3. Unity 主 UI 代码

主菜单、HUD、语言、总结、报告：

```text
C:\Users\admin\freebike\Assets\Scripts\App\CampusRideApp.cs
```

手柄输入：

```text
C:\Users\admin\freebike\Assets\Scripts\App\InputRouter.cs
```

旧 UI 自动启动屏蔽：

```text
C:\Users\admin\freebike\Assets\Scripts\App\RideUi.cs
```

注意：

- 当前主页仍然是代码生成的旧 UI；
- 新设计没有对应的 Prefab、Theme 或 ScriptableObject；
- 直接修改 Play 模式里的对象不会保存。

---

## 4. Unity 场景

正式 PC 场景：

```text
C:\Users\admin\freebike\Assets\Scenes\panoTest.unity
```

本地视频测试场景：

```text
C:\Users\admin\freebike\Assets\Scenes\PicoVideoBenchmark.unity
```

---

## 5. 本地视频播放相关脚本

```text
C:\Users\admin\freebike\Assets\Scripts\Video\IVideoBackend.cs
C:\Users\admin\freebike\Assets\Scripts\Video\UnityVideoBackend.cs
C:\Users\admin\freebike\Assets\Scripts\Video\PackagedVideoInstaller.cs
C:\Users\admin\freebike\Assets\Scripts\Video\PicoVideoBenchmark.cs
```

编辑器构建脚本：

```text
C:\Users\admin\freebike\Assets\Editor\PicoBenchmarkBuilder.cs
C:\Users\admin\freebike\Assets\Editor\PicoBenchmarkSceneBuilder.cs
```

---

## 6. 构建配置

```text
C:\Users\admin\freebike\ProjectSettings\EditorBuildSettings.asset
```

当前该文件把 `PicoVideoBenchmark` 放在构建场景列表第 0 位，`panoTest` 被取消勾选。

---

## 7. 背景与视频资源

```text
C:\Users\admin\freebike\Assets\Resources\homepage.jpg
C:\Users\admin\freebike\Assets\StreamingAssets\Videos\bench_4k60.mp4
C:\Users\admin\freebike\Assets\StreamingAssets\Videos\bench_57k30.mp4
```

说明：

- `homepage.jpg` 是当前占位背景；
- 两个 MP4 已打包进 APK；
- 新背景图尚未提供。

---

## 8. 构建产物

```text
D:\freebikeapk\Builds\PicoVideoBenchmark.apk
```

测试安装脚本：

```text
D:\freebikeapk\Builds\install_benchmark.cmd
```

---

## 9. 结论

当前可直接交给 UI 设计师的文件：

```text
D:\freebikeapk\UI\preview\
D:\freebikeapk\UI\make_ui_preview.py
D:\freebikeapk\UI\make_ui_screens.py
```

当前可直接交给 Unity 开发的文件：

```text
C:\Users\admin\freebike\Assets\Scripts\App\CampusRideApp.cs
C:\Users\admin\freebike\Assets\Scripts\App\InputRouter.cs
C:\Users\admin\freebike\Assets\Scripts\App\RideUi.cs
C:\Users\admin\freebike\Assets\Scenes\panoTest.unity
C:\Users\admin\freebike\Assets\Scenes\PicoVideoBenchmark.unity
```

新 UI 接入前，需要先确定：

```text
1. 是否走 Prefab 方案
2. 是否保留旧的运行时 UI
3. 新背景图
4. 九宫格切图
5. PICO 双手射线
```
