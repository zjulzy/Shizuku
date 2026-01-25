# Shizuku 项目结构说明

本文档详细说明项目的目录结构和文件组织方式。

---

## 📁 根目录结构

```
Shizuku/
├── Assets/                     # Unity 资源目录
├── Packages/                   # Unity 包配置
├── ProjectSettings/            # Unity 项目设置
├── Docs/                       # 项目文档
│   ├── QUICK_REFERENCE.md      # 快速参考
│   ├── ARCHITECTURE.md         # 架构设计文档
│   ├── ROADMAP.md              # 开发路线图
│   ├── CONTRIBUTING.md         # 贡献指南
│   └── PROJECT_STRUCTURE.md    # 本文档
├── README.md                   # 项目主文档（首页）
└── LICENSE                     # MIT 许可证
```

---

## 📦 Assets 目录详解

```
Assets/
├── Editor/                     # 编辑器代码（仅编辑器运行）
│   ├── ControlFlowPort.cs      # 控制流端口（菱形样式）
│   ├── ControlFlowPortContainer.cs  # 端口容器
│   ├── CustomGroup.cs          # 自定义分组
│   ├── ShizukuGraphView.cs     # GraphView 主视图
│   ├── ShizukuGraphViewExtensions.cs  # 扩展方法（环检测等）
│   ├── ShizukuGraphWindow.cs   # 编辑器窗口
│   ├── ShizukuNodeView.cs      # 节点视图
│   └── Resources/              # 编辑器资源
│       ├── ControlFlowPort.uss # 控制流端口样式
│       ├── ControlFlowPortContainer.uss  # 容器样式
│       ├── ShizukuGraphView.uss  # GraphView 样式
│       └── ShizukuNodeView.uss   # 节点视图样式
│
├── Scripts/                    # 运行时代码
│   ├── GraphRunner.cs          # 图执行器（MonoBehaviour）
│   │
│   ├── Graph/                  # 图相关
│   │   ├── ShizukuGraphBase.cs # 图基类（ScriptableObject）
│   │   ├── ParameterEdge.cs    # 参数边数据
│   │   ├── GroupData.cs        # 分组数据
│   │   │
│   │   └── Blueprint/          # 蓝图系统
│   │       ├── ShizukuBluePrint.cs      # 泛型蓝图基类
│   │       ├── BlueprintBehavior.cs     # MonoBehaviour 包装类
│   │       ├── BlueprintExtensions.cs   # 辅助扩展方法
│   │       └── EXAMPLE_EnemyBlueprint.cs  # 示例蓝图
│   │
│   └── Node/                   # 节点相关
│       ├── ShizukuNodeBase.cs  # 节点基类
│       ├── ParameterEdgePort.cs  # 端口系统
│       │
│       ├── DerivedNodes/       # 具体节点实现
│       │   ├── ShizukuRootNode.cs    # 根节点
│       │   ├── ShizukuIfNode.cs      # 条件节点
│       │   ├── ShizukuLogNode.cs     # 日志节点
│       │   ├── ShizikuAddOneNode.cs  # +1 节点
│       │   └── ShizukuForNode.cs     # 循环节点（未完成）
│       │
│       └── BlueprintNodes/     # 蓝图专用节点
│           └── BlueprintEventNode.cs  # 事件节点
│
├── Scenes/                     # 场景文件
│   └── SampleScene.unity       # 示例场景
│
├── Settings/                   # URP 渲染设置
│   ├── PC_RPAsset.asset        # PC 渲染管线
│   ├── PC_Renderer.asset
│   ├── Mobile_RPAsset.asset    # 移动端渲染管线
│   ├── Mobile_Renderer.asset
│   ├── DefaultVolumeProfile.asset
│   └── ...
│
├── ShizukuGraph.asset          # 示例图资源
│
└── Readme.asset                # Unity 欢迎页面
```

---

## 🏗️ 代码组织原则

### 1. 运行时 vs 编辑器分离

```
Assets/
├── Scripts/      # 运行时代码（打包到游戏中）
└── Editor/       # 编辑器代码（仅开发时使用）
```

