# FreeBike — AI 入口

**项目根目录：`D:\freebike`。**
**权威交接文档：`D:\freebike\Docs\AI\FreeBike_项目交接文档.md`（每次开始前先完整读一遍）。**
**Word 版：`D:\freebike\Docs\AI\FreeBike_项目交接文档.docx`（由 md 生成，改 md 后需重新生成）。**

- `D:\freebike` 是唯一真实目录；旧 `D:\freebikeapk` 已删除，`C:\Users\admin\freebike` 仍是指向它的兼容 Junction（不是副本）。
- 原 `FreebikeAPK` SMB 共享已改指向 `D:\freebike\Releases\Android`。
- 历史文档中的 `D:\freebikeapk\...` 是旧路径，已迁移：文档在 `D:\freebike\Docs\`，视频在 `Media\`，构建产物在 `Releases\`，工具在 `Tools\`；一切以 `D:\freebike` 实际目录为准。
- 用户只需说“下一步目标：XXX”。
- 不要重新探索工程；按文档 §4 路径直接打开文件。
- 不要重复已完成内容与已失败方案（文档 §5、§6）。
- 一次只做一个目标；先给 3–6 步计划再动手；回复中文、简洁。
- 用户负责在 Unity 里编译/打包；AI 负责改文件与给步骤。
- 不要修改 Azure Kinect / PICO SDK 的驱动、安装目录与构建配置（任务明确要求时除外，并写入文档变更记录）。
- 每次任务结束更新交接文档的“当前状态/变更记录”。

会话模板：
“按 D:\freebike\Docs\AI\FreeBike_项目交接文档.md 继续。下一步目标：<目标>。”
