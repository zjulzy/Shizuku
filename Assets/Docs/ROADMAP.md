# Shizuku 开发路线图

本文档概述 Shizuku 项目的发展方向和里程碑规划。

---

## 🎯 项目愿景

**将 Shizuku 打造成 Unity 生态中最好用的可视化蓝图系统。**

核心目标：
- 🎨 **直观易用** - 降低游戏逻辑开发门槛
- ⚡ **性能优异** - 运行时性能接近手写代码
- 🔧 **灵活扩展** - 支持各种游戏类型和自定义需求
- 🛡️ **稳定可靠** - 适合生产环境使用

---

## 📅 版本规划

### 🚀 v0.2.0 - 基础完善 ✅（大部分完成）

**目标**：完善核心功能，提升开发体验。

#### 功能列表

**1. 值节点缓存机制** ⭐⭐⭐ ✅
- [x] 同一拉取代（Pull Generation）内避免重复计算
- [x] `ShizukuValueNode` 基类实现 `_lastComputedGeneration` 缓存
- [x] 实现 `OnComputeOutputValues()` 接口
- [x] `ShizukuRunnableNode.Execute()` 递增全局 `CurrentPullGeneration`

**2. 节点库自动发现** ⭐⭐⭐ ✅
- [x] 通过反射扫描所有 `ShizukuNodeBase` 子类
- [x] 自动生成分类菜单
- [x] 支持 `[NodeMenuItem]` 特性标注元数据
- [x] 节点搜索功能（Unity SearchWindow）

**3. 扩展类型系统** ⭐⭐ ✅
- [x] 新增 `Vector3ParameterEdgePort`
- [x] 新增 `Vector2ParameterEdgePort`
- [x] 新增 `GameObjectParameterEdgePort`
- [x] 新增 `TransformParameterEdgePort`
- [x] 新增 `ColorParameterEdgePort`
- [x] 类型注册中心（支持自定义类型）— `ShizukuTypeRegistry`

**4. 更多实用节点** ⭐⭐ ✅
- [x] 数学节点：Add, Subtract, Multiply, Divide, Clamp, Lerp
- [x] 逻辑节点：And, Or, Not, Xor, Compare (支持 Int/Float/String/Bool 类型比较)
- [x] 工具节点：SetActive, FindGameObject, HasComponent, GetTransform, Get/SetPosition
- [x] 属性节点：GetProperty, SetProperty（通用版本）

**5. 蓝图自定义变量** ⭐⭐⭐ ✅
- [x] **变量定义系统**
  - [x] 在蓝图中定义局部变量
  - [x] 支持常见类型（Int, Float, Bool, String, Vector2/3, GameObject, Transform, Color）
  - [x] 变量默认值设置
  - [x] 变量序列化和持久化
  - [x] 零装箱优化（分类型字典存储）
- [x] **变量访问节点**
  - [x] Get Variable 节点（读取变量值）
  - [x] Set Variable 节点（设置变量值）
  - [x] GUID引用系统（支持重命名）
- [x] **变量管理界面**
  - [x] 变量列表面板（右侧面板）
  - [x] 添加/删除/重命名变量
  - [x] 变量类型修改
  - [x] 变量默认值编辑（所有类型）
  - [ ] 变量分组/排序
- [x] **编辑器增强**
  - [x] 变量选择器（下拉菜单）— `CreateVariableGUIDSelector`
  - [x] 节点上显示变量名 — `GetDisplayName()` / `RefreshNodeTitle()`
- [ ] **变量作用域**
  - [x] 局部变量（当前蓝图）

**6. 编辑器优化** ⭐
- [ ] 节点搜索框（Ctrl+Space 唤起）
- [ ] 节点折叠/展开
- [ ] 快捷键支持（复制/粘贴）
- [ ] 网格吸附

**7. 性能优化** ⭐⭐⭐
- [ ] **Update 执行优化**
  - [ ] 添加 `UpdateMode` 枚举（Always/OnDemand/Disabled）
  - [ ] 检测 Root Node 是否有实际逻辑
  - [ ] 提供 `EnableUpdate`/`DisableUpdate` API
- [ ] **批量初始化优化** - 静态缓存共享（已实现）

**8. 运行时错误处理** ⭐⭐⭐ ✅（基础完成）
- [x] **基础异常捕获**
  - [x] `ShizukuRunnableNode.Execute` 包裹 try-catch
  - [x] 错误时输出节点类型 + GUID + 异常信息
  - [x] 循环依赖运行时保护（`_executing` 标志）
  - [x] 执行链深度保护（防 StackOverflow）