**为什么分离？**
- 编辑器代码不会打包到游戏中，减小包体积
- 避免运行时依赖编辑器 API
- 符合 Unity 最佳实践

### 2. 功能模块化

```
Scripts/
├── Graph/        # 图数据层
├── Node/         # 节点逻辑层
└── GraphRunner.cs  # 应用层
```

**依赖关系**：
```
GraphRunner
    ↓ 使用
ShizukuGraphBase
    ↓ 包含
ShizukuNodeBase
    ↓ 使用
ParameterEdgePort
```

### 3. 命名规范

| 类型 | 命名规则 | 示例 |
|-----|---------|------|
| 基类 | `*Base` | `ShizukuNodeBase` |
| 抽象类 | 有意义的名称 | `BlueprintBehavior` |
| 具体实现 | 描述性名称 | `ShizukuIfNode` |
| 接口 | `I*` | `IGraphValidator` |
| 数据类 | `*Data` | `GroupData` |
| 扩展方法 | `*Extensions` | `BlueprintExtensions` |

---

## 📄 文件职责说明

### 核心运行时文件

#### `ShizukuGraphBase.cs`
- **职责**：图数据容器和执行入口
- **继承**：`ScriptableObject`
- **包含**：节点列表、边列表、分组列表
- **关键方法**：`Init()`, `Update()`, `AddNode()`, `AddParameterEdge()`

#### `ShizukuNodeBase.cs`
- **职责**：节点抽象基类
- **继承**：无（纯 C# 类）
- **包含**：端口集合、依赖列表、ChainPorts
- **关键方法**：`Execute()`, `Init()`, `GetInputValues()`, `GetOutputValues()`
- **抽象方法**：`OnExecute()`, `OnSelectNextNode()`

#### `ParameterEdgePort.cs`
- **职责**：参数端口系统
- **继承**：基类 `ParameterEdgePort`，泛型 `ParameterEdgePort<T>`
- **包含**：端口值、连接信息
- **关键方法**：`GetSourceValue()`

#### `ParameterEdge.cs`
- **职责**：边连接数据（序列化）
- **包含**：输出/输入节点 GUID、端口名称
- **关键方法**：`ConnectPorts()`

### 蓝图系统文件

#### `ShizukuBluePrint.cs`
- **职责**：泛型蓝图基类
- **继承**：`ShizukuGraphBase`
- **泛型参数**：`T : BlueprintBehavior`
- **关键方法**：`InitializeBehavior()`, `RegisterBlueprintEvents()`, `RegisterPropertyAccessors()`

#### `BlueprintBehavior.cs`
- **职责**：MonoBehaviour 包装类
- **继承**：`MonoBehaviour`
- **包含**：蓝图引用、事件系统、属性访问器
- **关键方法**：`ExecuteBlueprintEvent()`, `GetBlueprintProperty()`, `SetBlueprintProperty()`

#### `BlueprintEventNode.cs`
- **职责**：事件节点（蓝图入口点）
- **继承**：`ShizukuNodeBase`
- **包含**：事件名称、事件参数列表
- **关键方法**：`TriggerEvent()`, `BindToBehavior()`

### 编辑器文件

#### `ShizukuGraphWindow.cs`
- **职责**：编辑器窗口入口
- **继承**：`EditorWindow`
- **包含**：GraphView 实例、工具栏
- **关键方法**：`OpenWindow()`, `OnOpenAsset()`

#### `ShizukuGraphView.cs`
- **职责**：GraphView 主视图
- **继承**：`GraphView`
- **包含**：节点视图集合、环检测逻辑
- **关键方法**：`BuildContextualMenu()`, `OnGraphViewChanged()`, `LoadFromAsset()`, `SaveToAsset()`

#### `ShizukuNodeView.cs`
- **职责**：节点视图渲染
- **继承**：`Node`
- **包含**：运行时节点引用、端口容器
- **关键方法**：`InitPort()`, `CreateInputFieldForPort()`, `SetPosition()`

