# Shizuku 开发路线图

**当前发布版本**：v0.8.0

**最后更新**：2026-09-15

本文档只记录当前能力边界、近期工作和发布门槛。历史版本的具体改动以根目录和 UPM 包内的 `CHANGELOG.md` 为准。

---

## 项目定位

Shizuku 是面向 Unity Gameplay 编程的可视化 Graph / Blueprint 框架：

- **Graph**：通用执行容器，也是框架节点与运行时机制的主要测试入口。
- **Blueprint**：允许业务 `MonoBehaviour` 通过可覆写事件、变量和函数子图承载可视化逻辑，是框架的主要生产价值。
- **SkillEditor**：独立的技能时间轴与运行时模块，不作为 Graph / Blueprint 可投入使用的前置条件。
- **MCP**：可选的外部 Agent 接口，用稳定语义操作图资产；不在 Graph Editor 内维护聊天 UI，也不依赖第三方 Unity MCP 才能工作。

当前阶段优先保证序列化、运行时生命周期和消费项目兼容性，再根据真实项目反馈扩展功能。

---

## v0.8.0 已交付基线

### Graph / Blueprint 运行时

- [x] 控制流边、参数边、Root 执行与值节点 Pull 缓存
- [x] 常用基础类型、自定义类型注册与显式类型转换节点
- [x] 蓝图变量、默认值、GUID 引用、重命名与重名检查
- [x] 方法子图、动态参数端口与 `InvokeMethodNode`
- [x] Blueprint Event 参数传递、返回值路径与生成代码命名空间处理
- [x] `ShizukuLatentNode` 跨帧执行、重入保护与生命周期清理
- [x] `ShizukuGraphRuntime<TGraph>` 统一克隆、初始化、执行、Tick 和 Dispose
- [x] Timeline 播放节点的 Started / Completed / Failed 分支、动态 Track 绑定与非抢占式重入语义
- [x] 运行时循环执行与最大控制流深度保护
- [x] 节点端口反射缓存和边连接节点字典查找

### Graph Editor

- [x] 节点自动发现、分类菜单与搜索窗口
- [x] 节点复制粘贴，并迁移选中节点之间的内部边
- [x] 删除节点时同步清理控制流边和参数边
- [x] 主图 / 方法子图导航、变量与函数编辑面板
- [x] 节点、端口、选中状态与画布视觉整理
- [x] 初次打开自动定位完整图，普通刷新保留视口
- [x] 通用动态参数端口扩展契约
- [x] 通用序列化字段变更回调与延迟合并刷新
- [x] Addressables `AssetReference` 字段编辑与保存重载
- [x] Blueprint / 节点生成文件的项目级输出路径

### SkillEditor

- [x] Skill、Track、Clip 数据模型与多态序列化
- [x] Animation / VFX / SFX / Logic 内置轨道和运行时 Runner
- [x] 时间轴拖拽、缩放、帧吸附、Clip 时长与重叠规则
- [x] `SkillPlayer` 播放、停止、打断与 PlayableGraph 生命周期
- [x] `ShizukuSkillEditor.GraphIntegration`、`SkillGraph` 与 Graph Track / Clip 桥接

### MCP Agent 集成

- [x] 基于官方 Model Context Protocol C# SDK 的独立 stdio Server
- [x] 仅监听回环地址、使用项目级轮换令牌的 Unity Editor Bridge
- [x] 图列表、节点目录、读取、校验与事务式修改工具
- [x] dry-run、revision 冲突保护、Undo、SetDirty 与保存后刷新
- [x] Codex、Claude Code、Cursor 检测和项目级配置
- [x] 未知 Agent 的通用 stdio 配置
- [x] 发布包排除 MCP Server 的 `bin` / `obj` 生成物

### v0.8.0 发布验证

- [x] Shizuku EditMode：78 / 78
- [x] Shizuku PlayMode：4 / 4
- [x] 独立 MCP Server 构建与真实 stdio 握手
- [x] MCP 工具目录与真实图资产读取
- [x] UPM 包 `npm pack --dry-run`，不携带 Server 构建产物

---

## v0.8.x：消费项目验证与稳定性加固

这一阶段不主动扩大功能面，优先处理真实项目暴露的问题。

### P0：发布后验收

- [ ] 在独立消费项目中通过 Git Tag 全新安装 v0.8.0
- [ ] 验证 Odin 已安装、Shizuku 后安装时的首次编译流程
- [ ] 验证 `.NET 8 SDK` 首次构建 MCP Server 和重复配置流程
- [ ] 在 Project-Shiori 验证自定义 `AssetReference` Timeline 节点及动态 Track 端口
- [ ] 通过 MCP 完成 list → read → validate → dry-run → apply → 保存重载闭环
- [ ] 验证退出 Play Mode、Domain Reload 和重开图窗口后图资产不丢节点或边

### P0：文档与问题清单