- [x] **结构化错误上下文**
  - [x] GameObject / BlueprintAsset / 执行路径（`ShizukuExecutionContext` + `ShizukuErrorReporter`）
  - [x] `InvokeMethodNode` 自动压入 Method 帧，跨函数定位
  - [x] `Debug.LogError(msg, Owner)` 让 Console 双击跳转到出错对象
- [ ] **错误恢复机制 ErrorHandlingMode**（StopOnError / ContinueOnError）
- [ ] **调试日志增强**：错误码、文档链接、自定义处理器

**9. 数据类型传递优化** ⭐⭐⭐ ✅
- [x] **显式类型转换节点**
  - [x] 实现 `TypeConverterNode` 基类
  - [x] 实现常用转换节点（Float↔Int, Vector3↔Vector2, String↔Number）
  - [x] 实现 `ConverterNodeRegistry` 自动注册中心
  - [x] 编辑器自动插入转换节点（连接时检测类型）
  - [x] 视觉反馈（蓝色端口表示需要转换，禁止不可转换连接）
- [x] **扩展类型支持**
  - [x] Vector3/Vector2/GameObject/Transform/Color 类型端口
  - [ ] Quaternion 等其他 Unity 常用类型

**10. ShizukuClass 和 ShizukuFunction 支持** ⭐⭐⭐⭐ ✅
- [x] **Attribute 定义**
  - [x] `[ShizukuClass]` - 标记类可在蓝图中使用
  - [x] `[ShizukuFunction]` - 标记方法可生成蓝图节点
  - [x] 支持元数据（DisplayName、Category、Description 等）
- [x] **注册中心**
  - [x] `ShizukuTypeRegistry` - 反射扫描和注册系统
  - [x] 类型信息缓存（ShizukuClassInfo、ShizukuFunctionInfo）
  - [x] 编辑器下自动重新扫描（InitializeOnLoad）
- [x] **自定义类型支持**
  - [x] 作为变量类型（GraphVariable 扩展）
  - [x] 作为端口类型（CreatePortForType 扩展）
  - [x] 泛型端口类 `ShizukuClassParameterEdgePort<T>`
- [x] **函数节点代码生成**
  - [x] `ShizukuFunctionNodeGenerator` - 代码生成工具
  - [x] 为每个 [ShizukuFunction] 生成专用节点类
  - [x] 自动创建输入/输出端口
  - [x] 支持参数和返回值映射
  - [x] 生成节点管理窗口
- [x] **SearchWindow 集成**
  - [x] 在节点搜索中显示函数节点
  - [x] 按类型/分类组织菜单
  - [x] 支持快速搜索和过滤
- [ ] **高级功能**
  - [ ] 泛型方法支持（指定具体类型）
  - [x] 实例方法 vs 静态方法
  - [ ] 重载方法处理
  - [ ] 可选参数和默认值

---

### 🔍 v0.3.0 - 调试工具 ✅（核心完成）


**目标**：提供强大的调试能力，提升开发效率。

#### 功能列表

**1. 调试器基础架构** ⭐⭐⭐ ✅
- [x] `ShizukuDebugger` 静态管理器
- [x] 节点执行历史记录（帧缓冲双队列）
- [x] 执行状态可视化（运行中/暂停/断点命中）
- [x] 断点快照系统（`DebugSnapshot` + `RuntimeVariableStore.Clone`）

**2. 断点系统** ⭐⭐⭐ ✅
- [x] 节点断点设置/移除（`ShizukuDebugger` 全局 `HashSet<GUID>` 管理，与图实例解耦）
- [x] 断点触发时暂停执行（快照 + 恢复点机制）
- [x] 继续执行 / 单步执行（`ShizukuDebugger.ResumeExecute`）
- [ ] 条件断点（高级）

**3. 变量监视** ⭐⭐ ✅
- [x] 断点快照变量查看（调试信息面板）
- [x] 监视窗口（变量面板显示快照时刻变量值）
- [ ] 支持表达式求值（高级）

**4. 执行可视化** ⭐⭐ ✅
- [x] 当前执行节点高亮显示（绿色边框 `.debug-executed`）
- [x] 断点暂停节点高亮（黄色边框 `.debug-paused`）
- [x] 断点红色标记（节点左侧红点）
- [ ] 执行路径动画
- [ ] 数据流动画（显示值传递过程）
- [ ] 性能热点标注（慢节点标红）