#### `ControlFlowPort.cs`
- **职责**：控制流端口（菱形样式）
- **继承**：`Port`
- **关键方法**：`Create()`, `ApplyControlFlowStyle()`

---

## 🔄 数据流向

### 编辑器 → 运行时

```
用户在编辑器中操作
  ↓
ShizukuGraphView 更新数据
  ↓
ShizukuGraphBase (ScriptableObject)
  ├─ Nodes 列表
  ├─ Edges 列表
  └─ Groups 列表
  ↓
序列化到 .asset 文件
  ↓
运行时加载
  ↓
GraphRunner 或 BlueprintBehavior 执行
```

### 运行时执行流

```
GraphRunner.Update()
  ↓
ShizukuGraphBase.Update()
  ↓
RootNode.Execute()
  ↓
NextNode.Execute() (递归)
  ├─ GetInputValues() → 触发依赖节点
  ├─ OnExecute() → 执行逻辑
  └─ OnSelectNextNode() → 选择下一个
```

---

## 🎨 资源文件说明

### USS 样式表

| 文件 | 作用 |
|-----|------|
| `ShizukuGraphView.uss` | GraphView 和分组样式 |
| `ShizukuNodeView.uss` | 节点基础样式 |
| `ControlFlowPort.uss` | 控制流端口样式 |
| `ControlFlowPortContainer.uss` | 端口容器布局 |

### Unity 资源

| 文件 | 类型 | 说明 |
|-----|------|------|
| `ShizukuGraph.asset` | ScriptableObject | 示例图资源 |
| `Readme.asset` | TextAsset | Unity 欢迎页面 |

---

## 📝 添加新文件的建议位置

### 添加新节点
```
Assets/Scripts/Node/DerivedNodes/
└── YourNewNode.cs
```

### 添加新蓝图类型
```
Assets/Scripts/Graph/Blueprint/
└── YourNewBlueprint.cs
```

### 添加编辑器扩展
```
Assets/Editor/
└── YourEditorExtension.cs
```

### 添加样式表
```
Assets/Editor/Resources/
└── YourStyle.uss
```

### 添加文档
```
Docs/
└── YOUR_DOCUMENT.md
```

---

## 🔧 .meta 文件说明

Unity 为每个资源文件自动生成 `.meta` 文件，包含：
- GUID（全局唯一标识符）
- 导入设置
- 依赖关系

**重要**：
- ✅ 始终将 `.meta` 文件纳入版本控制
- ❌ 不要手动修改 GUID
- ✅ 删除文件时同时删除对应的 `.meta`

---

## 📦 项目配置文件

### `Packages/manifest.json`
定义项目依赖的 Unity 包：
```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.16.0",
    "com.unity.render-pipelines.universal": "17.2.0",
    "com.unity.visualscripting": "1.9.8",
    ...
  }
}
```

### `ProjectSettings/`
Unity 项目设置（图形、输入、物理等）。

---

## 🎯 最佳实践

### 1. 文件命名
- ✅ 使用 PascalCase
- ✅ 文件名与类名一致
- ✅ 一个文件一个主要类

### 2. 目录组织
- ✅ 按功能分组（Graph, Node, Editor）
- ✅ 避免过深的嵌套（最多 3-4 层）
- ✅ 相关文件放在一起

### 3. 依赖管理
- ✅ 运行时代码不依赖编辑器
- ✅ 基础类不依赖具体实现
- ✅ 避免循环依赖

### 4. 资源管理
- ✅ 使用 `Resources.Load<>()` 加载 USS
- ✅ 样式表放在 `Editor/Resources/`
- ✅ 运行时资源放在 `Assets/` 根目录或子目录

---

## 📊 项目统计

**代码量**（截至 v0.1.0）：
- 运行时代码：~2000 行
- 编辑器代码：~1500 行
- 文档：~8000 行
- 总计：~11500 行

**文件数量**：
- C# 脚本：~20 个
- USS 样式：4 个
- 文档：6 个
- Unity 资源：若干

---

**文档版本**：1.0
**最后更新**：2026-01-25