- [ ] 重新审计 `KNOWN_ISSUES.md`，删除已经完成的端口缓存、字典查找和循环保护旧描述
- [ ] 将仍可复现的问题改写为最小复现、影响范围和验证条件
- [ ] 为 MCP 增加消费项目安装、Agent 配置和常见故障排查

### 仅在复现后修复

- 图资产保存、复制粘贴、动态端口或边迁移的回归
- Latent / Timeline 生命周期、重入与退出 Play Mode 的回归
- UPM 安装、宏开关、Odin 前置依赖或 MCP 构建兼容问题

---

## v0.9.0：Graph / Blueprint 生产化

### P1：运行时边界

- [ ] `BlueprintUpdateMode`：Disabled / Manual / Always，避免所有实例无条件每帧 Tick
- [ ] `ErrorHandlingMode`：明确 StopOnError / ContinueOnError 的控制流语义
- [ ] 将 Blueprint Event 编辑器校验反射移出运行时热路径，或增加稳定元数据缓存
- [ ] 修复循环与复合控制流节点命中断点后的恢复游标
- [ ] 为静态反射缓存补充 Domain Reload、禁用 Domain Reload 和 Player 生命周期测试

### P1：序列化与编辑可靠性

- [ ] 覆盖节点、边、变量、方法、动态端口的 Undo / Redo
- [ ] 增加旧版本图资产迁移测试和缺失节点类型的诊断
- [ ] 增加复制粘贴、删除节点、保存重载和 Play Mode 往返回归测试
- [ ] 为大图建立加载、初始化和执行基准，性能优化以数据为依据

### P2：类型和控制流扩展

- [ ] Quaternion 等常用 Unity 类型端口
- [ ] 集合类型端口与 ForEach 节点
- [ ] Delay / WaitUntil 等通用 Latent 示例节点
- [ ] Sequence / Parallel 等复合控制流；先定义重入、取消和销毁语义
- [ ] ShizukuFunction 的重载、可选参数和泛型方法策略
- [ ] Blueprint `Call Parent` 的明确调用语义

### P2：编辑体验

- [ ] 节点折叠与更完整的快捷键提示
- [ ] 多选对齐、分组和批量编辑
- [ ] Mini-map 与可选网格吸附
- [ ] 自动布局；必须保留手工布局并支持 Undo

---

## MCP 后续路线

MCP 的下一步由消费项目反馈驱动，不把“工具数量”当作目标。

### 近期候选

- [ ] 变量定义、方法子图和 Blueprint Event 的专用语义操作
- [ ] apply 前提供更细粒度的结构差异预览
- [ ] 自动布局作为显式、可 dry-run 的独立操作
- [ ] 更清晰的 Bridge / Server 诊断状态与错误信息
- [ ] 根据实际使用情况增加其他 Agent 配置器

### 安全边界

- MCP 写操作默认 dry-run
- 实际写入必须带 revision，避免覆盖并发修改
- Bridge 只监听本机回环地址，令牌不写入版本控制
- MCP 不直接操作 Unity 序列化文本，统一经过框架语义层、校验和保存流程

---

## SkillEditor 独立路线

以下工作不会阻塞 Graph / Blueprint 的生产化，可按具体游戏项目需求推进。

### 编辑器与预览

- [ ] 非 Play Mode 动画、特效和播放头同步预览
- [ ] Preview Target、Scene Gizmo、帧步进与循环播放
- [ ] Track 排序、折叠、锁定和 Clip 多选批量操作
- [ ] 评估是否需要独立 SkillGraph 窗口；没有明确收益时继续复用 Graph Editor

### 运行时

- [ ] `SkillPlayer` Pause / Seek / TimeScale
- [ ] 判定轨道与相机轨道
- [ ] 伤害、Buff、检测、冷却等通用技能节点
- [ ] 以真实战斗项目验证动画混合、打断、资源释放和大量实例性能

---

## v1.0.0 发布门槛

v1.0 不要求完成所有愿望清单，但必须满足以下条件：

- [ ] 至少一个独立消费项目完成完整版本周期验证
- [ ] Graph / Blueprint 公共 API 和序列化格式有明确兼容策略
- [ ] 没有已知 Critical 数据丢失或生命周期问题
- [ ] 核心保存、执行、Latent、动态端口和 MCP 写入流程具有自动化回归测试
- [ ] 建立可重复的 EditMode、PlayMode、Server 构建和 UPM 打包发布检查
- [ ] README、快速上手、扩展节点、动态端口、MCP 和故障排查文档与代码一致
- [ ] 给出大图初始化、运行时 Tick 和内存占用的基准数据

---

## 暂缓项目

在核心稳定性和消费项目验证完成前，不主动投入：

- 云端 AI 聊天 UI
- Lua / Python / Web 图导出
- 多人实时协作和云端蓝图库
- 公式编辑器、连招系统等强业务功能
- 仅为展示效果服务的数据流动画和复杂主题系统

这些方向可以保留为未来提案，但进入实现前必须先有明确消费场景和生命周期设计。