**5. 调试编辑器 UI** ⭐⭐⭐ ✅
- [x] 调试工具栏（Debug 开关/继续/单步/停止按钮）
- [x] 调试状态显示（当前状态、暂停节点名、帧号）
- [x] 调试信息面板（快照变量值查看）
- [x] 右键菜单断点操作
- [x] Play 模式自动清理

**6. 性能分析** ⭐
- [ ] 节点执行耗时统计
- [ ] 调用次数统计
- [ ] 性能报告导出
- [ ] 瓶颈分析建议
---

### 🧩 v0.4.0 - 高级功能（部分完成）


**目标**：支持复杂场景，提升代码复用性。

#### 功能列表

**1. 函数/子图系统** ⭐⭐⭐ ✅
- [x] **ShizukuMethod 子图架构**
  - [x] `ShizukuMethod` 实现 `INodeContext`，拥有独立节点/边/分组列表
  - [x] `INodeContext` 接口统一图/函数上下文（节点查找解耦）
  - [x] 函数内部独立初始化、边连接
- [x] **函数节点**
  - [x] `MethodEntryNode` — 函数入口节点，动态输出端口
  - [x] `MethodReturnNode` — 函数返回节点，动态输入端口，收集返回值
  - [x] `InvokeMethodNode` — 调用函数节点，注入参数→执行子图→收集返回值
  - [x] `MethodPort` — 动态端口类，根据 `VariableType` 创建对应类型端口
- [x] **函数参数系统**
  - [x] 输入参数列表（`InputParameters`）
  - [x] 输出参数列表（`OutputParameters`）
  - [x] 端口自动同步（`SyncPortsFromMethod`）
- [x] **编辑器支持**
  - [x] 函数列表面板（右侧面板，添加/删除/重命名）
  - [x] 函数卡片 UI（展开/折叠、参数编辑、双击进入）
  - [x] 面包屑导航（`EnterMethodGraph` / `ReturnToMainGraph`）
  - [x] 参数编辑（名称、类型下拉、添加/删除参数行）
  - [x] 调用节点自动同步（`SyncAllInvokeMethodNodes`）

**2. 蓝图函数扩展** ⭐⭐⭐
- [ ] **有返回值的方法重写**
  - [ ] 事件系统扩展：`Action<>` → `Func<>`
  - [ ] `BlueprintEventNode` 增加返回值输出端口
- [ ] **父类方法调用（Call Parent）**
  - [ ] 新增 `CallParentNode` 节点
  - [ ] 支持在蓝图中调用 C# 原始实现
- [ ] **纯函数优化**
  - [ ] 标记为纯函数（无副作用）
  - [ ] 自动缓存优化
  - [ ] 内联优化（未来）

**3. 时间轴支持** ⭐⭐⭐
- [ ] 延迟执行节点（Delay）
- [ ] 等待条件节点（WaitUntil）
- [ ] 序列执行容器（Sequence）
- [ ] 并行执行容器（Parallel）
- [ ] 协程支持


**4. 循环节点完善** ⭐⭐ ✅（核心完成）
- [x] For 循环（指定次数）— `ShizukuForNode` 已实现，支持 start/end/step + 子链执行 + 迭代上限保护
- [ ] ForEach 循环（遍历集合）— 需要先实现集合类型端口
- ~~[ ] While 循环（条件循环）~~ — 已弃用，纯定时循环用 For 即可
- ~~[ ] Break / Continue 支持~~ — 已弃用，子链结束即跳出

**5. 异常处理** ⭐
- [ ] Try-Catch 节点
- [ ] 错误日志节点
- [ ] 异常恢复机制

---

### 🎮 v0.5.0 - 技能系统专用 ✅（核心完成）

> **当前进度**：技能数据模型、运行时引擎（含 PlayableGraph 动画混合）、时间轴编辑器 UI 均已完成；剩余工作集中在编辑器预览、技能节点库、桥接图模块。

**目标**：为 ARPG/RPG 游戏提供开箱即用的技能系统，以**时间轴编辑器**为核心。

#### 功能列表

**1. 技能数据模型** ⭐⭐⭐⭐ ✅
- [x] **`ShizukuSkillConfig`（ScriptableObject）**
  - [x] 技能基础属性（名称、时长）
  - [x] 时间轴总时长（`Duration`）
  - [x] 轨道列表（`List<SkillTrack>` + `[SerializeReference]`）
  - [ ] 技能标签 / 分类（暂未实现）
- [x] **运行时实例**：通过 `SkillPlayer.Play(config, ctx)` 直接驱动，无独立 ShizukuSkill 类
  - [x] 运行时状态管理（IsPlaying / CurrentTime / Duration）
  - [x] SkillContext 注入

**2. 技能上下文** ⭐⭐⭐ ✅
- [x] `SkillContext` 类（Caster / Target / CastPosition / CasterAnimator / Player）
- [x] 上下文在 TrackRunner 间传递
- [x] SkillPlayer.Play 时构造，Stop/Interrupt 时释放

**3. 时间轴数据结构** ⭐⭐⭐⭐ ✅
- [x] **`SkillTrack`（轨道）** — 抽象基类 + `[TrackRunner]` 绑定 Runner
  - [x] AnimationTrack / EffectTrack / LogicTrack
  - [x] 启用/禁用（`Enabled`）
  - [x] Clips 列表（`List<SkillClip>` + `[SerializeReference]`）
  - [ ] 锁定（暂未实现）
- [x] **`SkillClip`（关键帧/片段）** — 抽象基类 + `[ClipForTrack]` 绑定轨道
  - [x] StartTime / Duration / EndTime
  - [x] 多态：AnimationClipData、VfxClipData、SfxClipData、LogicClipData
  - [x] 序列化（[SerializeReference]）
- [x] **内置轨道类型**
  - [x] 动画轨道（AnimationClip + BlendIn/Out + 自定义曲线）
  - [x] 特效轨道（Prefab + AttachBone + Offset）
  - [x] 音效轨道（AudioClip + Volume）
  - [x] 逻辑事件轨道（EventName）
  - [ ] 判定轨道（暂未实现，预留 LogicTrack 拓展）
  - [ ] 相机轨道（暂未实现）
  - [ ] 蓝图事件轨道 → 由桥接包 `skilleditor-graph` 提供（待办）

**4. 时间轴编辑器 UI** ⭐⭐⭐⭐ ✅
- [x] **轨道区域**（左侧轨道头 + 右侧时间线，独立滚动）
  - [x] 轨道列表渲染
  - [x] 添加/删除轨道（右键菜单反射 `[TrackRunner]`）
  - [x] 启用/禁用
  - [ ] 排序、折叠/展开、锁定（暂未实现）
- [x] **时间线区域**
  - [x] 时间刻度尺（按帧 + 半秒标签）
  - [x] 播放头（Playhead）
  - [x] 缩放（滚轮）+ 横向滚动条
- [x] **关键帧编辑**
  - [x] Clip 色块可视化
  - [x] 拖动移动 + 拖动边缘调整时长 + 帧吸附
  - [x] 右键菜单（反射 `[ClipForTrack]` 自动列出）
  - [x] Clip 重叠规则（`[AllowClipOverlap]` Attribute，仅动画允许重叠且最多 2 层）
  - [ ] 多选与批量操作（暂未实现）
- [x] **检查器面板**
  - [x] 三层结构（Skill / Track / Clip 信息叠加显示）
  - [x] 选中 Clip 显示属性
  - [x] AnimationClipData 曲线（BlendInCurve / BlendOutCurve）编辑
- [x] **工具栏**
  - [x] 双击 ScriptableObject 打开
  - [ ] 新建/打开/保存（暂依赖 Unity 默认）
  - [x] 帧吸附（Snap to Frame，已固化）

**5. 时间轴运行时引擎** ⭐⭐⭐⭐ ✅
- [x] **`SkillPlayer`（MonoBehaviour）**
  - [x] 持久化 PlayableGraph（Awake 创建 / OnDestroy 销毁）
  - [x] 按时间推进，驱动所有轨道
  - [x] 动画槽位池（AcquireSlot / ReleaseSlot / SetSlotWeight / SetSlotTime）
  - [x] 双层 LayerMixer：Layer0 = AnimatorController，Layer1 = 技能动画混合
  - [x] 播放、停止、打断
  - [ ] 暂停 / Seek / TimeScale（暂未实现）
- [x] **轨道执行器（`ITrackRunner` + `[TrackRunner]` 绑定）**
  - [x] 每种轨道类型实现对应执行器
  - [x] OnSkillStart / OnTick / OnSkillEnd / OnSkillInterrupt
  - [x] SimpleTrackRunner（通用） + AnimationTrackRunner（PlayableGraph 槽位插拔）
  - [x] ClipHandlerRegistry 工厂注册
- [x] **事件调度**
  - [x] 持续事件（OnEnter / OnUpdate / OnExit）
  - [x] VfxClipHandler / SfxClipHandler / LogicClipHandler
- [x] **技能生命周期**：Play → Tick → Stop / Interrupt 全流程

**6. 编辑器预览系统** ⭐⭐⭐
- [ ] **编辑器内预览播放**
  - [ ] 非 Play 模式下预览动画和特效
  - [ ] 预览播放头与时间轴同步
  - [ ] 预览对象（Preview Target）选择
- [ ] **Scene 视图集成**
  - [ ] 预览时在 Scene 视图显示判定框 Gizmo
  - [ ] 特效挂点可视化
- [ ] **播放控制**
  - [ ] 对接已有的 ▶ ⏸ ⏹ 按钮
  - [ ] 帧步进（逐帧预览）
  - [ ] 循环播放

**7. 技能节点库** ⭐⭐⭐（依赖图桥接子模块）
- [x] **图桥接子模块 `ShizukuSkillEditor.GraphIntegration`** — 连接技能与图模块
  - 方案：与 SkillEditor 同包，通过 `SHIZUKU_GRAPH` 宏 + asmdef `defineConstraints` 选择性编译；
    图插件存在时由 `ShizukuGraphDefineSymbol`（`[InitializeOnLoad]`）自动注入宏。
  - [x] `SkillGraph : ShizukuGraphBase` + 持有 `SkillContext`
  - [x] `GraphClipData` / `GraphTrack`（复用 `SimpleTrackRunner`）+ `GraphClipHandler`
  - [x] `ClipHandlerRegistry` 自动注册（运行时 + Editor）
  - [x] `GraphClipEditorBootstrap`：Inspector 自定义绘制 + 双击跳转 SkillGraph 资产
  - [ ] SkillGraph 专用编辑器窗口（当前直接复用 `ShizukuGraphWindow`，UI 改进待补）
- [ ] **伤害计算**：DealDamage, DealDamageOverTime, AreaDamage
- [ ] **效果触发**：SpawnEffect, PlaySound, CameraShake
- [ ] **检测判定**：RaycastCheck, SphereCheck, LineOfSightCheck
- [ ] **Buff 系统**：ApplyBuff, RemoveBuff, CheckBuff
- [ ] **冷却管理**：SetCooldown, CheckCooldown

**8. 伤害计算公式** ⭐⭐
- [ ] 公式编辑器（类似 Excel 公式）
- [ ] 常用公式预设（物理/魔法/真实伤害）
- [ ] 属性查询节点
- [ ] 随机数节点（带种子）

**9. 技能指示器集成** ⭐
- [ ] 范围指示器节点
- [ ] 方向指示器节点
- [ ] 轨迹预测节点

**10. 技能链系统** ⭐
- [ ] 连招检测
- [ ] 技能衔接节点
- [ ] Combo 计数

---


### 📐 v0.6.0 - 编辑器增强

**目标**：提升大型项目的编辑体验。

#### 功能列表

**1. Mini-map** ⭐⭐
- [ ] 缩略图导航
- [ ] 当前视口标识
- [ ] 点击跳转

**2. 撤销/重做** ⭐⭐⭐
- [ ] 命令模式实现
- [ ] 支持所有编辑操作
- [ ] 历史记录查看

**3. 多选编辑** ⭐⭐
- [ ] 批量修改属性
- [ ] 对齐工具
- [ ] 分组编辑

**4. 主题系统** ⭐
- [ ] 明亮/暗黑主题
- [ ] 自定义颜色方案
- [ ] 节点样式预设

**5. 自动布局** ⭐⭐
- [ ] 层次布局算法
- [ ] 节点对齐
- [ ] 路径优化

**6. 注释系统** ⭐
- [ ] 文本注释节点
- [ ] 区域标注
- [ ] TODO 标记

---

### 🤖 v0.7.0 - AI 助手（UI 外壳已搭建）

**目标**：提供智能助手，提升开发效率和学习体验。

> **当前进度**：`ShizukuGraphView.AIAssistant.cs` 已搭建基本 UI（聊天窗口、消息气泡、输入框、发送按钮），但回复为硬编码占位（"✅ 已完成"），尚无命令解析和智能逻辑。

#### 功能列表

**1. AI 助手 UI 外壳** ⭐ ✅
- [x] 聊天窗口 UI（消息气泡、输入框、发送按钮）
- [x] 最小化/展开切换
- [x] 回车键发送
- [ ] 命令历史记录（上下键翻阅）

**2. 基础命令系统** ⭐⭐⭐
- [ ] 规则引擎命令解析
- [ ] 节点创建命令（"创建打印节点"）
- [ ] 节点查找命令（"查找 OnUpdate"）
- [ ] 帮助和文档查询

**3. 智能提示** ⭐⭐
- [ ] 输入自动补全
- [ ] 常用命令快捷按钮
- [ ] 上下文感知建议
- [ ] 错误提示和纠正

**4. 云端 AI 集成（可选）** ⭐⭐
- [ ] OpenAI API 支持
- [ ] API Key 配置界面
- [ ] 自然语言理解
- [ ] 对话式交互
- [ ] 成本监控和限额

**5. 进阶功能** ⭐
- [ ] 蓝图优化建议
- [ ] 节点推荐系统
- [ ] 学习用户习惯
- [ ] 代码生成（自定义节点）

**实现方案**：详见 [AI 助手方案文档](AI_ASSISTANT_SOLUTIONS.md)

---

### 🚀 v1.0.0 - 生产就绪


**目标**：达到生产环境使用标准。

#### 里程碑要求

**1. 功能完整性**
- [ ] 所有计划功能实现
- [ ] 覆盖 80% 以上常见需求
- [ ] 至少 100 个内置节点

**2. 稳定性**
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试覆盖核心流程
- [ ] 零已知 Critical Bug
- [ ] 性能测试通过

**3. 文档完善**
- [ ] 完整的 API 文档
- [ ] 详细的教程（文字 + 视频）
- [ ] 至少 10 个完整示例项目
- [ ] 常见问题解答

**4. 社区支持**
- [ ] 活跃的 Discord/QQ 群
- [ ] Issue 响应时间 < 48h
- [ ] 定期更新博客

**5. 性能优化**
- [ ] 代码生成器（蓝图编译为 C#）
- [ ] 运行时性能接近手写代码
- [ ] 内存占用优化
- [ ] 启动时间 < 100ms

---

## 🔮 未来展望（v2.0+）

### 高级特性
- [ ] **可视化脚本语言**：完整的编程语言特性
- [ ] **多人协作**：实时协同编辑
- [ ] **版本控制集成**：Git Diff/Merge 支持
- [ ] **云端蓝图库**：分享和下载社区蓝图
- [ ] **AI 辅助增强**：本地模型、语音输入、智能优化（详见 [AI 方案](AI_ASSISTANT_SOLUTIONS.md)）

### 跨平台支持
- [ ] **导出 Lua**：用于热更新
- [ ] **导出 Python**：用于工具脚本
- [ ] **Web 编辑器**：浏览器中编辑蓝图

### 游戏类型适配
- [ ] **AI 行为树**：专门的 AI 节点库
- [ ] **对话系统**：对话树编辑器
- [ ] **关卡脚本**：关卡事件和触发器
- [ ] **UI 流程**：UI 状态机

---

## 📊 优先级矩阵

| 功能 | 重要性 | 紧急度 | 难度 | 状态 | 优先级 |
|-----|-------|-------|------|------|-------|
| 值节点缓存 | 高 | 高 | 低 | ✅ 完成 | ⭐⭐⭐ |
| 节点自动发现 | 高 | 高 | 中 | ✅ 完成 | ⭐⭐⭐ |
| 调试工具 | 高 | 中 | 高 | ✅ 核心完成 | ⭐⭐⭐ |
| 函数/子图系统 | 高 | 中 | 高 | ✅ 核心完成 | ⭐⭐⭐ |
| 循环节点 | 高 | 中 | 中 | ✅ For 完成 | ⭐⭐⭐ |
| 运行时错误处理 | 高 | 中 | 中 | ✅ 基础完成 | ⭐⭐⭐ |
| 工具节点 | 高 | 中 | 低 | ✅ 完成 | ⭐⭐ |
| 对象池 | 中 | 中 | 低 | ✅ 完成 | ⭐⭐ |
| 技能数据模型 | 高 | 高 | 中 | ✅ 完成 | ⭐⭐⭐ |
| 技能时间轴编辑器 | 高 | 高 | 高 | ✅ 完成 | ⭐⭐⭐⭐ |
| 技能时间轴运行时 | 高 | 高 | 高 | ✅ 完成 | ⭐⭐⭐⭐ |
| 技能编辑器预览 | 中 | 中 | 中 | ❌ 未开始 | ⭐⭐⭐ |
| 桥接包 skilleditor-graph | 高 | 高 | 高 | ❌ 未开始 | ⭐⭐⭐⭐ |
| 技能节点库 | 中 | 中 | 中 | ❌ 未开始 | ⭐⭐⭐ |
| 反射缓存优化 | 中 | 中 | 低 | ❌ 未开始 | ⭐⭐ |
| BlueprintBehavior UpdateMode | 高 | 中 | 低 | ❌ 未开始 | ⭐⭐⭐ |
| 撤销/重做 | 中 | 高 | 中 | ❌ 未开始 | ⭐⭐ |
| 时间轴/协程（蓝图） | 中 | 中 | 高 | ❌ 未开始 | ⭐⭐ |
| AI 助手（基础） | 中 | 低 | 低 | 🔨 UI 外壳 | ⭐⭐ |
| 代码生成器 | 中 | 低 | 高 | ❌ 未开始 | ⭐⭐ |
| AI 助手（云端） | 低 | 低 | 中 | ❌ 未开始 | ⭐ |

---

## 📋 未完成功能速览

以下是截至当前所有**未完成**的功能项，按优先级排序：

### 🔴 高优先级（建议近期完成）
*（暂无项目，结构化错误上下文已于 2026-05-05 完成）*

### 🟠 技能系统（v0.5.0 剩余）
1. **编辑器预览系统** — 非 Play 模式动画/特效预览、Scene Gizmo、帧步进
2. **技能节点库** — 伤害、Buff、检测判定、冷却（基于已完成的 GraphIntegration 桥接子模块）
3. **SkillGraph 专用编辑器窗口** — 当前直接复用 ShizukuGraphWindow，UI 改进待补
4. **暂停 / Seek / TimeScale** — SkillPlayer 增强

### 🟡 中优先级
5. **错误恢复模式 ErrorHandlingMode** — StopOnError / ContinueOnError
6. **时间轴/协程支持（蓝图）** — Delay、Sequence、Parallel
7. **蓝图函数扩展** — 有返回值的方法重写、CallParent 节点
8. **Quaternion 等类型端口** — 扩展类型系统
9. **撤销 / 重做** — 命令模式
10. **编辑器优化** — Ctrl+Space 搜索框、节点折叠、快捷键、网格吸附
11. **ForEach 循环节点** — 需要先实现集合类型端口

### 🟢 低优先级（未来版本）
12. **条件断点** / **表达式求值** — 调试高级功能
13. **调试器子链恢复** — 循环节点暂停后从游标继续（KNOWN_ISSUES 调试器 #1）
14. **执行路径动画** / **数据流动画** — 可视化增强
15. **性能分析** — 节点耗时统计
16. **AI 助手命令系统** — 将 UI 外壳连接到真正的命令解析
17. **变量分组/排序**
18. **ShizukuFunction 高级功能** — 泛型方法、重载、可选参数
19. **技能进阶** — 伤害公式编辑器、技能指示器、技能链/连招
20. **判定轨道 / 相机轨道** — 技能轨道扩展

---

## 🤝 贡献指南

欢迎社区贡献！优先接受以下类型的 PR：

1. **Bug 修复** - 最高优先级
2. **新节点类型** - 通用性强的节点
3. **文档改进** - 教程、示例、翻译
4. **性能优化** - 有 Benchmark 数据支持
5. **新功能** - 需先提 Issue 讨论

详见 [贡献指南](CONTRIBUTING.md)。

---

## 📞 反馈渠道

- **GitHub Issues**：Bug 报告和功能请求
- **Discussions**：使用交流和想法讨论
- **Email**：[Your Email]
- **QQ 群**：[QQ Group Number]

---

## 📝 更新说明

- 本路线图会根据实际开发进度和社区反馈调整
- 版本发布日期为预估，实际可能延后
- 标注 ⭐ 的功能为核心优先功能

**最后更新**：2026-05-05
**下次更新**：每月第一个周一
